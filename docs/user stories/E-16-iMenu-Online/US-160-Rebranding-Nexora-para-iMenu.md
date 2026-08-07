# US-160 · Rebranding Nexora para iMenu

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-02, RF-PLT-04 |
| **Regras de negócio** | — |
| **ADRs** | ADR-040 |
| **Eventos** | — |
| **Aplicações** | Todas |
| **Autoridade do dado** | — |

---

## 1. História

> **Como** administrador da plataforma (P9) e como gestor do estabelecimento (P8),
> **quero** que o produto se chame iMenu em toda superfície visível e em todo artefato técnico,
> **para** que não sobre nenhum resíduo do nome anterior (Nexora) confundindo cliente, equipe ou time técnico.

## 2. Contexto e motivação

O nome Nexora nasceu quando o produto ainda tinha o modelo local-first como diferencial central. Com a mudança de foco de negócio — o produto agora compete diretamente com o cardápio web tradicional — o nome muda para **iMenu**, já validado juridicamente e comercialmente (domínio e disponibilidade de marca confirmados).

Diferente da maioria das histórias deste pacote, esta não é uma feature — é uma varredura de consistência. O risco não é técnico, é de **esquecimento**: um nome antigo sobrevivendo num e-mail transacional, num rodapé, numa mensagem de erro ou num namespace gera confusão desproporcional ao tamanho do problema.

Importante: **o nome do projeto interno da Replay Studio (`004_DonaBetinha`) não muda.** Esta história rebatiza o **produto**, não o projeto/cliente-piloto.

## 3. Escopo

### 3.1 Dentro desta história

- Nome do produto em toda interface (título, splash, e-mails transacionais, rodapés, textos de ajuda)
- Manifest do PWA por tenant (nome, nome curto — ver ADR-010) gerado com "iMenu" como base, mantendo a marca do estabelecimento como personalização por cima
- Domínio e URLs (ver US-162 para a estrutura completa)
- Namespaces do backend .NET: `Nexora.Domain`, `Nexora.Application`, `Nexora.Infrastructure`, `Nexora.Api.*` → `iMenu.Domain`, `iMenu.Application`, `iMenu.Infrastructure`, `iMenu.Api` (a consolidação de `Api.Edge`/`Api.Cloud` em um único projeto é objeto da US-161; esta história cobre o nome, aquela cobre a estrutura)
- Nome de pacotes internos do monorepo que carregam "nexora" no identificador (`package.json`, `.csproj`, imagens Docker, variáveis de ambiente com prefixo do produto)
- Toda a documentação do pacote `Docs/` — ADRs, domain, user stories, PRD — onde "Nexora" aparece como nome do produto (não como nome do projeto/cliente)
- Comentários de código que citam "Nexora" como produto
- **Nova identidade visual da marca** (definida em 06/08/2026, ver seção 10 e 15):
  - Símbolo: monograma "i" cujo ponto é um prato visto de cima
  - Rampa de marca **borgonha → páprica** (quente), substituindo a rampa fria como identidade
  - Wordmark em peso médio (não Bold)
  - Seis ativos entregues em `design system/assets/logo-imenu-*.svg`
- Auditoria dos 6 arquivos que consomem `--nx-gradient-brand`, para separar o que era **marca**
  (deve migrar para a rampa quente) do que era apenas **cromo de UI** (deve passar a usar
  `--nx-gradient-ui`, preservado com a rampa fria original)

### 3.2 Fora desta história

- Estrutura de URL por tenant em si (US-162)
- Consolidação técnica edge/cloud em `iMenu.Api` (US-161)
- Identidade visual do **estabelecimento** (logo, cores do tenant) — já coberta por US-003/ADR-010, não muda aqui
- Nome do projeto Replay Studio (`004_DonaBetinha`) e do cliente-piloto (Pizzaria Dona Betinha)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Rebranding para iMenu

  Cenário: Nenhuma referência visível ao nome antigo
    Dado o produto em produção
    Quando qualquer tela, e-mail transacional ou manifest de PWA for inspecionado
    Então nenhum deve conter "Nexora"
    E todos devem exibir "iMenu" onde o nome do produto aparece

  Cenário: Namespace consistente no backend
    Dado o código-fonte do backend
    Quando os namespaces forem inspecionados
    Então nenhum deve começar com "Nexora."
    E os namespaces raiz devem ser "iMenu.Domain", "iMenu.Application", "iMenu.Infrastructure", "iMenu.Api"

  Cenário: Build e testes passam após o rename
    Dado o rename de namespaces aplicado
    Quando o pipeline de CI rodar
    Então build, lint e toda a suíte de testes devem passar sem alteração de comportamento

  Cenário: Projeto interno preservado
    Dado o pacote de documentação Docs/
    Quando referências ao projeto ou ao cliente-piloto forem inspecionadas
    Então "004_DonaBetinha" e "Pizzaria Dona Betinha" devem permanecer inalterados
    E apenas o nome do produto (antes Nexora) deve ter sido substituído por iMenu
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história — é uma mudança de identidade, não de regra de negócio._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

_Não se aplica diretamente — nenhuma rota muda de contrato por causa do nome. O manifest dinâmico (ADR-010) passa a ter "iMenu" como valor-base:_

```http
GET /manifest.webmanifest?tenant=<slug>
→ { "name": "iMenu · <nome do estabelecimento>",
    "short_name": "<nome curto do estabelecimento>", ... }
```

## 8. Modelo de dados

_Não se aplica — nenhuma tabela ou coluna referencia o nome do produto._

## 9. Comportamento offline

_Não se aplica — ver ADR-040: não há mais modo offline no produto._

## 10. Interface e experiência

- Nenhuma tela nova — apenas troca de texto/marca em telas já existentes
- Splash screen e ícone do PWA seguem o padrão de theming do ADR-010, com "iMenu" como nome-base sob a marca do tenant
- E-mails transacionais (convite, recuperação de acesso) revisados individualmente, não só por busca e substituição — tom e contexto podem exigir ajuste além do nome

### 10.1 Identidade visual da marca

**Símbolo** — monograma "i" minúsculo cujo ponto é um **prato visto de cima** (anel de aro fino).
Duas formas sólidas apenas; validado por rasterização em 16/24/32/48 px — o furo do anel
permanece aberto em todos.

**Rampa de marca — borgonha → páprica:**

| Parada | Token | Valor |
|---|---|---|
| 0% | `--im-wine-900` | `#4A1020` |
| 38% | `--im-wine-600` | `#8A2224` |
| 66% | `--im-brick-500` | `#BF4A20` |
| 100% | `--im-paprika-500` | `#E2731C` |
| Tinta do wordmark | `--im-ink` | `#2E1016` (17,5:1 sobre branco — WCAG AAA) |

A rampa quente foi escolhida por posicionamento: o produto compete na categoria alimentação,
onde vermelho/laranja é o código visual dominante.

**Restrição obrigatória** — a rampa de marca vive em marca, hero e material institucional.
**Nunca** em fundo de conteúdo, chip de status ou qualquer superfície do KDS: lá, vermelho é
`--nx-time-late` (atraso) e âmbar é `--nx-time-warn` (atenção), e a marca não pode competir
com esses significados. A regra está registrada em `packages/ui/src/tokens/colors.css`.

**Tipografia do wordmark** — peso médio (não Bold). Ver pendência de fonte na seção 15.

**Ativos** — `design system/assets/logo-imenu-{symbol,horizontal,lockup}[-white].svg`

## 11. Métricas, alertas e observabilidade

_Não se aplica diretamente._ Recomendado: varredura automatizada (grep em CI) que falha o build se "Nexora" aparecer em código ou texto de UI, para evitar regressão futura.

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Estático | Busca automatizada por "Nexora" (case-insensitive) em `apps/`, `backend/`, `packages/`, textos de e-mail — zero ocorrências como nome de produto |
| Build | CI completo (build + lint + testes) verde após o rename de namespace |
| Visual | Checagem manual de manifest, splash e e-mails transacionais em pelo menos um tenant |
| Regressão | Nenhuma mudança de comportamento — apenas texto/identificador |

## 13. Dependências

**Depende de:** nenhuma (pode iniciar imediatamente)
**Habilita:** US-161, US-162 (compartilham o mesmo rename de namespace/infra)

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Lista completa de superfícies com "Nexora" levantada (grep estrutural, não amostral)
- [ ] Confirmação de que "iMenu" está juridicamente livre para uso (domínio, marca) — **já confirmado**
- [ ] Estratégia de rename de namespace definida (ferramenta assistida, não find-and-replace manual em massa)

**DoD — a história só é concluída quando:**

- [ ] Busca automatizada por "Nexora" retorna zero ocorrências como nome de produto (permitindo apenas referências históricas explicitamente marcadas, como nos ADRs substituídos)
- [ ] Build, lint e testes passam sem alteração de comportamento
- [ ] Manifest, splash e e-mails revisados visualmente
- [ ] Documentação atualizada na mesma PR
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **Risco principal:** volume — o rename de namespace toca centenas de arquivos no backend. Fazer isso manualmente é fonte garantida de erro. Recomenda-se ferramenta de rename assistido por IDE/Roslyn, seguida de build completo como portão, não revisão linha a linha.
- Esta história e a US-161 compartilham o mesmo commit/PR de rename de namespace na prática, mas são histórias separadas porque endereçam preocupações distintas (identidade vs. arquitetura) — o time pode optar por executá-las juntas.
- **[PENDÊNCIA]** confirmar se há qualquer contrato, e-mail já enviado ao cliente-piloto ou material comercial impresso com o nome "Nexora" que precise de comunicação de mudança, além do código e da documentação.
- **[PENDÊNCIA] Fonte do wordmark.** Os SVGs entregues usam **Poppins Medium** convertida em
  curvas. A fonte de display declarada em `packages/ui/src/tokens/fonts.css` é **Montserrat
  ExtraBold** — que não estava disponível no ambiente de geração. Decidir: (a) adotar Poppins
  como fonte de display do iMenu e atualizar `fonts.css`, ou (b) regerar o wordmark a partir
  da Montserrat. Enquanto não for decidido, `fonts.css` e os ativos de marca estão divergentes.
- **[PENDÊNCIA] Rampa fria em `tenants.css` e nas guidelines.** A rampa fria continua sendo cor
  de UI (usada em 12 a 25 arquivos por token) e não foi removida — apenas deixou de ser a
  identidade de marca. Os 18 arquivos de `design system/guidelines/` ainda descrevem a rampa
  fria como "a marca" e precisam ser revistos.
- **Risco de marca do tenant.** O produto é white-label (ADR-010) e restaurante brasileiro —
  pizzaria em especial — usa vermelho com muita frequência. Uma marca de plataforma quente
  compete com a marca do cliente com mais probabilidade do que a fria competia. Mitigação
  prevista: a marca iMenu aparece como assinatura discreta, não como cromo dominante das telas
  do tenant. Validar no piloto.

---

*US-160 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
