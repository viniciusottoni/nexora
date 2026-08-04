# US-146 · Atualizacao controlada do parque

|  |  |
|---|---|
| **Épico** | [E-14 · Plataforma em Escala](./README.md) |
| **Fase** | 5 — Produto replicável em escala |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Fase 5 |
| **Requisitos funcionais** | — |
| **Regras de negócio** | — |
| **ADRs** | ADR-019, ADR-029, ADR-033 |
| **Eventos** | — |
| **Aplicações** | infra/edge, web-platform, api-cloud |
| **Autoridade do dado** | Nuvem (publicação) → Local (aplicação) |

---

## 1. História

> **Como** administrador da plataforma (P9),
> **quero** atualizar as instalações de forma controlada, com rollback automático,
> **para** que uma atualização ruim não derrube a operação de vários clientes ao mesmo tempo.

## 2. Contexto e motivação

Atualizações são **puxadas** pelo edge, nunca empurradas cegamente (doc. 02, seção 9.3). O fluxo é: janela configurável fora do horário de operação, download, migration, health check e rollback automático se falhar.

É o que mitiga o risco T7 — deriva de versão entre lojas — sem criar o risco maior de derrubar o parque inteiro com uma release ruim.

## 3. Escopo

### 3.1 Dentro desta história

- Publicação de versão com liberação gradual
- Janela de atualização configurável por instalação
- Download, migration, health check e ativação
- Rollback automático em caso de falha
- Monitoramento da versão de cada instalação
- Bloqueio de atualização em instalação com pendências de sincronização

### 3.2 Fora desta história

- Atualização de aplicações web (feita por deploy normal)
- Atualização de sistema operacional do edge

## 4. Critérios de aceite

```gherkin
Funcionalidade: Atualização controlada do parque

  Cenário: Atualização na janela configurada
    Dado a janela configurada entre 4h e 6h
    Quando uma versão nova for publicada
    Então a instalação deve atualizar dentro da janela
    E a operação não deve ser interrompida

  Cenário: Rollback automático
    Dado uma atualização cujo health check falha
    Quando a falha for detectada
    Então deve haver rollback automático para a versão anterior
    E a plataforma deve ser alertada

  Cenário: Liberação gradual
    Dado uma versão nova publicada
    Quando a liberação for gradual
    Então deve atingir um subconjunto primeiro
    E só prosseguir se não houver falha

  Cenário: Instalação com pendência de sincronização
    Dado uma instalação com eventos pendentes acima do limiar
    Quando a janela chegar
    Então a atualização deve ser adiada
    E a plataforma deve ser informada

  Cenário: Migration compatível
    Dado uma migration incluída na atualização
    Quando for aplicada
    Então deve ser compatível com a versão anterior
    E o rollback deve permanecer possível

  Cenário: Backup antes da atualização
    Dado uma atualização prestes a iniciar
    Quando começar
    Então um backup deve ser gerado antes da migration
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-005 | A operação local não depende de internet | Atualização acontece fora do horário de operação |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
POST /v1/platform/releases
{ "version": "1.5.0", "rolloutPercent": 10,
  "notes": "..." }

GET /v1/sync/health
→ { "expectedVersion": "1.5.0", "configVersion": 88 }

# No edge, dentro da janela:
# 1. verifica versão esperada
# 2. verifica pendências de sincronização
# 3. gera backup
# 4. baixa imagens
# 5. aplica migration
# 6. health check
# 7. ativa ou reverte

GET /v1/platform/releases/{version}/rollout
→ { "total": 12, "updated": 3, "failed": 0, "pending": 9 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `release` | Versão publicada | `version`, `rollout_percent`, `published_at` |
| `edge_installation` | Versão e histórico | `version`, `target_version`, `last_update_at`, `last_update_status` |

## 9. Comportamento offline

A atualização exige internet para o download, mas acontece fora do horário de operação. Se a conexão falhar no meio, o rollback automático garante que a loja continue operando na versão anterior.

O backup gerado antes da migration (ADR-033) é a última linha de defesa.

## 10. Interface e experiência

- Progresso da liberação visível no painel de plataforma
- Falha de atualização como alerta prioritário
- Janela configurável por instalação, respeitando o horário real de cada cliente
- Cliente informado de que a atualização ocorreu, sem detalhe técnico

## 11. Métricas, alertas e observabilidade

- Distribuição de versões no parque
- Taxa de sucesso de atualização
- Tempo médio de propagação de uma versão
- Rollbacks por versão — indicador direto de qualidade da release

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Atualização completa em ambiente equivalente ao da loja |
| Integração | Rollback automático em falha de health check |
| Integração | Atualização adiada com pendências de sincronização |
| Caos | Queda de conexão no meio da atualização deixa a instalação operante |
| Restauração | Backup pré-atualização restaurável |

## 13. Dependências

**Depende de:** US-006, US-140  
**Habilita:** —

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

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
- [ ] Rollback automático testado em instalação real, não apenas em ambiente de teste

## 15. Riscos, premissas e pendências

- **Risco T7 (doc. 02)** — deriva de versão entre lojas. A atualização automática é a mitigação; o monitoramento de versão é a detecção.
- Uma release ruim liberada para todo o parque de uma vez é o pior cenário do modelo. Liberação gradual é obrigatória, não opcional.

---

*US-146 · Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*