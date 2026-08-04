# ADR-039 · Fronteiras de camada impostas por ProjectReference e testes de arquitetura

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 01/08/2026 |
| **Decisores** | Tech Lead |
| **Substitui** | [ADR-015](ADR-015-estrutura-monorepo-e-fronteiras.md) |
| **Relacionados** | ADR-002 (substituído), ADR-003 (substituído), ADR-013, ADR-036, ADR-037, ADR-038 |
| **Requisitos afetados** | RNF-MAN-02, RNF-MAN-03 |

---

## Contexto

O ADR-015 resolvia um problema real: monorepo sem fronteiras explícitas degrada — tudo importa tudo, o domínio passa a depender do banco, e a promessa do ADR-002 (regra escrita uma vez, válida em edge e nuvem) se perde silenciosamente. A solução foi zonas do ESLint (`import/no-restricted-paths`) verificadas no CI.

Com a migração para .NET (ADR-036), o mesmo problema existe — mas a ferramenta disponível para resolvê-lo é estruturalmente mais forte: em C#, uma fronteira entre projetos é imposta pelo grafo de `ProjectReference` do `.csproj`, verificado pelo **compilador**, não por uma regra de lint que roda em uma etapa separada e pode ser contornada com um comentário de supressão.

## Forças em jogo

| Força | Descrição |
|---|---|
| Fronteira que sobrevive a prazo apertado | Regra que depende só de disciplina humana não sobrevive — precisa ser verificada por máquina (princípio herdado do ADR-015) |
| Força da garantia | `ProjectReference` ausente é impossível de contornar sem editar o `.csproj` — mais difícil de burlar acidentalmente que uma regra de ESLint |
| Granularidade adicional | Algumas regras (ex.: "handler não pode ser `public`", "controller não injeta `DbContext`") não são expressáveis só com referências de projeto — exigem testes de arquitetura |
| Compatibilidade com o padrão de referência | `seminarioteologico` já usa este particionamento de projetos e prevê `SeminarioTeologico.ArchitectureTests` em `ARCHITECTURE.md` |

## Decisão

**As fronteiras entre camadas são impostas pelo grafo de `ProjectReference` da solution, complementadas por `Nexora.ArchitectureTests` (NetArchTest.Rules) para regras que a referência de projeto sozinha não expressa.**

## Detalhamento

### Estrutura (repete ADR-036, aqui do ponto de vista das fronteiras)

```
Nexora.slnx
├── Nexora.Domain              zero ProjectReference, zero PackageReference além da BCL
├── Nexora.Application         → Domain
├── Nexora.Contracts           → Domain
├── Nexora.Shared              (sem referência a nenhum outro projeto do domínio)
├── Nexora.Infrastructure      → Domain, Application, Contracts
├── Nexora.Api.Edge            → Application, Infrastructure, Contracts, Shared
└── Nexora.Api.Cloud           → Application, Infrastructure, Contracts, Shared
```

### Regra de dependência

| Projeto | Pode referenciar | Nunca referencia |
|---|---|---|
| `Domain` | nada além da BCL | MediatR, EF Core, ASP.NET Core, `Infrastructure`, `Api.*` |
| `Application` | `Domain`, MediatR, FluentValidation | EF Core, ASP.NET Core, `Infrastructure` |
| `Contracts` | `Domain` | `Application`, `Infrastructure`, ASP.NET Core |
| `Infrastructure` | `Domain`, `Application`, `Contracts`, EF Core, Npgsql, StackExchange.Redis | ASP.NET Core (exceto abstrações de `Options`) |
| `Api.Edge` / `Api.Cloud` | tudo | — |

> A linha mais importante, herdada literalmente do ADR-015: **`Nexora.Domain` não referencia nada.** É ela que garante que a regra de negócio é a mesma no edge e na nuvem. A diferença é que agora essa garantia não depende de configuração de lint — um `<ProjectReference>` de `Domain` para `Infrastructure` simplesmente não compila.

### Verificação automática — dois níveis

**Nível 1 — compilador (estrutural, não pode ser contornado):**

```xml
<!-- Nexora.Domain.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- Nenhum <ProjectReference> ou <PackageReference> além de utilitários puros -->
</Project>
```

**Nível 2 — testes de arquitetura (regras que referência de projeto não expressa):**

```csharp
// Nexora.ArchitectureTests
[Fact]
public void Domain_Nao_Deve_Depender_De_Nenhum_Outro_Projeto()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .Should()
        .NotHaveDependencyOnAny("MediatR", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}

[Fact]
public void Handlers_Nao_Devem_Ser_Publicos_Fora_De_Application()
{
    var result = Types.InAssembly(typeof(CreateOrderCommandHandler).Assembly)
        .That().ImplementInterface(typeof(IRequestHandler<,>))
        .Should().NotBePublic()
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}

[Fact]
public void Controllers_Nao_Devem_Injetar_AppDbContext_Diretamente()
{
    var result = Types.InAssembly(typeof(Program).Assembly)
        .That().Inherit(typeof(ControllerBase))
        .Should().NotHaveDependencyOn("Nexora.Infrastructure.Persistence.AppDbContext")
        .GetResult();

    result.IsSuccessful.Should().BeTrue();
}
```

### Convenção de namespace

```csharp
using Nexora.Domain.Orders;
using Nexora.Application.Orders.Commands.CreateOrder;
using Nexora.Contracts.Requests.Orders;
```

Sem equivalente ao "caminho relativo entre pacotes proibido" do ADR-015 — em C#, `using` sempre resolve pelo namespace, nunca por caminho de arquivo, então essa classe de erro não existe no novo stack.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Só `ProjectReference`, sem testes de arquitetura | Mais simples | Não cobre regras como "handler não pode ser `public`" ou "controller não injeta `DbContext`" | Perderia parte do valor que o ESLint com zonas customizadas dava no stack anterior |
| Só testes de arquitetura, sem restringir `ProjectReference` | Flexível | Qualquer projeto poderia referenciar qualquer outro; a violação só apareceria no teste, não no build | Mais fraco que o stack anterior, não mais forte — contraria o objetivo desta troca |
| Manter tudo em um único projeto (sem separação física) | Zero cerimônia de projetos | Nenhuma garantia estrutural; delegaria tudo a convenção e revisão de código | Repete exatamente o problema que o ADR-015 original resolveu — inaceitável regressão |

## Consequências

**Positivas**

- A promessa do ADR-036 ("`Domain` não importa nada") é verificada pelo **compilador**, mais forte que a verificação por lint do ADR-015 original
- Domínio testável sem banco e sem framework — testes rápidos, cobertura alta viável (mesmo ganho do ADR-015)
- `Nexora.ArchitectureTests` cobre as regras que `ProjectReference` sozinho não expressa
- Estrutura de projetos serve como mapa mental compartilhado do sistema, mesmo princípio do ADR-015

**Negativas**

- Adicionar um projeto novo exige configurar `ProjectReference` corretamente desde o início — mais cerimônia que criar uma pasta em um monorepo TS
- `Directory.Packages.props` mal configurado pode permitir que uma versão de pacote vaze para onde não deveria

**Mitigações**

- Template de projeto (`dotnet new` customizado ou script de scaffolding) já cria a estrutura com as referências corretas
- `Nexora.ArchitectureTests` roda em todo PR (bloqueante), mesma cadência do lint com zonas do ADR-015

## Como validar

- `dotnet build` falha se `Domain` ganhar uma `ProjectReference` indevida
- `Nexora.ArchitectureTests` roda em todo PR e bloqueia merge em caso de violação
- Cobertura de `Nexora.Domain` ≥ 90%, sem nenhum mock de infraestrutura

## Revisitar quando

- Um novo projeto exigir uma camada que não se encaixa na estrutura atual (candidato: um projeto `Nexora.Sync` dedicado, se o worker de sincronização crescer a ponto de justificar isolamento próprio)
- O time crescer a ponto de justificar divisão por squads donos de projeto
