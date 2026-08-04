# E-11 · Inteligencia de Fluxo

|  |  |
|---|---|
| **Fase** | 2 — Custo e controle |
| **Histórias** | 5 |
| **Pontos** | 42 |
| **Sprints previstas** | Fase 2 — sprints 13 e 14 |
| **Aplicações afetadas** | web-kds, web-admin, api-edge, api-cloud |
| **Pacotes do monorepo** | packages/domain, packages/metrics |

---

## 1. Objetivo do épico

Transformar a cozinha de reativa em orquestrada. Enquanto o E-04 entrega a fila cronometrada, este épico entrega a **inteligência sobre a fila**: quando iniciar cada item para que o pedido saia junto, qual a ordem que minimiza atraso, onde está o gargalo real e qual prazo prometer de verdade.

O motor de regras de fluxo está especificado no documento 04, seção 7. A restrição de produto que atravessa todo o épico: **sistema que reordena sem explicar perde a confiança da cozinha na primeira semana** — toda priorização é exibida com o motivo e pode ser sobreposta pelo operador.

## 2. Valor entregue

- Itens do mesmo pedido saindo sincronizados, resolvendo a perda de qualidade por espera
- Ordem de produção que minimiza atraso, com motivo explicado
- Visibilidade da ocupação e da ociosidade do gargalo — o número que revela capacidade desperdiçada
- Prazo prometido ao cliente calculado pela fila real, não fixo
- Mapa de calor revelando onde está o pico e como dimensionar equipe

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-115](./US-115-Fire-time-com-sequenciamento-reverso.md) | Fire time com sequenciamento reverso | S | 13 | RF-KDS-09 |
| [US-116](./US-116-Prioridade-dinamica-explicavel.md) | Prioridade dinamica explicavel | S | 8 | RF-KDS-12 |
| [US-117](./US-117-Indicador-de-ocupacao-do-gargalo.md) | Indicador de ocupacao do gargalo | S | 8 | RF-KDS-08 |
| [US-118](./US-118-Prazo-dinamico-calculado-pela-fila.md) | Prazo dinamico calculado pela fila | S | 8 | RF-PED-07 |
| [US-119](./US-119-Mapa-de-calor-de-demanda.md) | Mapa de calor de demanda | S | 5 | RF-BI-08 |

## 4. Ordem de execução recomendada

1. US-117 — indicador de ocupação do gargalo (base de diagnóstico)
2. US-115 — fire time e sequenciamento reverso
3. US-118 — prazo dinâmico
4. US-116 — prioridade dinâmica explicável
5. US-119 — mapa de calor de demanda

## 5. Dependências do épico

**Depende de:** E-01, E-04, E-07  
**Habilita:** E-13

## 6. Definition of Done do épico

- [ ] Fire time calculado e exibido no KDS, com saída sincronizada validada na prática
- [ ] Prioridade dinâmica sempre acompanhada do motivo e sobreponível pelo operador
- [ ] Ocupação e ociosidade do gargalo medidas e visíveis
- [ ] Prazo dinâmico melhorando o OTD em relação ao prazo fixo
- [ ] Validação com a cozinha real antes de considerar concluído

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Cozinha rejeitar o sequenciamento por não entender a lógica | Alta | Alto | Ordem sempre explicada e sobreponível; validar com a equipe antes de ativar por padrão |
| Fire time mal calibrado atrasar itens em vez de sincronizar | Média | Alto | Depende de tempo de preparo calibrado (US-016); ativar só após 30 dias de dado real |
| Complexidade não gerar ganho perceptível | Média | Médio | Medir OTD e tempo de expedição antes e depois; reverter se não houver ganho |

---

*Épico E-11 · Pacote 004_DonaBetinha · Replay Studio.*