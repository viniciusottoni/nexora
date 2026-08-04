# ADR-013 · Proibição de código específico por cliente

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO, Gestão |
| **Relacionados** | ADR-004, ADR-010, ADR-032 |
| **Requisitos afetados** | RN-016, RF-PLT-01 a 08, RNF-MAN-02 |

---

## Contexto

A diretriz do cliente é clara: o sistema deve ser implantável em **qualquer estabelecimento com as mesmas dores**. Isso transforma o projeto de software sob medida em **produto**.

O modo como produtos assim morrem é sempre o mesmo, e é previsível:

```
Mês 1   "só um ajustezinho para esse cliente"        →  if (tenant.Slug == "x")
Mês 6   dezenas de condicionais espalhadas
Mês 12  ninguém consegue atualizar o parque sem quebrar alguém
Mês 18  cada cliente vira uma versão; o produto deixou de existir
```

A primeira exceção é sempre razoável. É por isso que a regra precisa ser categórica, e não uma diretriz de bom senso.

## Decisão

**É proibido código condicional por tenant em qualquer camada.**

Toda solicitação de cliente recebe uma de três respostas:

| Resposta | Ação |
|---|---|
| **(a) Já é configurável** | Ajustar a configuração do tenant |
| **(b) Vira configuração nova** | Implementar como parâmetro do produto — beneficia todos os clientes |
| **(c) Não entra** | Registrar a recusa com justificativa, e a decisão vai para o backlog de produto |

## Detalhamento

### O que é proibido

```csharp
// PROIBIDO — em qualquer camada
if (tenant.Slug == "dona-betinha") { ApplySpecialRule(); }
if (tenantId == Guid.Parse("0191..."))     { ShowCustomScreen(); }
var rules = new Dictionary<string, Rule> { ["dona-betinha"] = ..., ["outro-cliente"] = ... };
```

### O que é correto

```csharp
// Configuração declarada no produto (ADR-032)
if (config.Operation.HalfAndHalfPricing == HalfAndHalfPricing.Highest) { ... }
if (config.Features.KitchenStations) { ... }
```

A diferença essencial: no segundo caso, **qualquer cliente pode ativar**, e a regra é testada uma vez para todos.

### Verificação automática no CI

```yaml
# bloqueante em todo PR
- name: Verificar ADR-013
  run: |
    if grep -rEn "(tenant(Id|\.Slug)?\s*==\s*[\"'])" \
         backend/src --include=*.cs \
         --exclude-dir=Platform --exclude-dir=*Tests; then
      echo "::error::Código condicional por tenant detectado — viola ADR-013"
      exit 1
    fi
```

Exceções permitidas: módulo de plataforma (que legitimamente opera sobre tenants) e testes.

### Registro de recusas

Toda resposta do tipo **(c)** é registrada em `Docs/decisoes-de-produto.md` com data, cliente, pedido e justificativa. Esse registro tem duas funções: proteger a decisão de ser reaberta informalmente e revelar padrões — se três clientes pedirem a mesma coisa, é sinal de que deveria ser configuração.

### Fronteira com personalização

| Personalizável (ADR-010, ADR-032) | Não personalizável |
|---|---|
| Marca, cores, domínio, textos | Estrutura de tela |
| Cardápio, preços, fichas técnicas | Fluxo de navegação |
| Regras paramétricas, limiares, metas | Máquina de estados |
| Módulos ativos por plano | Modelo de dados |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Permitir exceções pontuais com aprovação | Flexível comercialmente | A primeira exceção legitima a segunda; a regra vira negociação | Historicamente é assim que produtos deste tipo morrem |
| Sistema de plugins por cliente | Isolamento das exceções | Complexidade alta; cada plugin é código a manter e testar | Desproporcional; adiaria o problema sem resolvê-lo |
| Fork por cliente | Liberdade total | N bases de código para corrigir e atualizar | Exatamente o cenário que a diretriz quer evitar |
| Nenhuma regra formal | Sem atrito | Depende de disciplina individual permanente | Disciplina não escala; a regra precisa ser mecânica |

## Consequências

**Positivas**

- Uma base, um deploy, um conjunto de testes
- Toda evolução beneficia todos os clientes
- Atualização do parque continua viável indefinidamente
- Configuração vira ativo de produto, não dívida

**Negativas**

- É preciso dizer "não" a clientes, inclusive ao cliente-piloto
- Algumas demandas exigem generalizar antes de atender, o que custa mais no curto prazo
- Pode gerar atrito comercial se a regra não for explicada desde o início

**Mitigações**

- A regra é comunicada ao cliente **no contrato**, não durante o projeto
- Resposta (b) costuma atender a demanda com um custo pequeno a mais
- Registro de recusas mostra padrões e alimenta o roadmap

## Como validar

- Verificação de CI bloqueante em todo PR
- Revisão de arquitetura mensal: nenhuma exceção introduzida
- Auditoria trimestral do `Docs/decisoes-de-produto.md` para identificar demandas recorrentes

## Revisitar quando

- Nunca, enquanto a diretriz de produto replicável valer. Se o produto voltar a ser software sob medida para um único cliente, esta decisão perde sentido — mas nesse caso o modelo de negócio inteiro mudou.
