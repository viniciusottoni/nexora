# US-168 · Ajuste de metas de desempenho online

|  |  |
|---|---|
| **Épico** | [E-16 · iMenu Online](./README.md) |
| **Fase** | 0 — Fundação da plataforma (revisão) |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 0 (validação, ao final) |
| **Requisitos funcionais** | — |
| **Regras de negócio** | — |
| **ADRs** | ADR-040, ADR-011 (revisado) |
| **Eventos** | — |
| **Aplicações** | `iMenu.Api`, `web-kds` |
| **Autoridade do dado** | — |

---

## 1. História

> **Como** time de engenharia e QA,
> **quero** metas de desempenho realistas para uma arquitetura 100% online,
> **para** medir o sistema contra um alvo alcançável, em vez de um número herdado de uma topologia que não existe mais.

## 2. Contexto e motivação

RNF-PER-01 (pedido → KDS) era **< 2 s**, medido num caminho mesa → **servidor local na mesma rede** → KDS. Sem edge, o caminho passa a ser mesa → internet → `iMenu.Api` → internet → KDS — uma topologia genuinamente diferente, não a mesma topologia mais devagar.

Confirmado nesta revisão: a meta passa para **< 10 s**, medida no KDS. Esta história formaliza a nova meta, atualiza os documentos que a referenciam e valida que ela é alcançável com a arquitetura de US-161.

## 3. Escopo

### 3.1 Dentro desta história

- Atualizar RNF-PER-01 no documento 08 para `< 10 s (p95)` — **já feito nesta rodada**, aplicado diretamente no documento 08 durante a estruturação deste épico
- Remover RNF-PER-07 e RNF-PER-08 (metas de sincronização) — não fazem mais sentido sem outbox
- Revisar a matriz de alertas técnicos (documento 08, §7.1) removendo os limiares específicos de instalação/sincronização e ajustando "latência p95 pedido→KDS" para o novo alvo
- Medir, em ambiente de teste representativo (conexão 4G comum, não rede local), se a meta de 10 s é confortavelmente alcançável — não apenas assumida
- Ajustar qualquer critério de aceite em outras histórias (E-04/KDS, por exemplo) que ainda cite "2 segundos" como meta

### 3.2 Fora desta história

- Implementação do SignalR/polling em si (já existente, ADR-011)
- Otimização de performance além do necessário para atingir a nova meta

## 4. Critérios de aceite

```gherkin
Funcionalidade: Meta de desempenho revisada

  Cenário: Meta documentada
    Dado o documento 08 (RNF)
    Quando RNF-PER-01 for consultado
    Então deve indicar "< 10 s (p95)", não mais "< 2 s"

  Cenário: Meta medida e validada
    Dado o ambiente de teste com condição de rede pública representativa (4G comum)
    Quando o caminho pedido → KDS for medido ponta a ponta
    Então o p95 deve ficar abaixo de 10 s

  Cenário: Metas de sincronização removidas
    Dado o documento 08
    Quando RNF-PER-07 e RNF-PER-08 forem consultados
    Então devem estar marcados como removidos, não permanecer como meta ativa não alcançável
```

## 5. Regras de negócio aplicáveis

_Não se aplica._

## 6. Eventos emitidos e consumidos

_Não se aplica._

## 7. Contrato de API

_Não se aplica diretamente._

## 8. Modelo de dados

_Não se aplica._

## 9. Comportamento offline

_Não se aplica — ver ADR-040._

## 10. Interface e experiência

_Não se aplica diretamente — impacto indireto: se a meta de 10 s não for confortavelmente atingida em teste real, pode ser necessário reconsiderar a experiência de espera no KDS (indicação de "pedido a caminho", por exemplo) — avaliar apenas se a medição real indicar necessidade._

## 11. Métricas, alertas e observabilidade

- Telemetria fim a fim `order.placed` → render no KDS, já prevista, agora medida contra o novo alvo
- Alerta de latência p95 pedido→KDS ajustado de "> 3 s" para um novo limiar coerente com a meta de 10 s (ex.: alertar acima de 8 s, antes de estourar o alvo)

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Carga | p95 do caminho pedido→KDS sob condição de rede pública representativa |
| Documental | RNF-PER-01/07/08 e a matriz de alertas do documento 08 refletindo a nova meta |
| Regressão | Nenhum critério de aceite de outra história ainda referenciando "2 segundos" |

## 13. Dependências

**Depende de:** US-161 (a meta só pode ser medida de verdade com `iMenu.Api` já consolidada)
**Habilita:** fechamento do Definition of Done do épico E-16

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Ambiente de teste com condição de rede pública representativa disponível

**DoD — a história só é concluída quando:**

- [ ] RNF-PER-01/07/08 atualizados no documento 08
- [ ] Meta medida em teste real, não apenas assumida
- [ ] Matriz de alertas técnicos ajustada
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- Se a medição real mostrar que 10 s é folgado demais ou apertado demais para a experiência desejada no KDS, a meta deve ser ajustada com base em dado, não mantida por ter sido a primeira estimativa desta história.
- RNF-PER-01 já foi atualizado diretamente no documento 08 durante a estruturação deste épico (06/08/2026) — esta história cobre a validação empírica, que ainda não foi feita.

---

*US-168 · Épico E-16 · Pacote 004_DonaBetinha · Replay Studio.*
