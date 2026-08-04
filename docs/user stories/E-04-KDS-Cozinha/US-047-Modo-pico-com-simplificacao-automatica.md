# US-047 · Modo pico com simplificacao automatica

|  |  |
|---|---|
| **Épico** | [E-04 · KDS Cozinha](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | C — Could have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 4 (se houver folga) |
| **Requisitos funcionais** | — |
| **Regras de negócio** | — |
| **ADRs** | — |
| **Eventos** | — |
| **Aplicações** | web-kds |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** pizzaiolo (P3),
> **quero** que a tela simplifique sozinha quando a fila explode,
> **para** que eu consiga ler o essencial mesmo com 30 pedidos pendentes.

## 2. Contexto e motivação

No pico, a fila cresce e os cartões encolhem até ficarem ilegíveis — exatamente no momento em que a legibilidade importa mais.

O modo pico reduz a informação por cartão ao essencial (código, produto, tempo) e prioriza os mais antigos e os mais atrasados. É um recurso de mitigação, não uma funcionalidade central — daí a prioridade C.

## 3. Escopo

### 3.1 Dentro desta história

- Ativação automática por limiar de fila configurável
- Redução do cartão ao essencial: código, produto, quantidade e tempo
- Priorização visual dos mais antigos e dos críticos
- Ativação e desativação manual
- Indicação clara de que o modo está ativo

### 3.2 Fora desta história

- Prioridade dinâmica calculada (US-116, Fase 2)
- Sugestão de agrupamento (Fase 3)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Modo pico

  Cenário: Ativação automática
    Dado o limiar de modo pico configurado em 20 itens
    Quando a fila atingir 20 itens pendentes
    Então o modo pico deve ativar automaticamente
    E deve haver indicação visual de que está ativo

  Cenário: Informação reduzida
    Dado o modo pico ativo
    Quando os cartões forem exibidos
    Então devem mostrar apenas código, produto, quantidade e tempo
    E observações devem ficar acessíveis por um toque

  Cenário: Desativação automática
    Dado o modo pico ativo com a fila caindo para 12 itens
    Quando o limiar for cruzado para baixo com margem
    Então o modo deve desativar automaticamente
    E não deve oscilar entre os dois modos

  Cenário: Sobreposição manual
    Dado o modo pico ativo automaticamente
    Quando o operador desativá-lo manualmente
    Então deve permanecer desativado até o fim do turno
```

## 5. Regras de negócio aplicáveis

_Não se aplica a esta história._

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
PATCH /v1/devices/{id}/preferences
{ "kds": { "peakMode": { "auto": true, "thresholdItems": 20,
                         "hysteresisItems": 5 } } }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `device` | Preferência de modo pico | `preferences.kds.peakMode` |

## 9. Comportamento offline

Comportamento integralmente do cliente; não depende de rede.

## 10. Interface e experiência

- Histerese obrigatória na troca de modo — alternar a cada item que entra e sai é pior que não ter o recurso
- Transição visual suave, sem reorganização brusca que faça o operador perder a referência
- Observação acessível em um toque, nunca escondida por completo

## 11. Métricas, alertas e observabilidade

- Tempo em modo pico por turno — indicador direto de sobrecarga da cozinha
- Tempo médio de produção dentro e fora do modo pico

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Histerese impede oscilação entre modos |
| Integração | Ativação e desativação automáticas nos limiares corretos |
| Usabilidade | Legibilidade mantida com 30 itens na fila |

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

- Prioridade C — só entra se houver folga na sprint. A mitigação básica de legibilidade já está na US-040.

---

*US-047 · Épico E-04 · Pacote 004_DonaBetinha · Replay Studio.*