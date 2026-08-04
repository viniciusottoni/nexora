# US-042 · Filtro por praca de producao

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-06 |
| **Regras de negócio** | — |
| **ADRs** | ADR-012 |
| **Eventos** | — |
| **Aplicações** | web-kds, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** ver apenas os itens que eu mesmo vou preparar,
> **para** que eu não perca tempo lendo pedido que não é meu.

## 2. Contexto e motivação

Sem filtro por praça, o operador do forno vê pedido de refrigerante e o operador de bebidas vê pizza. O ruído faz a cozinha parar de olhar a tela — e o KDS deixa de cumprir sua função.

O filtro fica gravado por dispositivo: o KDS do forno abre sempre no forno, sem reconfiguração a cada turno.

## 3. Escopo

### 3.1 Dentro desta história

- Filtro por praça, persistido por dispositivo
- Modo múltiplas praças em uma tela, para cozinha pequena
- Modo todas as praças, para supervisão
- Indicação clara da praça ativa no cabeçalho
- Contagem de itens pendentes nas demais praças

### 3.2 Fora desta história

- Vínculo de operador a praça (fora do escopo do MVP)
- Balanceamento automático entre praças

## 4. Critérios de aceite

```gherkin
Funcionalidade: Filtro por praça

  Cenário: Filtro do KDS por praça
    Dado três praças cadastradas
    Quando o operador do forno abrir o KDS filtrado por Forno
    Então deve ver apenas os itens roteados para o forno

  Cenário: Persistência por dispositivo
    Dado um KDS configurado para a praça Forno
    Quando o dispositivo for reiniciado
    Então deve abrir novamente na praça Forno, sem reconfiguração

  Cenário: Cozinha pequena com praça única
    Dado um tenant com apenas uma praça cadastrada
    Quando o KDS abrir
    Então o filtro não deve ser exibido
    E todos os itens devem aparecer

  Cenário: Múltiplas praças na mesma tela
    Dado um KDS configurado para Forno e Montagem
    Quando a fila for exibida
    Então os itens das duas praças devem aparecer
    E devem ser distinguíveis pela cor da praça

  Cenário: Visão de supervisão
    Dado o gestor acessando o KDS em modo todas as praças
    Quando a fila for exibida
    Então deve ver a fila completa agrupada por praça
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/kds/queue?stationId=<forno>
GET /v1/kds/queue?stationId=<forno>,<montagem>
GET /v1/kds/queue                      # todas as praças

PATCH /v1/devices/{id}/preferences
{ "kds": { "stationIds": ["<forno>"], "layout": "GRID" } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `device` | Preferência persistida | `preferences` (JSONB) |
| `order_item` | Praça do item | `station_id` |
| `station` | Cor e nome para distinção visual | `color`, `name` |

## 9. Comportamento offline

Integralmente local; preferência do dispositivo guardada no edge e em cache no navegador.

## 10. Interface e experiência

- Praça ativa sempre visível no cabeçalho, em fonte grande
- Cor da praça consistente com o cadastro e com os relatórios
- Troca de praça protegida por confirmação — evita mudança acidental no meio do pico
- Contagem discreta de itens pendentes nas outras praças, sem competir com a fila principal

## 11. Métricas, alertas e observabilidade

- Volume de itens por praça e por hora — mapa de carga da cozinha
- Tempo médio de produção por praça, revelando qual etapa trava

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Filtro retorna apenas itens da praça correta |
| Integração | Preferência persiste após reinício do dispositivo |
| Integração | Praça única não exibe filtro |

## 13. Dependências

**Depende de:** US-017, US-040  
**Habilita:** US-117

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

## 15. Riscos, premissas e pendências

- A separação por praças é hipótese a validar com a equipe (Visão Geral 6.2). Se a cozinha da Dona Betinha operar sem separação real, o modo praça única precisa ser o padrão.

---

*US-042 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*