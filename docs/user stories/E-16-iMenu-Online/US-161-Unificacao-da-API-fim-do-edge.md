# US-161 · Unificação da API — fim do edge

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 13 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | RF-PLT-05 |
| **Regras de negócio** | — |
| **ADRs** | ADR-040 (substitui ADR-001) |
| **Eventos** | — |
| **Aplicações** | `iMenu.Api` (novo), remove `api-edge`, `api-cloud` |
| **Autoridade do dado** | Nuvem — única, para todo domínio |

---

## 1. História

> **Como** time de engenharia,
> **quero** uma única API servindo todos os aplicativos, sem distinção entre edge e cloud,
> **para** que exista um único ponto de autoridade, um único deploy e um único modelo de dados para manter.

## 2. Contexto e motivação

O [ADR-001](../../adrs/ADR-001-arquitetura-local-first.md) dividia a autoridade dos dados entre um servidor local por loja (edge) e a nuvem, com sincronização bidirecional entre os dois. Essa divisão deixou de ter propósito: sem o requisito de operação sem internet, não há mais motivo para duas pontas.

O [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) formaliza a decisão; esta história é a execução técnica dela. É a história mais estruturalmente arriscada do épico — toda outra história de E-16, e boa parte do backlog restante, assume que ela está concluída.

## 3. Escopo

### 3.1 Dentro desta história

- Consolidar `Nexora.Api.Edge` e `Nexora.Api.Cloud` em um único projeto `iMenu.Api`
- Remover todo código específico de sincronização: outbox, worker de envio, recepção idempotente de sync, pull de catálogo/configuração edge→nuvem (ver também US-167 para a limpeza de histórias/documentação relacionadas)
- Remover `infra/edge/*` por completo (`docker-compose.yml`, `backup.sh`, `doctor.sh`, `install.sh`, `restore.sh`, `nginx.conf`, `postgres-init`, `test-backup-restore.sh`, `web.Dockerfile`)
- Renomear `infra/cloud/api-cloud.Dockerfile` para o Dockerfile único de `iMenu.Api`; remover `infra/cloud/api-edge.Dockerfile`
- Ajustar pipeline de CI/CD (`.github/workflows`) para o novo único artefato de deploy
- SignalR (ADR-011) passa a rodar dentro de `iMenu.Api`, não mais em `Api.Edge`
- Revisar `ADR-036` (Clean Architecture), `ADR-037` (ASP.NET Core backend) e `ADR-039` (fronteiras por project reference) quanto às referências a `Api.Edge`/`Api.Cloud` — marcar como pendente de atualização de conteúdo nesta mesma iniciativa (fora do detalhamento desta história, mas deve ser rastreado)

### 3.2 Fora desta história

- Rename de namespace `Nexora.*` → `iMenu.*` em si (US-160, ainda que tecnicamente ocorram no mesmo período)
- Estrutura de URLs por tenant (US-162)
- Remoção de tabelas do schema (`edge_installation`, `sync_cursor`) — US-169

## 4. Critérios de aceite

```gherkin
Funcionalidade: API única

  Cenário: Ponto único de entrada
    Dado qualquer aplicativo cliente (mesa, garçom, KDS, caixa, admin)
    Quando fizer uma requisição
    Então deve falar exclusivamente com iMenu.Api
    E não deve existir nenhum endpoint "api-edge" ou "api-cloud" ativo

  Cenário: Remoção de infraestrutura de edge
    Dado o repositório após esta história
    Quando o diretório infra/ for inspecionado
    Então infra/edge não deve existir
    E infra/cloud deve conter apenas o necessário para iMenu.Api

  Cenário: Tempo real sem servidor local
    Dado um pedido criado por um garçom
    Quando o evento for emitido
    Então deve chegar ao KDS via SignalR servido por iMenu.Api
    E nenhuma etapa deve depender de um servidor rodando na rede da loja

  Cenário: Nenhuma regressão funcional
    Dado o fluxo operacional completo (mesa → pedido → cozinha → caixa)
    Quando executado ponta a ponta após a consolidação
    Então deve se comportar exatamente como antes, exceto pela ausência de comportamento offline
```

## 5. Regras de negócio aplicáveis

_Não se aplica — é consolidação de arquitetura, não regra de negócio nova._

## 6. Eventos emitidos e consumidos

_Não se aplica diretamente._ Eventos de domínio existentes continuam sendo emitidos e gravados em `domain_event`/`audit_log` (ADR-006) — apenas deixam de alimentar um outbox de sincronização, porque este deixa de existir.

## 7. Contrato de API

```http
# Antes (dois hosts):
https://edge.local/v1/...
https://api.nexora.../v1/...

# Depois (um host):
https://api.imenu.../v1/...
```

Todos os contratos de endpoint que já existiam para operação (pedido, mesa, KDS, caixa) permanecem com o mesmo corpo e a mesma semântica — muda o host, não o contrato. Endpoints exclusivos de sincronização (`/v1/sync/push`, `/v1/sync/pull`, `/v1/sync/health`) são removidos.

## 8. Modelo de dados

_Ver US-169 para o detalhamento da remoção de `edge_installation` e `sync_cursor`._ Esta história não altera schema por si só — apenas a topologia de código que o acessa.

## 9. Comportamento offline

_Não se aplica — ver ADR-040. Esta história é, em parte, a remoção física do que sustentava o comportamento offline._

## 10. Interface e experiência

_Não se aplica — mudança de backend, sem impacto direto de UI além da URL base consumida pelos clientes (ver US-162)._

## 11. Métricas, alertas e observabilidade

- Métricas de sync (fila de outbox, atraso de sincronização) removidas dos painéis técnicos
- Observabilidade (OpenTelemetry, ADR-022) passa a instrumentar um único serviço em vez de dois

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Suíte de testes de integração completa passa contra `iMenu.Api` único |
| E2E | Fluxo salão→cozinha→caixa completo, sem qualquer componente de edge no ambiente de teste |
| Infra | Pipeline de CI/CD builda e publica um único artefato de API |
| Regressão | Nenhum teste que dependia de `api-edge` ou de comportamento offline permanece na suíte (removido ou adaptado — ver US-167) |

## 13. Dependências

**Depende de:** nenhuma (fundacional)
**Habilita:** US-162, US-163, US-164, US-167, US-169, e por extensão toda história de E-00 em diante que hoje referencia `api-edge`/`api-cloud`

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Levantamento completo do que existe em `infra/edge`, `infra/cloud` e no backend referenciando Edge/Cloud
- [ ] Estratégia de corte definida (branch isolada, portão de build/teste antes do merge)

**DoD — a história só é concluída quando:**

- [ ] `infra/edge` removido do repositório
- [ ] `Nexora.Api.Edge`/`Nexora.Api.Cloud` (ou `iMenu.Api.Edge`/`iMenu.Api.Cloud`, se o rename de US-160 ocorrer antes) não existem mais como projetos distintos
- [ ] `iMenu.Api` é o único artefato de backend publicado
- [ ] Suíte de testes completa (unitário + integração + E2E) passando
- [ ] Pipeline de CI/CD ajustado e verde
- [ ] Documentação atualizada (OpenAPI, ADR-036/037/039 sinalizados para revisão)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- **Risco principal:** esta é a história de maior risco técnico do épico — toca a espinha dorsal do backend. Recomenda-se executá-la em branch/worktree isolada, com build e suíte de testes como portão obrigatório antes de qualquer merge.
- **[PENDÊNCIA]** ADR-036, ADR-037 e ADR-039 citam `Api.Edge`/`Api.Cloud` no detalhamento de fronteiras e referências de projeto — precisam de revisão de conteúdo à parte, não coberta em detalhe por esta história (fica registrado aqui para não ser esquecido).
- Como não há loja em produção com hardware de edge instalado, não há plano de migração de dados a executar — é substituição de arquitetura em ambiente ainda de implementação, não corte de sistema em operação.

---

*US-161 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
