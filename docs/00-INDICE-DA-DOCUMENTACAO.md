# Índice da Documentação — Projeto 004_DonaBetinha
## Ecossistema de Gestão e Operação para Estabelecimentos de Alimentação

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Produto** | Ecossistema de controle total para pizzarias e estabelecimentos similares |
| **Cliente-piloto** | Pizzaria Dona Betinha (primeira instância do produto) |
| **Responsável** | Sáskia — Replay Studio |
| **Versão do pacote** | 1.0 |
| **Data** | 31/07/2026 |

---

## Como esta documentação está organizada

O pacote segue a cadeia natural de decisão: **descoberta → visão → processo → produto → arquitetura → implementação**. Cada documento assume os anteriores como contexto.

```
DESCOBERTA          Assets/Briefing-Pedido-Inicial.docx
                              │
VISÃO               Visao-Geral-Sistema-Dona-Betinha.md
                    "o que é o produto e por quê"
                              │
PROCESSO            Otimizacao-Processos-Metricas-e-Experiencia-por-Usuario.md
                    "como a operação deve se comportar"
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
   PRODUTO              ARQUITETURA            EXECUÇÃO
   01-PRD               02-Arquitetura         07-Backlog
   08-RNF               03-Modelo de Dados     09-Roadmap
                        04-Eventos             10-Testes
                        05-API
                        06-ADRs
```

---

## Documentos

### Base — contexto e decisão de negócio

| # | Documento | Conteúdo | Público |
|---|---|---|---|
| — | **Visao-Geral-Sistema-Dona-Betinha.md** | Problema, visão, módulos, multi-tenant, white-label, fases, riscos | Cliente, gestão, time |
| — | **Otimizacao-Processos-Metricas-e-Experiencia-por-Usuario.md** | Gargalo, timestamps, métricas por usuário, automações, anti-padrões | Produto, UX, time |

### 01 a 10 — pacote de arquitetura e implementação

| # | Documento | Conteúdo | Público |
|---|---|---|---|
| **01** | `01-PRD-Especificacao-Funcional.md` | Escopo, personas, requisitos funcionais numerados (RF), regras de negócio (RN), fora de escopo | Produto, time, cliente |
| **02** | `02-Arquitetura-Tecnica.md` | Stack decidida, topologia local/nuvem, multi-tenant, sincronização, segurança, deploy | Arquitetura, dev, infra |
| **03** | `03-Modelo-de-Dados.md` | Visão **conceitual** de entidades e relacionamentos — o modelo físico executável está em `Domain/` | Backend, dados |
| **04** | `04-Catalogo-de-Eventos-e-Maquinas-de-Estado.md` | Event store, catálogo de eventos, máquinas de estado, derivação de métricas | Backend, dados, produto |
| **05** | `05-Contratos-de-API.md` | REST, WebSocket, autenticação, paginação, erros, idempotência, sincronização | Backend, frontend |
| **06** | `06-ADRs-Decisoes-Arquiteturais.md` | Sumário histórico das 14 primeiras decisões — **substituído por `ADRs/`** | Arquitetura, time |
| **ADRs** | `ADRs/` (pasta) | **39 ADRs individuais e detalhados** (35 originais + ADR-036 a 039, migração do backend para .NET) — conteúdo normativo. Comece por `ADRs/README.md` | Arquitetura, dev |
| **Domain** | `Domain/` (pasta) | **ERDs e DDL executável** por contexto delimitado — modelo físico. Comece por `Domain/README.md` | Backend, dados |
| **07** | `07-Backlog-Epicos-e-User-Stories.md` | Épicos, histórias, critérios de aceite em Gherkin, estimativas | Time, PO |
| **08** | `08-Requisitos-Nao-Funcionais.md` | Desempenho, disponibilidade, offline, segurança, LGPD, acessibilidade, observabilidade | Arquitetura, QA |
| **09** | `09-Roadmap-e-Plano-de-Entrega.md` | Fases, sprints, marcos, DoR/DoD, equipe, riscos de execução | Gestão, time |
| **10** | `10-Estrategia-de-Testes-e-Qualidade.md` | Pirâmide de testes, cenários críticos, teste de pico, caos offline, piloto | QA, time |

---

## Ordem de leitura sugerida

**Para começar a implementar (dev/arquiteto):**
`02 → ADRs/README.md → 03 → 04 → 05 → 07`

Dentro de `ADRs/`, a ordem de leitura que dá o panorama mais rápido é:
`001 → 015 → 004 → 006 → 007 → 018 → 017 → 020`

Para escrever a primeira migration, siga `Domain/README.md` — os documentos 00 a 12 estão na ordem exata de execução do DDL.

**Para entender o produto (PO/UX/cliente):**
`Visão → Otimização → 01 → 07 → 09`

**Para planejar contrato e proposta:**
`Visão → 01 → 08 → 09`

**Para QA:**
`01 → 04 → 08 → 10`

---

## Convenções usadas em todo o pacote

| Prefixo | Significado |
|---|---|
| **RF-xxx** | Requisito funcional |
| **RN-xxx** | Regra de negócio |
| **RNF-xxx** | Requisito não funcional |
| **ADR-xxx** | Decisão arquitetural registrada |
| **E-xx** | Épico |
| **US-xxx** | User story |
| **EVT-xxx** | Evento de domínio |
| **MET-xxx** | Métrica catalogada |

| Marcação | Significado |
|---|---|
| **[FATO]** | Confirmado na descoberta com o cliente |
| **[HIPÓTESE]** | Interpretação da Replay — exige validação |
| **[PENDÊNCIA]** | Informação ou decisão ausente — bloqueia definição |
| **[FASE n]** | Alocação na fase do roadmap |

---

## Pendências que ainda bloqueiam a proposta comercial

Estas questões atravessam todo o pacote e permanecem abertas:

| # | Pendência | Impacto |
|---|---|---|
| 1 | Emissão fiscal (NFC-e / SAT) | Escopo, custo e prazo |
| 2 | Propriedade do produto e modelo comercial | Contrato e estratégia |
| 3 | Prazo, orçamento e priorização do cliente | Dimensionamento |
| 4 | Modalidade de integração de pagamento (TEF × gateway) | Arquitetura de pagamento |
| 5 | Plano de contingência para falha do servidor local | Risco operacional crítico |
| 6 | Integração com iFood | Escopo da Fase 4 |
| 7 | App de frios — produto separado ou módulo | Escopo geral |

---

## Controle de versão do pacote

| Versão | Data | Alteração |
|---|---|---|
| 1.0 | 31/07/2026 | Criação do pacote completo de arquitetura e implementação |

> Este pacote é documento vivo. Alterações de escopo devem gerar nova versão do documento afetado e registro nesta tabela.

---

*Replay Studio — Projeto 004_DonaBetinha.*
