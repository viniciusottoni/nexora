# US-169 · Migração do modelo de dados

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 0 |
| **Requisitos funcionais** | — |
| **Regras de negócio** | — |
| **ADRs** | ADR-040 |
| **Eventos** | — |
| **Aplicações** | `iMenu.Api` (camada de dados) |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** time de engenharia,
> **quero** um schema sem tabelas e campos que só faziam sentido com edge e sincronização,
> **para** que o modelo de dados reflita exatamente a arquitetura atual, sem coluna morta confundindo quem ler o schema depois.

## 2. Contexto e motivação

O modelo de dados atual (`domain/01-Plataforma-e-Identidade.md`) inclui `edge_installation` (registro de cada servidor local por loja) e referências a `sync_cursor`. Sem edge, essas estruturas não têm mais o que representar.

Como o projeto ainda está em implementação — sem loja em produção com dado real de instalação — esta é uma remoção de schema, não uma migração de dados de produção.

## 3. Escopo

### 3.1 Dentro desta história

- Remover a tabela `edge_installation` do ERD e do DDL (`domain/01-Plataforma-e-Identidade.md`) — **feito nesta rodada**
- Remover a relação `store ||--o| edge_installation` do ERD — **feito nesta rodada**
- Remover a constraint `uq_edge_store` e a linha correspondente na tabela "Regras de integridade" — **feito nesta rodada**
- Remover/avaliar a tabela `sync_cursor` (referenciada em US-006 original) — **pendente**, confirmar se chegou a ser modelada em outro documento de domínio além da menção em US-006
- Remover o campo `syncDelayMinutes` do exemplo de `thresholds` em `tenant_config` (documento de domínio 01) — **pendente**
- Escrever a migration real de remoção (`DROP TABLE edge_installation`, ajustes de FK) quando a implementação técnica desta história ocorrer — **pendente**, depende de US-161 estar em andamento

### 3.2 Fora desta história

- Remoção de código que lê/escreve essas tabelas (US-161)
- Qualquer dado de produção a migrar (não existe, ver contexto)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Modelo de dados sem resíduo de edge

  Cenário: Schema sem edge_installation
    Dado o schema após esta história
    Quando inspecionado
    Então a tabela edge_installation não deve existir
    E nenhuma FK ou índice órfão relacionado a ela deve permanecer

  Cenário: Documentação de domínio consistente
    Dado domain/01-Plataforma-e-Identidade.md
    Quando consultado
    Então o ERD e o DDL não devem referenciar edge_installation
    E a tabela "Regras de integridade" não deve citar uq_edge_store

  Cenário: Migration real aplicável
    Dado o ambiente de desenvolvimento
    Quando a migration de remoção for aplicada
    Então o banco deve ficar em estado consistente, sem erro de FK órfã
```

## 5. Regras de negócio aplicáveis

_Não se aplica._

## 6. Eventos emitidos e consumidos

_Não se aplica._

## 7. Contrato de API

_Não se aplica diretamente — endpoints que expunham `edge_installation` (ex.: `GET /v1/platform/installations`, usado por US-140) perdem sua fonte de dado original; ver US-140 para o encaminhamento dessa história._

## 8. Modelo de dados

| Tabela | Ação | Situação |
|---|---|---|
| `edge_installation` | Remover | Feito na documentação nesta rodada; migration real pendente |
| `sync_cursor` | Avaliar e remover se existir | Pendente |
| `tenant_config.thresholds.syncDelayMinutes` (campo JSON) | Remover do exemplo documentado | Pendente |

## 9. Comportamento offline

_Não se aplica — ver ADR-040._

## 10. Interface e experiência

_Não se aplica._

## 11. Métricas, alertas e observabilidade

_Não se aplica diretamente — métricas derivadas de `edge_installation` (US-140, RNF-OBS-05/06) perdem a fonte; ver US-140._

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Migration | Aplicação da migration de remoção em ambiente limpo, sem erro |
| Documental | ERD e DDL de domain/01 consistentes com o schema real após a migration |
| Regressão | Nenhuma query ou caso de uso do backend ainda referencia `edge_installation` após US-161 |

## 13. Dependências

**Depende de:** US-161 (a migration real só faz sentido depois que o código para de escrever na tabela)
**Habilita:** US-140 (redesenho), fechamento de US-167

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Confirmado que `sync_cursor` não está modelada em nenhum outro documento além da menção em US-006

**DoD — a história só é concluída quando:**

- [ ] Migration de remoção escrita, testada e aplicada em ambiente de desenvolvimento
- [ ] `domain/01` consistente com o schema real (já ajustado nesta rodada, a confirmar contra a migration real)
- [ ] Nenhuma referência de código a `edge_installation`/`sync_cursor`
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- Como não há dado de produção, o risco de migração é mínimo — a única atenção real é garantir que nenhuma FK ou índice fique órfão após a remoção.
- **[PENDÊNCIA]** confirmar se `sync_cursor` chegou a ser formalmente modelada em algum documento de domínio além da menção em US-006, para garantir que a remoção seja completa.

---

*US-169 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
