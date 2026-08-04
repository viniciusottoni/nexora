# US-045 · Alerta sonoro de pedido novo e de atraso

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 4 |
| **Requisitos funcionais** | RF-KDS-13 |
| **Regras de negócio** | RN-003 |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-kds |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** ser avisado por som quando chega pedido novo ou quando algo atrasa,
> **para** que eu não precise ficar olhando a tela o tempo todo.

## 2. Contexto e motivação

A cozinha trabalha de costas para o monitor boa parte do tempo. Sem sinal sonoro, o pedido chega e fica parado até alguém olhar — e o tempo de fila, que é a métrica mais sensível, dispara.

O desafio é o ambiente: calor, ruído de coifa, conversa. O som precisa vencer o ruído sem virar poluição sonora — daí volume e timbre configuráveis, e agrupamento de avisos repetidos.

## 3. Escopo

### 3.1 Dentro desta história

- Sinal sonoro em pedido novo e em item que atinge o limiar crítico
- Volume e timbre configuráveis por dispositivo
- Agrupamento de sons em rajada de pedidos
- Modo silencioso com sinal visual reforçado
- Teste de som na configuração

### 3.2 Fora desta história

- Anúncio por voz
- Integração com campainha física

## 4. Critérios de aceite

```gherkin
Funcionalidade: Sinais sonoros do KDS

  Cenário: Pedido novo
    Dado o KDS em operação
    Quando um pedido novo chegar
    Então deve tocar o sinal de pedido novo uma vez

  Cenário: Atraso crítico
    Dado um item que ultrapassou o limiar crítico
    Quando o limiar for cruzado
    Então deve tocar o sinal de atraso
    E o cartão deve ficar vermelho

  Cenário: Rajada de pedidos
    Dado cinco pedidos confirmados em dois segundos
    Quando os eventos chegarem
    Então o sinal deve tocar uma vez, não cinco

  Cenário: Modo silencioso
    Dado o KDS configurado em modo silencioso
    Quando um pedido novo chegar
    Então nenhum som deve tocar
    E o sinal visual deve ser reforçado

  Cenário: Alerta repetido de atraso
    Dado um item em atraso crítico há vários minutos
    Quando o tempo continuar correndo
    Então o sinal deve repetir no intervalo configurado, não continuamente
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-003 | Cada transição gera alerta aos perfis envolvidos | O som é a entrega do alerta no contexto da cozinha |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
PATCH /v1/devices/{id}/preferences
{ "kds": { "sound": { "enabled": true, "volume": 0.8,
                      "newOrderTone": "CHIME",
                      "lateTone": "ALERT",
                      "lateRepeatSeconds": 60 } } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `device` | Preferência de som | `preferences.kds.sound` (JSONB) |
| `tenant_config` | Limiares que disparam o alerta | `thresholds.orderCriticalMinutes` |

## 9. Comportamento offline

Integralmente local; o som é disparado pelo próprio dispositivo ao receber o evento pela LAN.

## 10. Interface e experiência

- Sons curtos e distinguíveis entre si — pedido novo e atraso não podem soar parecido
- Volume alto o suficiente para vencer o ruído da coifa, testável na própria tela de configuração
- Repetição de atraso em intervalo, nunca contínua — som contínuo é desligado no primeiro dia
- Modo silencioso sempre disponível, com reforço visual compensatório

## 11. Métricas, alertas e observabilidade

- Tempo entre chegada do pedido e primeiro avanço (T1−T0) — deve cair com o sinal sonoro ativo
- Uso do modo silencioso por dispositivo — adoção alta indica som mal calibrado

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Agrupamento de sons em rajada |
| Integração | Som dispara em pedido novo e em limiar crítico |
| Usabilidade | Teste em ambiente real de cozinha, com ruído |

## 13. Dependências

**Depende de:** US-040  
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

## 15. Riscos, premissas e pendências

- Alerta sonoro excessivo é desligado pela equipe e o benefício se perde. Calibrar no piloto e medir a adoção do modo silencioso como sinal de alerta.

---

*US-045 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*