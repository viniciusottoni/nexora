# 06 — ADRs · Decisões Arquiteturais
## Ecossistema Nexora

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Architecture Decision Records |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |

> ## ⚠ Este documento foi substituído
>
> As decisões arquiteturais passaram a ser mantidas como **ADRs individuais e detalhados** em **[`Docs/ADRs/`](ADRs/README.md)**.
>
> Este arquivo permanece como **sumário histórico** das 14 primeiras decisões. Para o conteúdo normativo — contexto completo, alternativas comparadas, detalhamento de implementação, critérios de validação e gatilho de revisão — consulte a pasta `ADRs/`, que contém **39 decisões** (35 originais + ADR-036 a 039, adicionadas em 01/08/2026 com a migração do backend de Node/NestJS/Prisma para C#/.NET).
>
> **Nota (01/08/2026):** as decisões originais de linguagem e framework de backend deste sumário — ADR-002 (TypeScript/monorepo), ADR-003 (NestJS) e ADR-005 (Prisma) — foram **substituídas** por [ADR-036](ADRs/ADR-036-dotnet-solution-clean-architecture.md), [ADR-037](ADRs/ADR-037-aspnet-core-backend.md) e [ADR-038](ADRs/ADR-038-ef-core-orm.md). O resumo abaixo é mantido como registro histórico do raciocínio original.
>
> | Onde | O quê |
> |---|---|
> | [`ADRs/README.md`](ADRs/README.md) | Índice por categoria, processo e regras |
> | [`ADRs/ADR-template.md`](ADRs/ADR-template.md) | Modelo para novas decisões |
> | `ADRs/ADR-0XX-*.md` | Uma decisão por arquivo |

---

> Cada ADR registra **contexto, decisão, alternativas consideradas e consequências** — inclusive as ruins. Um ADR não é revogado: é substituído por outro que o supera.

**Status possíveis:** `Proposto` · `Aceito` · `Substituído por ADR-xxx` · `Descontinuado`

---

## Índice

| ADR | Título | Status |
|---|---|---|
| [001](#adr-001) | Arquitetura local-first com servidor na loja | Aceito |
| [002](#adr-002) | TypeScript de ponta a ponta em monorepo | Substituído por [ADR-036](ADRs/ADR-036-dotnet-solution-clean-architecture.md) |
| [003](#adr-003) | NestJS como framework de backend | Substituído por [ADR-037](ADRs/ADR-037-aspnet-core-backend.md) |
| [004](#adr-004) | PostgreSQL com Row Level Security para multi-tenancy | Aceito |
| [005](#adr-005) | Prisma como ORM | Substituído por [ADR-038](ADRs/ADR-038-ef-core-orm.md) |
| [006](#adr-006) | Event sourcing seletivo em vez de completo | Aceito |
| [007](#adr-007) | Sincronização por transactional outbox | Aceito |
| [008](#adr-008) | Saldo de estoque derivado de movimentos | Aceito |
| [009](#adr-009) | PWA em vez de aplicativo nativo | Aceito |
| [010](#adr-010) | Theming em runtime, build único | Aceito |
| [011](#adr-011) | WebSocket local com fallback de polling | Aceito |
| [012](#adr-012) | Agregados pré-calculados para o painel | Aceito |
| [013](#adr-013) | Proibição de código específico por cliente | Aceito |
| [014](#adr-014) | Autenticação por PIN para perfis operacionais | Aceito |

---

<a id="adr-001"></a>
## ADR-001 · Arquitetura local-first com servidor na loja

**Status:** Aceito · 31/07/2026

### Contexto
O cliente foi explícito: *"se internet cair, produção local continua funcionando"*. A operação de uma pizzaria em horário de pico não tolera interrupção. Internet em ponto comercial no Brasil é instável o suficiente para que isso seja regra, não exceção.

### Decisão
Cada loja recebe um **servidor local (edge)** rodando API, banco e WebSocket em containers. Ele é a **autoridade operacional** para pedido, mesa, KDS e caixa. A nuvem consolida, administra e serve os canais externos.

### Alternativas consideradas

| Alternativa | Por que foi descartada |
|---|---|
| 100% nuvem com cache offline no navegador | IndexedDB não coordena múltiplos dispositivos entre si; dois garçons offline não veem o pedido um do outro |
| Sincronização peer-to-peer entre dispositivos | Complexidade de consenso muito alta; sem fonte de verdade clara |
| Nuvem com modo degradado só de leitura | Não atende: precisa **criar** pedido offline |

### Consequências

**Positivas:** operação nunca para; latência de pedido→KDS abaixo de 2 s; funciona em local com internet ruim — um diferencial comercial real.

**Negativas:** hardware por loja (custo e logística); sincronização é o componente mais complexo do sistema; suporte remoto a N instalações físicas; risco de falha do equipamento.

**Mitigações:** hardware padronizado com nobreak; cold standby pré-configurado; monitoramento remoto desde a Fase 1; runbook de contingência.

---

<a id="adr-002"></a>
## ADR-002 · TypeScript de ponta a ponta em monorepo

**Status:** Aceito

### Contexto
Sete aplicações (edge, cloud, admin, POS, KDS, menu, platform) precisam compartilhar entidades, regras de negócio e contratos. Uma regra de negócio divergente entre local e nuvem produz dado inconsistente — o pior defeito possível neste sistema.

### Decisão
TypeScript em todas as camadas, em monorepo `pnpm + Turborepo`. Regras de negócio puras em `packages/domain`, usadas **igualmente** por edge e cloud.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Backend em Go/Java, front em TS | Duplicaria as regras de domínio nos dois lados — fonte garantida de divergência |
| Repositórios separados | Versionamento cruzado de contratos vira trabalho permanente |

### Consequências
**Positivas:** um só time; tipos compartilhados; refatoração atômica; regra de negócio escrita uma vez.
**Negativas:** Node é menos eficiente que Go em CPU (irrelevante nesta volumetria); monorepo exige disciplina de build cache.

---

<a id="adr-003"></a>
## ADR-003 · NestJS como framework de backend

**Status:** Aceito

### Contexto
O mesmo código-base precisa rodar em dois contextos (edge e cloud) com módulos habilitados de forma diferente.

### Decisão
NestJS, com módulos por domínio e composição diferente por aplicação:

```ts
// api-edge
@Module({ imports: [OrderModule, TableModule, KdsModule, CashModule, SyncOutboxModule] })
// api-cloud
@Module({ imports: [SyncGatewayModule, MetricsModule, FinanceModule, PlatformModule, CatalogModule] })
```

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Fastify puro | Mais rápido, mas sem estrutura — em time que cresce, vira inconsistência |
| Express | Sem opinião arquitetural; código diverge por desenvolvedor |

### Consequências
**Positivas:** injeção de dependência, guards, interceptors e OpenAPI automático; módulos favorecem a separação edge/cloud.
**Negativas:** curva inicial de decorators; overhead de boot irrelevante aqui.

---

<a id="adr-004"></a>
## ADR-004 · PostgreSQL com RLS para multi-tenancy

**Status:** Aceito

### Contexto
RN-015 é categórica: nenhum dado de um estabelecimento pode ser visível a outro. Um `WHERE tenant_id` esquecido em uma query vaza dados entre clientes — e esse é o pior incidente possível para um produto multi-cliente.

### Decisão
Banco único, schema compartilhado, `tenant_id` em todas as tabelas de negócio, **isolamento imposto pelo PostgreSQL Row Level Security**.

### Alternativas

| Alternativa | Isolamento | Custo operacional | Descartada porque |
|---|---|---|---|
| Banco por tenant | Máximo | Alto | Migrations e backup × N inviabilizam a Fase 5 |
| Schema por tenant | Alto | Médio | Migrations ainda multiplicam; connection pooling complica |
| Filtro só na aplicação | Frágil | Baixo | Um esquecimento = vazamento |

### Consequências
**Positivas:** erro de isolamento torna-se impossível por construção; uma migration serve todos; custo linear baixo.
**Negativas:** exige `SET LOCAL app.tenant_id` em toda conexão (middleware); rota de plataforma precisa de `BYPASSRLS` com auditoria obrigatória; consultas cross-tenant só na camada de plataforma.

**Mitigação:** teste automatizado de isolamento roda em todo PR — tenta ler dado de outro tenant e espera falha.

---

<a id="adr-005"></a>
## ADR-005 · Prisma como ORM

**Status:** Aceito

### Contexto
Necessidade de migrations versionadas aplicáveis a um parque de lojas, com tipagem forte e produtividade alta.

### Decisão
Prisma como ORM principal. SQL puro (`$queryRaw`) para consultas analíticas e agregações complexas.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| TypeORM | Histórico de instabilidade em migrations |
| Drizzle | Excelente, mas ecossistema menos maduro para migrations em parque distribuído |
| Knex + SQL puro | Produtividade menor; sem tipagem derivada do schema |

### Consequências
**Positivas:** tipos gerados do schema; migrations reprodutíveis; `prisma migrate deploy` no edge é simples e confiável.
**Negativas:** Prisma exige `SET LOCAL` via `$executeRaw` para RLS — encapsular em middleware; consultas analíticas pesadas saem do ORM (aceitável e até desejável).

---

<a id="adr-006"></a>
## ADR-006 · Event sourcing seletivo, não completo

**Status:** Aceito

### Contexto
As diretrizes exigem métrica total, auditoria e sincronização confiável — três coisas que eventos resolvem bem. Mas event sourcing puro (estado reconstruído sempre por replay) adiciona complexidade que não se justifica para consultas operacionais simples como "quais mesas estão abertas".

### Decisão
**Modelo híbrido:** tabelas de estado tradicionais **mais** um log `domain_event` append-only. Toda transição grava as duas coisas na mesma transação. O estado serve a operação; o log serve métrica, auditoria e sincronização.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Event sourcing puro (CQRS completo) | Complexidade alta; consulta operacional exigiria projeções para tudo |
| Só estado, sem eventos | Perde métrica retroativa, auditoria e o mecanismo de sincronização |

### Consequências
**Positivas:** consulta operacional simples e rápida; métrica nova pode ser calculada sobre histórico já existente; auditoria vem de graça; sync tem unidade natural de transporte.
**Negativas:** dupla escrita exige disciplina (mitigada por interceptor que falha o build se a transição não emitir evento); volume de eventos exige particionamento.

---

<a id="adr-007"></a>
## ADR-007 · Sincronização por transactional outbox

**Status:** Aceito

### Contexto
Nenhum pedido pode se perder na sincronização, e nenhum pode duplicar. O clássico "salvar e depois publicar" falha se o processo cair entre as duas operações.

### Decisão
**Transactional outbox:** o evento é gravado na tabela `outbox` **dentro da mesma transação** do estado. Um worker lê o outbox e envia em lotes idempotentes, com cursor persistido.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Publicar direto em fila após commit | Janela de falha entre commit e publish = evento perdido |
| CRDTs / replicação bidirecional automática | Complexidade muito alta; semântica de negócio (autorizações, estoque) não é comutativa |
| Replicação lógica do PostgreSQL | Acopla schemas; não permite transformação nem filtragem por domínio |

### Consequências
**Positivas:** entrega garantida ao menos uma vez + idempotência = exatamente uma vez na prática; retomada automática; auditável.
**Negativas:** latência de segundos (aceitável — o painel remoto não é tempo real crítico); requer limpeza periódica do outbox.

---

<a id="adr-008"></a>
## ADR-008 · Saldo de estoque derivado de movimentos

**Status:** Aceito

### Contexto
O único conflito real de sincronização é o estoque: a loja dá baixa offline enquanto a nuvem registra uma compra. Sincronizar saldo produziria sobrescrita e perda de informação.

### Decisão
**Nunca sincronizar saldo. Sincronizar movimentos.** `ingredient.current_stock` é campo materializado por conveniência, recalculado a partir de `stock_movement`. Cada movimento tem identidade própria e é idempotente.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Sincronizar o saldo | Sobrescrita destrói informação; impossível auditar |
| Last-write-wins no saldo | Perde baixas feitas offline |

### Consequências
**Positivas:** conflito deixa de existir — vira apenas ordem de aplicação; auditabilidade completa; CMV teórico × real fica confiável.
**Negativas:** recálculo periódico do materializado; tabela de movimentos cresce (mitigada por agregação mensal).

---

<a id="adr-009"></a>
## ADR-009 · PWA em vez de aplicativo nativo

**Status:** Aceito

### Contexto
O cliente do salão precisa acessar o cardápio pelo QR Code **sem instalar nada** — instalação é a maior barreira de adoção em mesa. Garçom e cozinha precisam de instalação simples, sem loja de aplicativos.

### Decisão
PWA para todas as interfaces: mesa, garçom, KDS, caixa, painel. Service Worker para cache e fila offline.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| App nativo (React Native) | Instalação obrigatória mata a adoção na mesa; publicação em loja atrasa correção |
| Híbrido (PWA público + nativo interno) | Duplica base de código sem ganho proporcional |

### Consequências
**Positivas:** zero fricção na mesa; atualização instantânea sem loja; um só código; instalável na tela inicial para garçom e KDS.
**Negativas:** sem impressão térmica direta pelo navegador (resolvido por serviço de impressão no edge); notificações push em iOS têm limitações (mitigado: alertas críticos ficam nas telas operacionais internas).

---

<a id="adr-010"></a>
## ADR-010 · Theming em runtime, build único

**Status:** Aceito

### Contexto
Diretriz de produto: toda camada web personalizada por estabelecimento. Gerar um build por cliente cresce linearmente em custo de CI, deploy e correção de bug.

### Decisão
**Um único artefato para todos os tenants.** A identidade é carregada em runtime via `GET /v1/public/branding` e aplicada por CSS custom properties. Manifest do PWA gerado dinamicamente por tenant.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Build por cliente | N builds, N deploys, N pipelines para corrigir um bug |
| Tema em tempo de build com variáveis | Mesma limitação acima |

### Consequências
**Positivas:** um deploy corrige todos; novo cliente entra sem pipeline; personalização é dado, não código.
**Negativas:** flash de tema não estilizado (mitigado por cache do branding no Service Worker); personalização limitada aos tokens previstos — o que é **desejável** (ver ADR-013).

---

<a id="adr-011"></a>
## ADR-011 · WebSocket local com fallback de polling

**Status:** Aceito

### Contexto
O pedido precisa chegar ao KDS em menos de 2 segundos. A cozinha não pode depender de um único canal de comunicação — falha silenciosa de WebSocket significa pedido invisível.

### Decisão
Socket.IO no edge, com salas por tenant, praça, mesa e papel. **Fallback automático para polling a cada 5 s** quando o WebSocket estiver indisponível. Reconexão envia `lastEventId` para recuperar o que foi perdido.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Só polling | Latência e carga desnecessárias |
| Server-Sent Events | Unidirecional; precisamos de ack do cliente |
| Só WebSocket, sem fallback | Falha silenciosa = pedido perdido — inaceitável no gargalo do produto |

### Consequências
**Positivas:** latência baixa; degradação previsível; recuperação de mensagens perdidas.
**Negativas:** duas vias de entrega para testar e manter; necessidade de deduplicação no cliente.

---

<a id="adr-012"></a>
## ADR-012 · Agregados pré-calculados para o painel

**Status:** Aceito

### Contexto
RF-BI exige painel com resposta abaixo de 3 s, sobre milhões de eventos, com comparativos de período.

### Decisão
Worker mantém `metric_hourly` e `metric_daily` incrementalmente. Job noturno **recalcula o dia anterior por completo**, corrigindo agregados afetados por eventos sincronizados com atraso. O painel lê agregado; o drill-down consulta o evento apenas quando o usuário abre o número.

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| Consulta direta ao event store | Lento e caro em cada abertura do painel |
| Materialized views | Refresh completo é caro; incremental é limitado |
| Data warehouse dedicado | Complexidade desproporcional à volumetria |

### Consequências
**Positivas:** painel rápido; agregados são recalculáveis do zero se corrompidos; drill-down mantém rastreabilidade (RF-BI-11).
**Negativas:** dado do dia corrente pode ficar segundos atrás (aceitável e sinalizado na tela); worker é ponto de falha a monitorar.

---

<a id="adr-013"></a>
## ADR-013 · Proibição de código específico por cliente

**Status:** Aceito

### Contexto
A diretriz de produto replicável só se sustenta se a base for única. Historicamente, o que mata produtos deste tipo é a primeira exceção: um `if (tenant === 'x')` que se multiplica até tornar impossível atualizar o parque.

### Decisão
**É proibido código condicional por tenant.** Toda solicitação recebe uma de três respostas:

| Resposta | Ação |
|---|---|
| **(a) Já é configurável** | Ajustar configuração |
| **(b) Vira configuração nova** | Implementar como parâmetro do produto, beneficiando todos |
| **(c) Não entra** | Registrar a recusa com a justificativa |

Regra de CI: PR contendo comparação literal com identificador de tenant em código de negócio é **bloqueado**.

### Consequências
**Positivas:** uma base, um deploy, um teste; evolução beneficia todos os clientes; escalabilidade preservada.
**Negativas:** dizer "não" a clientes; algumas demandas exigem generalizar antes de atender, o que custa mais no curto prazo — e muito menos no longo.

---

<a id="adr-014"></a>
## ADR-014 · Autenticação por PIN para perfis operacionais

**Status:** Aceito

### Contexto
Garçom e cozinha trocam de operador dezenas de vezes por turno, com mãos ocupadas e sob pressão. Exigir e-mail e senha nesse contexto significa que a equipe vai compartilhar uma sessão única — destruindo toda a métrica por operador e toda a auditoria.

### Decisão
PIN numérico de 4 a 6 dígitos, **vinculado a dispositivo previamente registrado**. Token de sessão dura o turno (8 h). Ações sensíveis exigem PIN de perfil superior digitado no próprio dispositivo (`/v1/auth/authorize`).

### Alternativas
| Alternativa | Descartada porque |
|---|---|
| E-mail e senha | Inviável operacionalmente; leva ao compartilhamento de conta |
| Cartão RFID / crachá | Hardware adicional; considerar em fase futura |
| Biometria | Inviável com mãos sujas ou com luva |

### Consequências
**Positivas:** troca de operador em segundos; métrica e auditoria por pessoa passam a ser reais; autorização de gerente sem trocar de sessão.
**Negativas:** PIN é credencial fraca — mitigado por: só funciona em dispositivo registrado, na rede local, com bloqueio após 5 tentativas e rotação obrigatória a cada 90 dias.

---

## Decisões adiadas (a revisitar)

| Tema | Quando decidir | Por que está adiado |
|---|---|---|
| Emissão fiscal (NFC-e/SAT) | Antes da Fase 1 entrar em produção | **[PENDÊNCIA]** com cliente e contador |
| Integração TEF de maquininha | Fase 3 | Depende de definição comercial do cliente |
| Estratégia de impressão térmica | Fase 1, sprint 5 | Depende do hardware escolhido |
| Data warehouse dedicado | Acima de ~100 lojas | Desnecessário na volumetria atual |
| Banco por tenant | Se cliente exigir isolamento físico contratual | Sem demanda atual |
| App nativo para entregador | Fase 4 | Avaliar se PWA atende GPS em segundo plano |

---

*Documento 06 do pacote 004_DonaBetinha. Replay Studio.*
