# ADR-034 · Relógio, sequência e tolerância a desvio

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-006, ADR-007, ADR-012, ADR-016, ADR-018 |
| **Requisitos afetados** | RF-OFF-04, RN-020, RNF-OFF-04 |

---

## Contexto

Em sistema distribuído, tempo é a origem de defeitos sutis e difíceis de rastrear. Aqui o problema é agravado por três fatores:

1. O edge pode ficar **horas offline** e depois sincronizar — o horário de chegada não é o horário do fato
2. O relógio do edge pode estar **errado** (mini-PC sem NTP, bateria de CMOS fraca, fuso mal configurado)
3. **Toda a proposta de valor do produto é medição de tempo** — se o relógio mentir, o produto mente

Uma regra já foi estabelecida (RN-020): métrica de horário usa sempre `occurredAt`. Este ADR define como isso é garantido na prática, e o que fazer quando o relógio estiver errado.

## Decisão

**Dois carimbos de tempo obrigatórios em todo evento, sequência monotônica por instalação, sincronização NTP no edge e detecção ativa de desvio de relógio.**

## Detalhamento

### Dois carimbos

| Campo | Significado | Quem atribui | Uso |
|---|---|---|---|
| `occurred_at` | Momento do fato | **Origem** (dispositivo ou edge) | **Todas as métricas** |
| `recorded_at` | Momento da chegada | Destino (nuvem) | Diagnóstico de sync |

```
20h03  pedido criado offline   → occurred_at = 20h03
21h15  sincronizado            → recorded_at = 21h15

Relatório por faixa horária: aparece às 20h  ✔
```

Confundir os dois faz com que uma loja com internet ruim tenha todos os picos deslocados — e o dono conclua coisas erradas sobre o próprio negócio.

### Origem do `occurred_at`

```
Ação do usuário no dispositivo
   └─► cliente envia X-Occurred-At (relógio do dispositivo)
         └─► edge valida contra o próprio relógio
               ├─ diferença ≤ 2 min  → aceita o do cliente
               └─ diferença > 2 min  → usa o do edge + registra o desvio
```

O relógio do dispositivo é conveniente (mais próximo do fato) mas menos confiável. A validação contra o edge dá o melhor dos dois.

### Sequência monotônica

```sql
CREATE SEQUENCE device_seq;   -- por instalação
```

| Propriedade | Uso |
|---|---|
| Estritamente crescente | Ordem de aplicação na sincronização |
| Independente do relógio | Imune a ajuste de horário e horário de verão |
| Por instalação | Não exige coordenação global |

**A sequência define a ordem de aplicação; o `occurred_at` define a posição no tempo.** São coisas diferentes e ambas necessárias.

### Sincronização de relógio no edge

```
chrony/ntpd configurado no install.sh
  ├─ servidores: pool.ntp.br
  ├─ verificação a cada 5 min
  └─ desvio reportado no heartbeat
```

### Detecção de desvio

| Verificação | Ação |
|---|---|
| Edge compara o próprio relógio com o `Date` da resposta da nuvem a cada sync | Registra a diferença |
| Desvio > 30 s | Alerta à Replay |
| Desvio > 5 min | Alerta ao gestor e à Replay; sinalizado no painel |
| Desvio > 1 h | Sync continua, mas os eventos são **marcados como suspeitos** |

Eventos marcados aparecem em relatório de qualidade de dado. Não são descartados — descartar dado de operação real seria pior —, mas o dono precisa saber que aquele período tem medição duvidosa.

### Ajuste de relógio para trás

Se o relógio voltar (correção de NTP após estar adiantado), pode haver eventos com `occurred_at` no futuro relativo. Tratamento:

- A sequência garante a ordem correta de aplicação
- Evento com `occurred_at` mais de 5 min à frente do relógio da nuvem é **clampeado** ao horário de recepção e marcado
- O valor original fica preservado em `payload.originalOccurredAt`, para auditoria

### Duração é calculada, nunca armazenada como número solto

```
tempo_de_producao = ready_at − fired_at
```

Guardar "12 minutos" em vez dos dois carimbos impediria recalcular, auditar e corrigir. Todos os intervalos derivam dos carimbos.

### Monotonicidade em medição local

Para cronômetros de tela (KDS), usa-se `performance.now()` — que não retrocede — e não `Date.now()`. Assim, um ajuste de relógio no meio do serviço não faz o cronômetro do KDS pular para trás na frente do cozinheiro.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Um único carimbo (horário de chegada) | Simples | Métrica por horário fica errada em toda loja com internet instável | Falha no requisito central |
| Confiar apenas no relógio do dispositivo | Mais próximo do fato | Relógio de celular pode estar muito errado | Sem validação, dado fica arbitrário |
| Relógio lógico (Lamport, vetorial) | Ordem causal correta | Não dá horário real; o produto precisa de horário de parede | Métrica de negócio exige tempo real |
| Rejeitar eventos com relógio divergente | Dado sempre confiável | Perderia operação real de uma loja com NTP quebrado | Perder venda é pior que medir mal |
| TrueTime / relógio com incerteza | Correção teórica | Exige infraestrutura indisponível | Desproporcional |

## Consequências

**Positivas**

- Métrica por faixa horária correta mesmo com sincronização atrasada
- Ordem de aplicação garantida, independente de relógio
- Desvio de relógio é detectado e comunicado, não silencioso
- Auditoria preserva o valor original mesmo quando há correção

**Negativas**

- Dois carimbos em toda parte — mais colunas, mais disciplina
- Regra de validação e clamp adiciona complexidade
- Evento marcado como suspeito exige interpretação humana

**Mitigações**

- Helper único em `packages/domain/time` — nenhum código lida com isso manualmente
- Lint bloqueia uso direto de `Date.now()` em código de domínio
- Relatório de qualidade de dado destaca períodos com eventos suspeitos

## Como validar

- Teste D-06: evento criado às 20h03 e sincronizado às 21h15 é contabilizado às 20h
- Cenário C-05: relógio do edge adiantado 10 min → desvio detectado e alertado
- Teste: evento com `occurred_at` no futuro é clampeado, com valor original preservado
- Teste: cronômetro do KDS não retrocede após ajuste de relógio

## Revisitar quando

- Surgir necessidade de ordenação causal entre lojas diferentes
