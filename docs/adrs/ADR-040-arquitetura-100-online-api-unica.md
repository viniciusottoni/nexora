# ADR-040 · Arquitetura 100% online, API única

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 06/08/2026 |
| **Decisores** | Tech Lead, PO, Cliente |
| **Substitui** | ADR-001, ADR-007, ADR-019, ADR-027, ADR-033 |
| **Substituído por** | — |
| **Relacionados** | ADR-008, ADR-009, ADR-010, ADR-011, ADR-041 |
| **Requisitos afetados** | RF-OFF-01 a 09 (removidos), RF-PLT-05, RNF-DIS-01 a 08, RNF-PER-01/07/08, RNF-IMP-01 a 03 |

---

## Contexto

O ADR-001 registrou uma exigência categórica do cliente na descoberta: *"é necessário funcionar sem internet rodando em rede local as mesas, caixa e KDS"*. Toda a arquitetura — servidor local (edge) por loja, sincronização outbox/inbox com a nuvem, fila de ações offline no dispositivo, backup e recuperação de hardware físico distribuído — nasceu dessa exigência.

Essa exigência deixou de existir. Não por mudança técnica, mas por **mudança de foco de negócio**: o produto passa a competir diretamente com o cardápio web tradicional, e portanto opera 100% online, como qualquer produto dessa categoria. Confirmado pelo cliente: o projeto ainda está em fase de implementação — não há loja em produção com hardware de edge instalado, o que torna esta mudança uma revisão de arquitetura, não uma migração de sistema em operação.

O nome do produto muda de **Nexora** para **iMenu** no mesmo movimento — ver E-16/US-160 para o rebranding.

## Forças em jogo

| Força | Descrição |
|---|---|
| Simplicidade operacional | Uma API, um banco, um deploy — sem parque de hardware distribuído para manter |
| Custo | Elimina hardware por loja, backup físico, equipamento reserva e suporte de campo |
| Velocidade de entrega | Sem outbox, sem worker de sync, sem fila offline no cliente — menos superfície para construir e testar |
| Paridade competitiva | Concorrentes de cardápio web já operam 100% online; não há mais motivo de diferenciação por continuidade offline |
| Risco aceito | A operação para se a internet cair — mitigado apenas pelo modelo manual em papel, como hoje |

## Decisão

**O sistema passa a ser servido por uma única API (`iMenu.Api`), sem servidor local por loja e sem sincronização.** Todos os dispositivos operacionais (mesa, garçom, KDS, caixa, admin) falam diretamente com a nuvem, pela internet.

Não existe mais "edge" nem "cloud" como conceitos arquiteturais distintos — existe **iMenu.Api**, ponto único de autoridade para todo dado do sistema.

## Detalhamento

### Nova topologia

```
   Internet ──► iMenu.Api (multi-tenant, autoridade única)
                  ▲
        ┌─────────┼─────────┬─────────┬─────────┐
      Mesa      Garçom      KDS      Caixa     Admin
   (/table)    (/server)  (/kds)    (/pos)   (/admin)
```

Todo domínio de dado (pedido, mesa, catálogo, estoque, financeiro, configuração) tem **um único dono**: `iMenu.Api`. A distinção de autoridade "edge vs. nuvem" do ADR-001 deixa de existir — não porque foi resolvida, mas porque só resta uma parte.

### O que é removido

| Componente do modelo anterior | Situação |
|---|---|
| Servidor local (edge) por loja — `infra/edge/*` | Removido. Ver E-16/US-161 |
| `api-edge` (`Nexora.Api.Edge`) | Removido. Funcionalidade absorvida por `iMenu.Api` |
| `api-cloud` (`Nexora.Api.Cloud`) | Renomeado para `iMenu.Api` — não há mais distinção |
| Outbox transacional e worker de sync (ADR-007) | Removido — não há mais duas pontas para sincronizar |
| Pull de catálogo/configuração edge↔nuvem | Removido — configuração é lida direto da fonte única |
| Fila de ações offline no dispositivo (ADR-027) | Removida — sem servidor local, não há o que enfileirar localmente com garantia de entrega posterior (ver seção "Comportamento em queda de conexão") |
| Backup/recuperação de hardware de loja (ADR-033) | Removido — backup passa a ser o de um banco gerenciado em nuvem, prática padrão, sem ADR dedicado |
| Migrations para parque distribuído (ADR-019) | Removido — migração de schema volta a ser rotina de banco único, com as práticas usuais de zero-downtime deploy |
| Tabela `edge_installation` e `sync_cursor` | Removidas do modelo de dados (E-16/US-169) |
| RF-OFF-01 a 09 | Removidos do PRD — não há mais modo de operação offline a especificar |

### Comportamento em queda de conexão

**Não há mais fila de contingência.** Se o dispositivo perde conexão com `iMenu.Api`, a aplicação informa indisponibilidade e a operação retorna ao procedimento manual em papel até a conexão voltar — exatamente como acontece hoje com qualquer cardápio web comum. Nenhuma ação é enfileirada localmente para reenvio automático.

```
┌────────────────────────────────────────────┐
│ ⚠ Sem conexão com o servidor                │
│ Tente novamente em instantes.               │
│ Se persistir, use o controle em papel.      │
└────────────────────────────────────────────┘
```

Isso é uma escolha deliberada, não uma lacuna: a resiliência de rede deixou de ser requisito de produto (ver ADR-001, revogado). Adicionar qualquer fila de reenvio client-side reintroduziria a complexidade que esta ADR existe para eliminar, sem um requisito de negócio que a justifique.

### Metas de desempenho revisadas

O caminho pedido → KDS deixa de ser mesa → **edge local** → KDS (rede interna, latência de milissegundos) e passa a ser mesa → **internet** → `iMenu.Api` → internet → KDS. RNF-PER-01 é revisado de **< 2 s** para **< 10 s (p95)** — ver E-16/US-168 e documento 08.

RNF-PER-07 e RNF-PER-08 (metas de sincronização) são removidos — não há mais o que sincronizar.

### O que continua igual

- SignalR (ADR-011) continua sendo o canal de tempo real, agora apontando para `iMenu.Api` em vez do edge local — ver nota de revisão em ADR-011.
- `domain_event` e `audit_log` continuam existindo e sendo a base de auditoria (ADR-006) — não são específicos de sincronização, são log de domínio.
- Saldo de estoque derivado de movimentos (ADR-008) continua sendo a prática adotada, mas por razão diferente — ver nota de revisão em ADR-008.
- Multi-tenancy por RLS (ADR-004), autorização por PIN (revisado em ADR-041), theming em runtime (ADR-010, com resolução de tenant revisada) permanecem.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Manter edge, mas simplificar sincronização | Preserva o diferencial de continuidade offline | Não resolve o motivo real da mudança — o produto não compete mais nesse eixo | O requisito de negócio que justificava o edge não existe mais |
| Cache offline apenas para leitura (sem fila de escrita) | Meio-termo | Mantém complexidade de Service Worker/IndexedDB sem benefício claro de continuidade de venda | Cliente confirmou: contingência aceitável é "ficar sem o serviço", igual ao cardápio web hoje |
| Migração gradual (manter os dois modelos em paralelo por um tempo) | Reduz risco de corte abrupto | Não há loja em produção — não existe parque a migrar; manter os dois modelos custaria esforço sem benefício | Projeto ainda em implementação; corte direto é mais barato que transição |

## Consequências

**Positivas**

- Elimina o componente mais arriscado e caro do MVP original: o motor de sincronização (E-06) e tudo que dependia dele
- Elimina custo recorrente de hardware por loja, backup físico e suporte de campo
- Um único deploy, um único banco, um único pipeline — modelo de operação muito mais simples de sustentar com equipe pequena
- Remove um "novo modo de falha" inteiro (o servidor local podia quebrar; agora não existe para quebrar)

**Negativas**

- A operação para completamente se a internet da loja cair — o diferencial "a loja nunca para de vender" deixa de existir
- Latência pedido→KDS passa a depender de rede externa, fora do controle da operação da loja
- Nenhuma mitigação automática para queda de conexão — a resposta é 100% processo manual (papel)

**Mitigações**

- Nenhuma mitigação técnica é proposta — a queda de internet é tratada como exceção aceitável, não como cenário a resolver em software (decisão de negócio, não lacuna técnica)
- RNF-PER-01 revisado para uma meta realista de rede pública (10 s), evitando compromisso que a nova topologia não sustenta

## Como validar

- Nenhum diretório `infra/edge` ou pacote `Nexora.Api.Edge` remanescente no repositório
- `iMenu.Api` é o único ponto de entrada de todas as aplicações cliente
- RNF-PER-01 medido em produção contra a meta revisada (p95 < 10 s)
- Nenhuma tabela `edge_installation`/`sync_cursor` no schema
- Teste manual: desconectar a internet de um dispositivo cliente durante o uso — o app informa indisponibilidade, sem tentar operar localmente

## Revisitar quando

- O modelo de negócio voltar a exigir continuidade de operação sem internet (ex.: expansão para regiões de conectividade muito instável)
- Escala do produto tornar viável reintroduzir cache regional/edge por razões de latência (não de continuidade), como decisão de infraestrutura, não de produto

---

*ADR-040 · Pacote 004_DonaBetinha · Replay Studio.*
