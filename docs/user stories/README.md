# User Stories — Ecossistema Dona Betinha

## Backlog detalhado por épico

|  |  |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Backlog detalhado — épicos e user stories |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Responsável** | Sáskia — Replay Studio |
| **Depende de** | `../01-PRD-Especificacao-Funcional.md`, `../04-Catalogo-de-Eventos-e-Maquinas-de-Estado.md`, `../05-Contratos-de-API.md` |
| **Resumo** | **16 épicos · 120 histórias · 785 pontos** |

---

## 1. O que é este pacote

Este diretório detalha, em nível de implementação, cada história do backlog consolidado em `../07-Backlog-Epicos-e-User-Stories.md`.

Cada épico é uma **pasta**; cada user story é um **arquivo** dentro dela. O documento 07 continua sendo a visão panorâmica do backlog — este pacote é o detalhamento que o time consome no refinamento e na sprint.

### Estrutura

```
Docs/User Stories/
├── README.md                          este arquivo
├── E-00-Fundacao-da-Plataforma/
│   ├── README.md                      visão do épico
│   ├── US-001-Estrutura-multi-tenant-com-isolamento.md
│   └── … (7 histórias)
├── E-01-Catalogo-e-Cardapio/
│   ├── README.md                      visão do épico
│   ├── US-010-Cadastrar-categorias-e-produtos.md
│   └── … (8 histórias)
├── E-02-Mesa-e-Comanda/
│   ├── README.md                      visão do épico
│   ├── US-020-Cadastrar-ambientes-mesas-e-gerar-QR-Code.md
│   └── … (9 histórias)
├── E-03-Pedido-e-Roteamento/
│   ├── README.md                      visão do épico
│   ├── US-030-Criar-pedido-com-itens-modificadores-e-fracoes.md
│   └── … (6 histórias)
├── E-04-KDS-Cozinha/
│   ├── README.md                      visão do épico
│   ├── US-040-Fila-de-pedidos-com-cartoes-e-cronometro.md
│   └── … (9 histórias)
├── E-05-Caixa-e-Pagamento/
│   ├── README.md                      visão do épico
│   ├── US-050-Painel-de-mesas-e-comandas-abertas.md
│   └── … (9 histórias)
├── E-06-Sincronizacao-Local-Nuvem/
│   ├── README.md                      visão do épico
│   ├── US-060-Outbox-transacional.md
│   └── … (9 histórias)
├── E-07-Painel-do-Dono-v1/
│   ├── README.md                      visão do épico
│   ├── US-070-Pulso-em-tempo-real-no-celular.md
│   └── … (8 histórias)
├── E-08-Alertas-e-Notificacoes/
│   ├── README.md                      visão do épico
│   ├── US-080-Motor-de-alertas-com-limiares-configuraveis.md
│   └── … (4 histórias)
├── E-09-Auditoria/
│   ├── README.md                      visão do épico
│   ├── US-090-Trilha-imutavel-de-acoes-sensiveis.md
│   └── … (2 histórias)
├── E-10-Estoque-e-Ficha-Tecnica/
│   ├── README.md                      visão do épico
│   ├── US-100-Cadastro-de-insumos-e-fornecedores.md
│   └── … (11 histórias)
├── E-11-Inteligencia-de-Fluxo/
│   ├── README.md                      visão do épico
│   ├── US-115-Fire-time-com-sequenciamento-reverso.md
│   └── … (5 histórias)
├── E-12-Financeiro-de-Gestao/
│   ├── README.md                      visão do épico
│   ├── US-120-Receita-automatica-a-partir-de-pagamentos.md
│   └── … (9 histórias)
├── E-13-Delivery-Proprio/
│   ├── README.md                      visão do épico
│   ├── US-130-Canal-publico-de-pedido-com-marca-propria.md
│   └── … (9 histórias)
├── E-14-Plataforma-em-Escala/
│   ├── README.md                      visão do épico
│   ├── US-140-Painel-de-instalacoes-com-saude.md
│   └── … (7 histórias)
└── E-15-Gestao-Geral-da-Plataforma/
    ├── README.md                      visão do épico
    ├── US-150-Estrutura-e-navegacao-do-painel-de-plataforma.md
    └── … (8 histórias)
```

### O que cada arquivo de história contém

| Seção | Conteúdo |
|---|---|
| Cabeçalho | Épico, fase, prioridade, pontos, sprint, RF, RN, ADR, eventos, aplicações e autoridade do dado |
| 1. História | Formato `Como <persona>, quero <ação>, para <resultado>` |
| 2. Contexto | Por que a história existe, ligada à descoberta e às decisões arquiteturais |
| 3. Escopo | O que entra e o que explicitamente não entra |
| 4. Critérios de aceite | Cenários em Gherkin, incluindo caminhos de exceção e offline |
| 5. Regras de negócio | RN aplicáveis e como se manifestam, com marcação de hipótese e pendência |
| 6. Eventos | EVT emitidos e consumidos, com payload e direção de sincronização |
| 7. Contrato de API | Endpoints, corpo, respostas e erros |
| 8. Modelo de dados | Tabelas e campos relevantes, com notas de modelagem |
| 9. Comportamento offline | O que funciona, o que degrada e como o usuário é informado |
| 10. Interface | Diretrizes de experiência específicas do contexto de uso |
| 11. Métricas | Indicadores gerados, alertas e observabilidade |
| 12. Testes | Níveis e o que verificar em cada um |
| 13. Dependências | De quais histórias depende e quais habilita |
| 14. DoR / DoD | Checklists de entrada em sprint e de conclusão |
| 15. Riscos | Riscos, premissas e pendências abertas |

## 2. Convenções

| Prefixo | Significado |
|---|---|
| **E-xx** | Épico |
| **US-xxx** | User story |
| **RF-xxx** | Requisito funcional (doc. 01) |
| **RN-xxx** | Regra de negócio (doc. 01) |
| **RNF-xxx** | Requisito não funcional (doc. 08) |
| **ADR-xxx** | Decisão arquitetural registrada (pasta `../ADRs/`) |
| **EVT-xxx** | Evento de domínio (doc. 04) |
| **MET-xxx** | Métrica catalogada (doc. 04) |
| **Px** | Persona (doc. 01, seção 3) |

| Marcação | Significado |
|---|---|
| **[FATO]** | Confirmado na descoberta com o cliente |
| **[HIPÓTESE]** | Interpretação da Replay — exige validação antes de virar compromisso |
| **[PENDÊNCIA]** | Informação ou decisão ausente — bloqueia definição |

| Prioridade | Significado |
|---|---|
| **M** | Must have — sem isso a fase não entrega valor |
| **S** | Should have — importante, mas a fase sobrevive sem |
| **C** | Could have — entra se houver folga |

**Estimativa:** Fibonacci em pontos (1, 2, 3, 5, 8, 13). Acima de 13 → fatiar.

## 3. Épicos por fase

### Fase 0 — Fundação da plataforma

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-00](./E-00-Fundacao-da-Plataforma/README.md) | Fundacao da Plataforma | 7 | 55 |
|  | **Subtotal** | **7** | **55** |

### Fase 1 — MVP

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-01](./E-01-Catalogo-e-Cardapio/README.md) | Catalogo e Cardapio | 8 | 42 |
| [E-02](./E-02-Mesa-e-Comanda/README.md) | Mesa e Comanda | 9 | 47 |
| [E-03](./E-03-Pedido-e-Roteamento/README.md) | Pedido e Roteamento | 6 | 53 |
| [E-04](./E-04-KDS-Cozinha/README.md) | KDS Cozinha | 9 | 50 |
| [E-05](./E-05-Caixa-e-Pagamento/README.md) | Caixa e Pagamento | 9 | 48 |
| [E-06](./E-06-Sincronizacao-Local-Nuvem/README.md) | Sincronizacao Local-Nuvem | 9 | 55 |
| [E-07](./E-07-Painel-do-Dono-v1/README.md) | Painel do Dono v1 | 8 | 47 |
| [E-08](./E-08-Alertas-e-Notificacoes/README.md) | Alertas e Notificacoes | 4 | 21 |
| [E-09](./E-09-Auditoria/README.md) | Auditoria | 2 | 13 |
|  | **Subtotal** | **64** | **376** |

### Fase 2 — Custo e controle

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-10](./E-10-Estoque-e-Ficha-Tecnica/README.md) | Estoque e Ficha Tecnica | 11 | 84 |
| [E-11](./E-11-Inteligencia-de-Fluxo/README.md) | Inteligencia de Fluxo | 5 | 42 |
|  | **Subtotal** | **16** | **126** |

### Fase 3 — Financeiro de gestão

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-12](./E-12-Financeiro-de-Gestao/README.md) | Financeiro de Gestao | 9 | 55 |
|  | **Subtotal** | **9** | **55** |

### Fase 4 — Delivery próprio

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-13](./E-13-Delivery-Proprio/README.md) | Delivery Proprio | 9 | 68 |
|  | **Subtotal** | **9** | **68** |

### Fase 5 — Produto replicável em escala

| Épico | Nome | Histórias | Pontos |
|---|---|--:|--:|
| [E-14](./E-14-Plataforma-em-Escala/README.md) | Plataforma em Escala | 7 | 47 |
| [E-15](./E-15-Gestao-Geral-da-Plataforma/README.md) | Gestao Geral da Plataforma | 8 | 58 |
|  | **Subtotal** | **15** | **105** |

> **Total geral: 16 épicos · 120 histórias · 785 pontos.**  
> **Fases 0 e 1 (MVP): 71 histórias · 431 pontos.**

## 4. Índice completo de histórias

### [E-00 · Fundacao da Plataforma](./E-00-Fundacao-da-Plataforma/README.md) — Fase 0

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-001](./E-00-Fundacao-da-Plataforma/US-001-Estrutura-multi-tenant-com-isolamento.md) | Estrutura multi-tenant com isolamento | M | 8 | RF-PLT-01 |
| [US-002](./E-00-Fundacao-da-Plataforma/US-002-Provisionar-novo-estabelecimento.md) | Provisionar novo estabelecimento | M | 5 | RF-PLT-05 |
| [US-003](./E-00-Fundacao-da-Plataforma/US-003-Identidade-visual-por-estabelecimento.md) | Identidade visual por estabelecimento | M | 8 | RF-PLT-02, RF-PLT-04 |
| [US-004](./E-00-Fundacao-da-Plataforma/US-004-Autenticacao-e-perfis-de-acesso.md) | Autenticacao e perfis de acesso | M | 13 | RF-IAM-01, RF-IAM-02, RF-IAM-03, RF-IAM-04, RF-IAM-06, RF-IAM-07 |
| [US-005](./E-00-Fundacao-da-Plataforma/US-005-Registro-de-dispositivos-autorizados.md) | Registro de dispositivos autorizados | M | 5 | RF-IAM-05 |
| [US-006](./E-00-Fundacao-da-Plataforma/US-006-Servidor-local-instalavel-por-script.md) | Servidor local instalavel por script | M | 8 | RF-PLT-05, RF-OFF-01 |
| [US-007](./E-00-Fundacao-da-Plataforma/US-007-Pipeline-de-CI-CD-com-travas-de-governanca.md) | Pipeline de CI-CD com travas de governanca | M | 8 | — |

### [E-01 · Catalogo e Cardapio](./E-01-Catalogo-e-Cardapio/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-010](./E-01-Catalogo-e-Cardapio/US-010-Cadastrar-categorias-e-produtos.md) | Cadastrar categorias e produtos | M | 5 | RF-CAT-01 |
| [US-011](./E-01-Catalogo-e-Cardapio/US-011-Variacoes-de-produto-com-preco-proprio.md) | Variacoes de produto com preco proprio | M | 5 | RF-CAT-02 |
| [US-012](./E-01-Catalogo-e-Cardapio/US-012-Grupos-de-modificadores.md) | Grupos de modificadores | M | 8 | RF-CAT-03 |
| [US-013](./E-01-Catalogo-e-Cardapio/US-013-Pizza-meio-a-meio-com-fracoes.md) | Pizza meio a meio com fracoes | M | 8 | RF-CAT-04, RF-CAT-05 |
| [US-014](./E-01-Catalogo-e-Cardapio/US-014-Preco-por-canal-de-venda.md) | Preco por canal de venda | M | 3 | RF-CAT-06 |
| [US-015](./E-01-Catalogo-e-Cardapio/US-015-Marcar-produto-indisponivel-com-propagacao-imediata.md) | Marcar produto indisponivel com propagacao imediata | M | 5 | RF-CAT-07 |
| [US-016](./E-01-Catalogo-e-Cardapio/US-016-Tempo-de-preparo-e-praca-por-produto.md) | Tempo de preparo e praca por produto | M | 3 | RF-CAT-08, RF-CAT-09 |
| [US-017](./E-01-Catalogo-e-Cardapio/US-017-Cadastro-de-pracas-de-producao.md) | Cadastro de pracas de producao | M | 5 | RF-CAT-09, RF-KDS-06 |

### [E-02 · Mesa e Comanda](./E-02-Mesa-e-Comanda/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-020](./E-02-Mesa-e-Comanda/US-020-Cadastrar-ambientes-mesas-e-gerar-QR-Code.md) | Cadastrar ambientes mesas e gerar QR Code | M | 5 | RF-SAL-01 |
| [US-021](./E-02-Mesa-e-Comanda/US-021-Cliente-acessa-cardapio-pelo-QR-Code.md) | Cliente acessa cardapio pelo QR Code | M | 8 | RF-SAL-02 |
| [US-022](./E-02-Mesa-e-Comanda/US-022-Abrir-mesa-por-garcom-ou-por-cliente.md) | Abrir mesa por garcom ou por cliente | M | 5 | RF-SAL-04 |
| [US-023](./E-02-Mesa-e-Comanda/US-023-Mapa-de-mesas-com-status-e-tempo.md) | Mapa de mesas com status e tempo | M | 8 | RF-SAL-05 |
| [US-024](./E-02-Mesa-e-Comanda/US-024-Consumo-da-mesa-em-tempo-real.md) | Consumo da mesa em tempo real | M | 5 | RF-SAL-06 |
| [US-025](./E-02-Mesa-e-Comanda/US-025-Chamar-garcom-pela-mesa.md) | Chamar garcom pela mesa | M | 3 | RF-SAL-07 |
| [US-026](./E-02-Mesa-e-Comanda/US-026-Solicitar-a-conta.md) | Solicitar a conta | M | 3 | RF-SAL-08 |
| [US-027](./E-02-Mesa-e-Comanda/US-027-Dividir-a-conta.md) | Dividir a conta | M | 8 | RF-SAL-10 |
| [US-028](./E-02-Mesa-e-Comanda/US-028-Repetir-item-com-um-toque.md) | Repetir item com um toque | S | 2 | RF-SAL-11 |

### [E-03 · Pedido e Roteamento](./E-03-Pedido-e-Roteamento/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-030](./E-03-Pedido-e-Roteamento/US-030-Criar-pedido-com-itens-modificadores-e-fracoes.md) | Criar pedido com itens modificadores e fracoes | M | 13 | RF-PED-01, RF-PED-08, RF-SAL-03, RF-SAL-04 |
| [US-031](./E-03-Pedido-e-Roteamento/US-031-Roteamento-simultaneo-para-cozinha-e-caixa.md) | Roteamento simultaneo para cozinha e caixa | M | 8 | RF-KDS-01, RF-CXA-01 |
| [US-032](./E-03-Pedido-e-Roteamento/US-032-Carimbos-de-tempo-T0-a-T5.md) | Carimbos de tempo T0 a T5 | M | 8 | RF-PED-02, RF-PED-03 |
| [US-033](./E-03-Pedido-e-Roteamento/US-033-Cancelar-item-ou-pedido-com-autorizacao.md) | Cancelar item ou pedido com autorizacao | M | 8 | RF-PED-04, RF-PED-05 |
| [US-034](./E-03-Pedido-e-Roteamento/US-034-Operar-pedido-integralmente-offline.md) | Operar pedido integralmente offline | M | 13 | RF-PED-09, RF-OFF-01, RF-OFF-02, RF-OFF-05 |
| [US-035](./E-03-Pedido-e-Roteamento/US-035-Bloquear-fechamento-com-item-pendente.md) | Bloquear fechamento com item pendente | S | 3 | RF-PED-06 |

### [E-04 · KDS Cozinha](./E-04-KDS-Cozinha/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-040](./E-04-KDS-Cozinha/US-040-Fila-de-pedidos-com-cartoes-e-cronometro.md) | Fila de pedidos com cartoes e cronometro | M | 13 | RF-KDS-02, RF-KDS-03, RF-KDS-05 |
| [US-041](./E-04-KDS-Cozinha/US-041-Avancar-estado-com-um-toque-via-teclado-numerico.md) | Avancar estado com um toque via teclado numerico | M | 8 | RF-KDS-04, RF-KDS-05 |
| [US-042](./E-04-KDS-Cozinha/US-042-Filtro-por-praca-de-producao.md) | Filtro por praca de producao | M | 5 | RF-KDS-06 |
| [US-043](./E-04-KDS-Cozinha/US-043-Contagem-consolidada-all-day.md) | Contagem consolidada all-day | S | 5 | RF-KDS-07 |
| [US-044](./E-04-KDS-Cozinha/US-044-Marcar-item-indisponivel-pelo-KDS.md) | Marcar item indisponivel pelo KDS | M | 5 | RF-KDS-10 |
| [US-045](./E-04-KDS-Cozinha/US-045-Alerta-sonoro-de-pedido-novo-e-de-atraso.md) | Alerta sonoro de pedido novo e de atraso | M | 3 | RF-KDS-13 |
| [US-046](./E-04-KDS-Cozinha/US-046-Historico-do-turno-no-KDS.md) | Historico do turno no KDS | S | 3 | RF-KDS-14 |
| [US-047](./E-04-KDS-Cozinha/US-047-Modo-pico-com-simplificacao-automatica.md) | Modo pico com simplificacao automatica | C | 5 | — |
| [US-048](./E-04-KDS-Cozinha/US-048-Fallback-de-polling-se-WebSocket-cair.md) | Fallback de polling se WebSocket cair | M | 3 | RF-KDS-01 |

### [E-05 · Caixa e Pagamento](./E-05-Caixa-e-Pagamento/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-050](./E-05-Caixa-e-Pagamento/US-050-Painel-de-mesas-e-comandas-abertas.md) | Painel de mesas e comandas abertas | M | 5 | RF-CXA-01 |
| [US-051](./E-05-Caixa-e-Pagamento/US-051-Conta-montada-automaticamente.md) | Conta montada automaticamente | M | 8 | RF-CXA-02 |
| [US-052](./E-05-Caixa-e-Pagamento/US-052-Multiplas-formas-de-pagamento-na-mesma-conta.md) | Multiplas formas de pagamento na mesma conta | M | 8 | RF-CXA-03 |
| [US-053](./E-05-Caixa-e-Pagamento/US-053-Taxa-de-servico-configuravel-com-retirada-registrada.md) | Taxa de servico configuravel com retirada registrada | M | 5 | RF-CXA-04 |
| [US-054](./E-05-Caixa-e-Pagamento/US-054-Desconto-com-autorizacao.md) | Desconto com autorizacao | M | 5 | RF-CXA-05 |
| [US-055](./E-05-Caixa-e-Pagamento/US-055-Abertura-e-fechamento-de-caixa.md) | Abertura e fechamento de caixa | M | 8 | RF-CXA-06, RF-CXA-08 |
| [US-056](./E-05-Caixa-e-Pagamento/US-056-Sangria-e-suprimento.md) | Sangria e suprimento | S | 3 | RF-CXA-07 |
| [US-057](./E-05-Caixa-e-Pagamento/US-057-Comprovante-nao-fiscal-de-consumo.md) | Comprovante nao fiscal de consumo | M | 3 | RF-CXA-12 |
| [US-058](./E-05-Caixa-e-Pagamento/US-058-Registrar-pagamento-de-maquininha-externa.md) | Registrar pagamento de maquininha externa | M | 3 | RF-CXA-10 |

### [E-06 · Sincronizacao Local-Nuvem](./E-06-Sincronizacao-Local-Nuvem/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-060](./E-06-Sincronizacao-Local-Nuvem/US-060-Outbox-transacional.md) | Outbox transacional | M | 8 | RF-OFF-02 |
| [US-061](./E-06-Sincronizacao-Local-Nuvem/US-061-Worker-de-envio-com-retry-e-cursor.md) | Worker de envio com retry e cursor | M | 8 | RF-OFF-02 |
| [US-062](./E-06-Sincronizacao-Local-Nuvem/US-062-Recepcao-idempotente-na-nuvem.md) | Recepcao idempotente na nuvem | M | 8 | RF-OFF-03 |
| [US-063](./E-06-Sincronizacao-Local-Nuvem/US-063-Pull-de-cardapio-e-configuracao.md) | Pull de cardapio e configuracao | M | 5 | RF-OFF-02 |
| [US-064](./E-06-Sincronizacao-Local-Nuvem/US-064-Preservacao-do-horario-de-ocorrencia.md) | Preservacao do horario de ocorrencia | M | 5 | RF-OFF-04 |
| [US-065](./E-06-Sincronizacao-Local-Nuvem/US-065-Indicador-de-conexao-e-atraso-de-sincronizacao.md) | Indicador de conexao e atraso de sincronizacao | M | 5 | RF-OFF-05, RF-BI-14 |
| [US-066](./E-06-Sincronizacao-Local-Nuvem/US-066-Alerta-de-atraso-de-sincronizacao.md) | Alerta de atraso de sincronizacao | M | 3 | RF-OFF-06 |
| [US-067](./E-06-Sincronizacao-Local-Nuvem/US-067-Registro-e-revisao-de-conflitos.md) | Registro e revisao de conflitos | M | 5 | RF-OFF-07 |
| [US-068](./E-06-Sincronizacao-Local-Nuvem/US-068-Recuperacao-apos-reconexao-longa.md) | Recuperacao apos reconexao longa | M | 8 | RF-OFF-02, RF-OFF-03 |

### [E-07 · Painel do Dono v1](./E-07-Painel-do-Dono-v1/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-070](./E-07-Painel-do-Dono-v1/US-070-Pulso-em-tempo-real-no-celular.md) | Pulso em tempo real no celular | M | 8 | RF-BI-01, RF-BI-14 |
| [US-071](./E-07-Painel-do-Dono-v1/US-071-Tempos-por-etapa-com-media-e-p90.md) | Tempos por etapa com media e p90 | M | 8 | RF-BI-02, RF-BI-03 |
| [US-072](./E-07-Painel-do-Dono-v1/US-072-Aderencia-ao-prazo-OTD.md) | Aderencia ao prazo OTD | M | 5 | RF-BI-04 |
| [US-073](./E-07-Painel-do-Dono-v1/US-073-Faturamento-com-comparativo.md) | Faturamento com comparativo | M | 5 | RF-BI-05 |
| [US-074](./E-07-Painel-do-Dono-v1/US-074-Venda-por-canal-produto-e-categoria.md) | Venda por canal produto e categoria | M | 5 | RF-BI-06 |
| [US-075](./E-07-Painel-do-Dono-v1/US-075-Ticket-medio-giro-de-mesa-e-ocupacao.md) | Ticket medio giro de mesa e ocupacao | M | 5 | RF-BI-07 |
| [US-076](./E-07-Painel-do-Dono-v1/US-076-Drill-down-do-numero-ate-o-pedido.md) | Drill-down do numero ate o pedido | M | 8 | RF-BI-11 |
| [US-077](./E-07-Painel-do-Dono-v1/US-077-Resumo-diario-automatico.md) | Resumo diario automatico | S | 3 | RF-BI-12 |

### [E-08 · Alertas e Notificacoes](./E-08-Alertas-e-Notificacoes/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-080](./E-08-Alertas-e-Notificacoes/US-080-Motor-de-alertas-com-limiares-configuraveis.md) | Motor de alertas com limiares configuraveis | M | 8 | RF-ALT-01, RF-ALT-02 |
| [US-081](./E-08-Alertas-e-Notificacoes/US-081-Entrega-in-app-e-push-de-navegador.md) | Entrega in-app e push de navegador | M | 5 | RF-ALT-03 |
| [US-082](./E-08-Alertas-e-Notificacoes/US-082-Direcionamento-por-perfil-e-por-acao.md) | Direcionamento por perfil e por acao | M | 5 | RF-ALT-01 |
| [US-083](./E-08-Alertas-e-Notificacoes/US-083-Agrupamento-de-alertas-repetidos.md) | Agrupamento de alertas repetidos | S | 3 | RF-ALT-04 |

### [E-09 · Auditoria](./E-09-Auditoria/README.md) — Fase 1

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-090](./E-09-Auditoria/US-090-Trilha-imutavel-de-acoes-sensiveis.md) | Trilha imutavel de acoes sensiveis | M | 8 | RF-AUD-01, RF-AUD-02, RF-AUD-04 |
| [US-091](./E-09-Auditoria/US-091-Consulta-e-filtro-da-trilha.md) | Consulta e filtro da trilha | M | 5 | RF-AUD-03 |

### [E-10 · Estoque e Ficha Tecnica](./E-10-Estoque-e-Ficha-Tecnica/README.md) — Fase 2

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-100](./E-10-Estoque-e-Ficha-Tecnica/US-100-Cadastro-de-insumos-e-fornecedores.md) | Cadastro de insumos e fornecedores | M | 5 | RF-EST-01, RF-EST-11 |
| [US-101](./E-10-Estoque-e-Ficha-Tecnica/US-101-Ficha-tecnica-por-variacao.md) | Ficha tecnica por variacao | M | 8 | RF-EST-02 |
| [US-102](./E-10-Estoque-e-Ficha-Tecnica/US-102-Sub-receitas-de-preparo-intermediario.md) | Sub-receitas de preparo intermediario | S | 8 | RF-EST-03 |
| [US-103](./E-10-Estoque-e-Ficha-Tecnica/US-103-Baixa-automatica-na-conclusao-do-item.md) | Baixa automatica na conclusao do item | M | 13 | RF-EST-04 |
| [US-104](./E-10-Estoque-e-Ficha-Tecnica/US-104-Entradas-de-compra-com-custo-e-validade.md) | Entradas de compra com custo e validade | M | 8 | RF-EST-05 |
| [US-105](./E-10-Estoque-e-Ficha-Tecnica/US-105-Registro-de-perda-com-motivo-classificado.md) | Registro de perda com motivo classificado | M | 5 | RF-EST-06 |
| [US-106](./E-10-Estoque-e-Ficha-Tecnica/US-106-Contagem-ciclica-e-divergencia.md) | Contagem ciclica e divergencia | M | 8 | RF-EST-07 |
| [US-107](./E-10-Estoque-e-Ficha-Tecnica/US-107-CMV-teorico-versus-real.md) | CMV teorico versus real | M | 8 | RF-EST-08 |
| [US-108](./E-10-Estoque-e-Ficha-Tecnica/US-108-Alerta-de-estoque-minimo-e-validade.md) | Alerta de estoque minimo e validade | M | 5 | RF-EST-09, RF-EST-12 |
| [US-109](./E-10-Estoque-e-Ficha-Tecnica/US-109-Custo-e-margem-por-produto.md) | Custo e margem por produto | M | 8 | RF-EST-13 |
| [US-110](./E-10-Estoque-e-Ficha-Tecnica/US-110-Matriz-de-engenharia-de-cardapio.md) | Matriz de engenharia de cardapio | M | 8 | RF-BI-09 |

### [E-11 · Inteligencia de Fluxo](./E-11-Inteligencia-de-Fluxo/README.md) — Fase 2

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-115](./E-11-Inteligencia-de-Fluxo/US-115-Fire-time-com-sequenciamento-reverso.md) | Fire time com sequenciamento reverso | S | 13 | RF-KDS-09 |
| [US-116](./E-11-Inteligencia-de-Fluxo/US-116-Prioridade-dinamica-explicavel.md) | Prioridade dinamica explicavel | S | 8 | RF-KDS-12 |
| [US-117](./E-11-Inteligencia-de-Fluxo/US-117-Indicador-de-ocupacao-do-gargalo.md) | Indicador de ocupacao do gargalo | S | 8 | RF-KDS-08 |
| [US-118](./E-11-Inteligencia-de-Fluxo/US-118-Prazo-dinamico-calculado-pela-fila.md) | Prazo dinamico calculado pela fila | S | 8 | RF-PED-07 |
| [US-119](./E-11-Inteligencia-de-Fluxo/US-119-Mapa-de-calor-de-demanda.md) | Mapa de calor de demanda | S | 5 | RF-BI-08 |

### [E-12 · Financeiro de Gestao](./E-12-Financeiro-de-Gestao/README.md) — Fase 3

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-120](./E-12-Financeiro-de-Gestao/US-120-Receita-automatica-a-partir-de-pagamentos.md) | Receita automatica a partir de pagamentos | M | 5 | RF-FIN-01 |
| [US-121](./E-12-Financeiro-de-Gestao/US-121-Categorias-de-despesa-e-lancamentos.md) | Categorias de despesa e lancamentos | M | 5 | RF-FIN-02 |
| [US-122](./E-12-Financeiro-de-Gestao/US-122-Custos-fixos-recorrentes.md) | Custos fixos recorrentes | M | 5 | RF-FIN-03 |
| [US-123](./E-12-Financeiro-de-Gestao/US-123-Folha-de-pagamento.md) | Folha de pagamento | M | 8 | RF-FIN-04 |
| [US-124](./E-12-Financeiro-de-Gestao/US-124-CMV-custo-de-pessoal-e-prime-cost.md) | CMV custo de pessoal e prime cost | M | 8 | RF-FIN-05 |
| [US-125](./E-12-Financeiro-de-Gestao/US-125-Ponto-de-equilibrio.md) | Ponto de equilibrio | M | 5 | RF-FIN-06 |
| [US-126](./E-12-Financeiro-de-Gestao/US-126-Fluxo-de-caixa-realizado-e-projetado.md) | Fluxo de caixa realizado e projetado | S | 8 | RF-FIN-07 |
| [US-127](./E-12-Financeiro-de-Gestao/US-127-Resultado-do-periodo-com-composicao.md) | Resultado do periodo com composicao | M | 8 | RF-FIN-08 |
| [US-128](./E-12-Financeiro-de-Gestao/US-128-Exportacao-para-o-contador.md) | Exportacao para o contador | S | 3 | RF-FIN-09 |

### [E-13 · Delivery Proprio](./E-13-Delivery-Proprio/README.md) — Fase 4

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-130](./E-13-Delivery-Proprio/US-130-Canal-publico-de-pedido-com-marca-propria.md) | Canal publico de pedido com marca propria | M | 13 | RF-DEL-01 |
| [US-131](./E-13-Delivery-Proprio/US-131-Zonas-de-entrega-e-taxa.md) | Zonas de entrega e taxa | M | 8 | RF-DEL-02 |
| [US-132](./E-13-Delivery-Proprio/US-132-Prazo-dinamico-ao-cliente-de-delivery.md) | Prazo dinamico ao cliente de delivery | M | 5 | RF-DEL-03 |
| [US-133](./E-13-Delivery-Proprio/US-133-Acompanhamento-de-status-pelo-cliente.md) | Acompanhamento de status pelo cliente | M | 5 | RF-DEL-04 |
| [US-134](./E-13-Delivery-Proprio/US-134-Pagamento-online-integrado.md) | Pagamento online integrado | M | 13 | RF-CXA-09 |
| [US-135](./E-13-Delivery-Proprio/US-135-Endereco-salvo-e-repetir-pedido.md) | Endereco salvo e repetir pedido | S | 5 | RF-DEL-05 |
| [US-136](./E-13-Delivery-Proprio/US-136-Atribuicao-e-app-do-entregador.md) | Atribuicao e app do entregador | M | 8 | RF-DEL-06, RF-DEL-07, RF-DEL-10 |
| [US-137](./E-13-Delivery-Proprio/US-137-Aviso-de-pedido-proximo-de-sair.md) | Aviso de pedido proximo de sair | S | 3 | RF-DEL-08 |
| [US-138](./E-13-Delivery-Proprio/US-138-Agrupamento-de-entregas-proximas.md) | Agrupamento de entregas proximas | C | 8 | RF-DEL-09 |

### [E-14 · Plataforma em Escala](./E-14-Plataforma-em-Escala/README.md) — Fase 5

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-140](./E-14-Plataforma-em-Escala/US-140-Painel-de-instalacoes-com-saude.md) | Painel de instalacoes com saude | M | 8 | RF-PLT-07 |
| [US-141](./E-14-Plataforma-em-Escala/US-141-Provisionamento-autoatendido.md) | Provisionamento autoatendido | M | 8 | RF-PLT-05 |
| [US-142](./E-14-Plataforma-em-Escala/US-142-Modelos-por-tipo-de-negocio.md) | Modelos por tipo de negocio | S | 8 | RF-PLT-06 |
| [US-143](./E-14-Plataforma-em-Escala/US-143-Dominio-proprio-por-cliente.md) | Dominio proprio por cliente | S | 8 | RF-PLT-03 |
| [US-144](./E-14-Plataforma-em-Escala/US-144-Importacao-de-cardapio-por-planilha.md) | Importacao de cardapio por planilha | S | 5 | RF-CAT-12 |
| [US-145](./E-14-Plataforma-em-Escala/US-145-Acesso-de-suporte-auditado.md) | Acesso de suporte auditado | M | 5 | RF-PLT-08 |
| [US-146](./E-14-Plataforma-em-Escala/US-146-Atualizacao-controlada-do-parque.md) | Atualizacao controlada do parque | M | 5 | — |

### [E-15 · Gestão Geral da Plataforma](./E-15-Gestao-Geral-da-Plataforma/README.md) — Fase 5

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-150](./E-15-Gestao-Geral-da-Plataforma/US-150-Estrutura-e-navegacao-do-painel-de-plataforma.md) | Estrutura e navegação do painel de plataforma | M | 5 | RF-PLT-09 |
| [US-151](./E-15-Gestao-Geral-da-Plataforma/US-151-Diretorio-de-estabelecimentos-com-busca-e-filtros.md) | Diretório de estabelecimentos com busca e filtros | M | 8 | RF-PLT-10 |
| [US-152](./E-15-Gestao-Geral-da-Plataforma/US-152-Visao-360-e-acesso-aos-modulos-do-estabelecimento.md) | Visão 360 e acesso aos módulos do estabelecimento | M | 8 | RF-PLT-11 |
| [US-153](./E-15-Gestao-Geral-da-Plataforma/US-153-Ciclo-de-vida-do-estabelecimento.md) | Ciclo de vida do estabelecimento | M | 8 | RF-PLT-12 |
| [US-154](./E-15-Gestao-Geral-da-Plataforma/US-154-Gestao-de-planos-e-configuracao-comercial.md) | Gestão de planos e configuração comercial | S | 8 | RF-PLT-13 |
| [US-155](./E-15-Gestao-Geral-da-Plataforma/US-155-Proprietarios-usuarios-iniciais-e-convites.md) | Proprietários, usuários iniciais e convites | M | 8 | RF-PLT-14 |
| [US-156](./E-15-Gestao-Geral-da-Plataforma/US-156-Recuperacao-do-provisionamento-e-token-de-instalacao.md) | Recuperação do provisionamento e token de instalação | M | 8 | RF-PLT-15 |
| [US-157](./E-15-Gestao-Geral-da-Plataforma/US-157-Central-operacional-auditoria-e-atalhos-de-suporte.md) | Central operacional, auditoria e atalhos de suporte | M | 5 | RF-PLT-16 |

## 5. Ordem de leitura sugerida

**Para começar a implementar:** `E-00 → E-01 → E-03 → E-02 → E-04 → E-05 → E-06 → E-07`

A ordem privilegia **fechar o fluxo operacional completo antes da sincronização**. O sync (E-06) é a peça mais arriscada do MVP — só faz sentido construí-lo quando já existe fluxo real gerando eventos reais para sincronizar (doc. 02, seção 13).

**Para o PO e o cliente:** comece pelos README de cada épico; eles trazem objetivo, valor e riscos sem o detalhe técnico.

**Para QA:** as seções 4 (Gherkin) e 12 (testes) de cada história, cruzadas com o `../10-Estrategia-de-Testes-e-Qualidade.md`.

## 6. Definition of Ready

Uma história só entra em sprint quando:

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

## 7. Definition of Done

Uma história só é concluída quando:

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO

## 8. Pendências que atravessam o backlog

Estas questões continuam abertas e afetam múltiplas histórias. Enquanto não forem resolvidas, as histórias marcadas permanecem com escopo estimado, não confirmado.

| # | Pendência | Histórias afetadas | Impacto |
|---|---|---|---|
| 1 | **Emissão fiscal (NFC-e / SAT)** — RN-023 | US-057 | Bloqueia lançamento em produção legal |
| 2 | Propriedade do produto e modelo comercial | US-002, US-154, E-14, E-15 | Contrato e estratégia |
| 3 | Prazo, orçamento e priorização do cliente | Todo o roadmap | Dimensionamento |
| 4 | Modalidade de integração de pagamento (TEF × gateway) | US-058, US-134 | Arquitetura de pagamento |
| 5 | Contingência para falha do servidor local | US-006, US-034 | Risco operacional crítico |
| 6 | Integração com iFood | E-13 | Escopo da Fase 4 |
| 7 | App de frios — produto separado ou módulo | Fora do escopo atual | Escopo geral |
| 8 | Momento da baixa de estoque — RN-007 | US-103 | Modelo de custo |
| 9 | Tratamento de insumo em item cancelado — RN-008 | US-033, US-105 | CMV e perda |
| 10 | Regra de precificação de meio a meio — RN-009 | US-013 | Receita e margem |
| 11 | Política de taxa de serviço — RN-010 | US-053, US-027 | Fechamento de conta |
| 12 | Quem autoriza cancelamento e desconto | US-033, US-054 | Governança operacional |
| 13 | Indicadores prioritários do painel v1 | E-07 | Escopo da Fase 1 |
| 14 | Separação da cozinha por praças | US-017, US-042 | Desenho do KDS |
| 15 | Entregadores próprios ou terceirizados | US-136 | Escopo da Fase 4 |
| 16 | Metas de 10 e 25 min: objetivo ou requisito? | US-072, US-132 | Compromisso contratual |

## 9. Materiais pendentes do cliente

Itens da lista da Visão Geral (seção 20.2) que bloqueiam histórias específicas:

| Material | Bloqueia |
|---|---|
| Cardápio completo com preços (salão e delivery) | US-010, US-011, US-014 |
| Fotos dos produtos | US-010 |
| Lista de insumos e fornecedores | US-100 |
| Relatório de sobras e compras | US-104 |
| Planta ou quantidade de mesas | US-020 |
| Lista de funcionários e funções | US-004, US-123 |
| Identidade visual da marca | US-003 |
| Contato do contador | US-128 |
| Faturas Cielo e Mercado Pago | US-058, US-134 |
| Infraestrutura de rede e internet da loja | US-006, US-031 |

## 10. Rastreabilidade de requisitos

Dos **151 requisitos funcionais** do PRD, **134 têm história dedicada** neste pacote. Os 17 restantes são requisitos de prioridade **C** ou de fase posterior que o backlog consolidado (doc. 07) também não detalhou — estão referenciados nas seções *Fora desta história* das US relacionadas, mas ainda não foram escritos como história própria.

| RF | Descrição resumida | Prio | Onde está referenciado |
|---|---|---|---|
| RF-CAT-10 | Cardápio com disponibilidade por horário/dia | C | US-010 (fora do escopo) |
| RF-CAT-11 | Combos e promoções | C | US-010, US-014 (fora do escopo) |
| RF-SAL-09 | Transferir itens entre mesas; unir e separar mesas | S | US-020, US-022, US-027 |
| RF-SAL-12 | Avaliação do cliente ao fechar a conta | S | US-026 (fora do escopo) |
| RF-SAL-13 | Identificar qual cliente fez cada pedido na mesa | C | US-021, US-024 |
| RF-KDS-11 | Registrar refazimento de item (re-fire) com motivo | S | US-033, US-041, US-046 |
| RF-KDS-15 | Sugerir agrupamento de itens idênticos | C | US-043, US-116 |
| RF-CXA-11 | Conciliar recebimentos eletrônicos com o registrado | C | US-052, US-058 |
| RF-CXA-13 | Estorno com motivo e autorização | S | US-033, US-052, US-134 |
| RF-EST-10 | Sugerir lista de compras por consumo e cobertura | S | US-100, US-104, US-108 |
| RF-FIN-10 | Registrar custo de taxa de cartão por transação | S | US-058, US-073, US-120 |
| RF-BI-10 | Definir metas e acompanhar realizado × meta | S | E-07 (fora do escopo atual) |
| RF-BI-13 | Exportar qualquer visão em planilha/PDF | S | E-07 (fora do escopo atual) |
| RF-ALT-05 | Silenciar tipos de alerta por usuário | S | US-081, US-083 |
| RF-ALT-06 | Medir taxa de alertas ignorados por tipo | C | US-080, US-083 |
| RF-OFF-08 | Cache de cardápio no dispositivo para contingência | S | US-021, US-034 |
| RF-DEL-11 | Pausar canal automaticamente por fila excessiva | C | US-132 (implementado parcialmente) |

> **Recomendação:** escrever essas histórias no refinamento da fase correspondente. Nenhuma delas bloqueia o MVP, e antecipá-las agora consumiria esforço em escopo que ainda pode mudar.

---

*Pacote de user stories do projeto 004_DonaBetinha. Replay Studio.*
