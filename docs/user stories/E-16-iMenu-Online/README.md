# E-16 · iMenu Online

|  |  |
|---|---|
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Histórias** | 10 |
| **Pontos** | 71 |
| **Sprints previstas** | Sprint 0 (antes ou em paralelo a E-00) |
| **Aplicações afetadas** | Todas — `iMenu.Api`, `web-admin`, `web-kds`, `web-menu`, `web-platform`, `web-pos` |
| **Pacotes do monorepo** | `backend/src` (namespaces), `infra/*`, `packages/contracts`, `packages/ui` |

---

## 1. Objetivo do épico

Reestruturar o produto para operar 100% online sob a marca **iMenu**, encerrando o modelo local-first (edge + sincronização) registrado no ADR-001 e substituído pelo [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md).

Motivo: **mudança de foco de negócio**, não limitação técnica. O produto passa a competir diretamente com o cardápio web tradicional — e portanto opera como um, 100% online, sem servidor local por loja e sem sincronização.

Este épico é **fundacional**: toda a documentação, todo o código e todo o modelo de dados que ainda referenciam Nexora, edge, cloud ou sincronização dependem dele. Por isso está alocado na Fase 0, revisando diretamente parte do que E-00 já estabelece (US-005, US-006).

## 2. Valor entregue

- Produto rebatizado como **iMenu**, com URLs previsíveis e por tenant (`/{tenantName}/server`, `/kds`, `/pos`, `/table/{qrCode}`, `/menu`, `/admin`)
- Uma única API (`iMenu.Api`) — fim da distinção edge/cloud, fim do custo e da complexidade de manter hardware por loja
- Autorização de dispositivo e login pessoal por PIN preservados, sem depender de rede local
- Abertura de mesa simplificada: QR Code + número da mesa, com confirmação e alerta imediato à equipe
- Base de dados e documentação (ADRs, backlog, RNFs) consistentes com a nova arquitetura, sem resíduo de conceitos que deixaram de existir

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-160](./US-160-Rebranding-Nexora-para-iMenu.md) | Rebranding Nexora para iMenu | M | 8 | RF-PLT-02, RF-PLT-04 |
| [US-161](./US-161-Unificacao-da-API-fim-do-edge.md) | Unificação da API — fim do edge | M | 13 | RF-PLT-05 |
| [US-162](./US-162-Nova-convencao-de-URLs-por-tenant.md) | Nova convenção de URLs por tenant | M | 8 | RF-PLT-03 |
| [US-163](./US-163-Autorizacao-de-dispositivo-operacional.md) | Autorização de dispositivo operacional | M | 5 | RF-IAM-05 |
| [US-164](./US-164-Login-pessoal-por-PIN-sem-rede-local.md) | Login pessoal por PIN sem rede local | M | 8 | RF-IAM-03, RF-IAM-07 |
| [US-165](./US-165-Abertura-de-mesa-por-QR-Code-e-numero.md) | Abertura de mesa por QR Code e número | M | 8 | RF-SAL-01, RF-SAL-04 |
| [US-166](./US-166-Impressao-de-QR-Codes-numerados-por-mesa.md) | Impressão de QR Codes numerados por mesa | M | 5 | RF-SAL-01 |
| [US-167](./US-167-Encerramento-da-sincronizacao-e-do-offline.md) | Encerramento da sincronização e do offline | M | 8 | — |
| [US-168](./US-168-Ajuste-de-metas-de-desempenho-online.md) | Ajuste de metas de desempenho online | M | 3 | — |
| [US-169](./US-169-Migracao-do-modelo-de-dados.md) | Migração do modelo de dados | M | 5 | — |

## 4. Ordem de execução recomendada

1. **US-161** — unificação da API (fundação técnica; tudo mais depende de existir um único `iMenu.Api`)
2. **US-169** — migração do modelo de dados (schema precisa estar limpo antes de construir em cima)
3. **US-160** — rebranding (nome, marca, textos — pode andar em paralelo à 161/169)
4. **US-162** — nova convenção de URLs por tenant
5. **US-163** — autorização de dispositivo operacional
6. **US-164** — login pessoal por PIN sem rede local
7. **US-165** — abertura de mesa por QR Code e número
8. **US-166** — impressão de QR Codes numerados
9. **US-167** — encerramento formal da sincronização e do offline (limpeza de código/doc residual)
10. **US-168** — ajuste de metas de desempenho (valida no fim, com a arquitetura já rodando)

## 5. O que este épico cancela ou substitui

| Item | Situação | Ver |
|---|---|---|
| **E-06 · Sincronização Local-Nuvem** (9 histórias) | Cancelado por inteiro | README de E-06 |
| **US-006** · Servidor local instalável por script (E-00) | Cancelada | US-006 |
| **US-034** · Operar pedido integralmente offline (E-03) | Cancelada | US-034 |
| **US-005** · Registro de dispositivos autorizados (E-00) | Substituída por US-163 | US-005 |
| **US-140** · Painel de instalações com saúde (E-14) | Marcada para redesenho na Fase 5 | US-140 |
| **ADR-001, 007, 019, 027, 033** | Substituídas por ADR-040 | `docs/adrs/` |
| **ADR-014** | Substituída por ADR-041 | `docs/adrs/` |
| **ADR-008, 010, 011** | Mantidas, com nota de revisão | `docs/adrs/` |
| **RF-OFF-01 a 09** (documento 01) | A remover do PRD (ver US-167) | `01-PRD-Especificacao-Funcional.md` |
| **RNF-OFF (seção 3, documento 08)** | Descontinuada (ver US-167) | `08-Requisitos-Nao-Funcionais.md` |

## 6. Dependências do épico

**Depende de:** nada tecnicamente — é fundacional. Depende apenas da decisão de negócio já confirmada (mudança de foco, produto 100% online).
**Habilita:** E-00 em diante — todo o restante do backlog assume esta arquitetura e esta marca a partir daqui.
**Bloqueia (até concluir):** qualquer história de E-00, E-02, E-03, E-04, E-05 que hoje referencia `api-edge`, `Nexora.*`, ou comportamento offline deve ser lida à luz deste épico antes de entrar em sprint.

## 7. Definition of Done do épico

- [ ] Nenhuma referência a "Nexora" em código, documentação ou UI (US-160)
- [ ] Nenhum diretório `infra/edge`; `Nexora.Api.Edge` e `Nexora.Api.Cloud` não existem mais como projetos distintos (US-161)
- [ ] As seis rotas por tenant descritas na visão geral resolvem corretamente (`/server`, `/kds`, `/pos`, `/table/{qrCode}`, `/menu`, `/admin`) (US-162)
- [ ] Autorização de dispositivo funciona ponta a ponta pela internet, sem qualquer dependência de rede local (US-163, US-164)
- [ ] Abertura de mesa por QR Code + número, com alerta à equipe, testada em cenário real (US-165, US-166)
- [ ] E-06 e as histórias listadas na seção 5 estão formalmente marcadas como canceladas/substituídas, não apenas ausentes do board (US-167)
- [ ] RNF-PER-01 revisado e medido contra a nova meta (US-168)
- [ ] Schema sem `edge_installation`, `sync_cursor` ou campos órfãos do modelo anterior (US-169)

## 8. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Volume de arquivos de código com namespace `Nexora.*` (≈1.260 no backend) torna o rename mecânico propenso a erro e a quebra de build | Alta | Alto | Executar em branch isolada, com rename assistido por ferramenta (não manual), build e suíte de testes completa como portão antes do merge — ver US-160/161 |
| Perda do diferencial "a loja nunca para" pode gerar objeção comercial futura, se mal comunicada ao cliente-piloto | Baixa | Médio | Decisão já confirmada pelo cliente nesta rodada; registrar por escrito no contrato/proposta comercial |
| Endpoint de PIN exposto à internet pública aumenta superfície de ataque em relação ao modelo de rede local | Média | Médio | Rate limit agressivo + token de dispositivo, conforme ADR-041 |
| Histórias de outros épicos (E-00, E-02 a E-05) ainda referenciam `api-edge`/offline em seus contratos de API e modelo de dados, e não foram todas revisadas nesta rodada | Alta | Médio | US-167 inclui varredura completa do backlog; até lá, qualquer história tocada deve ser conferida manualmente contra este épico antes de entrar em sprint |

---

*Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
