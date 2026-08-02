---
title: US-238 — Determinar o dia do programa por rotação sobre o histórico (último dia concluído)
sidebar_position: 238
---

# US-238 — Determinar o dia do programa por rotação sobre o histórico (último dia concluído)

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-238 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Sistema |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | Não aplicável (cálculo interno) |
| Dependência principal | US-237 (split map), US-062 (treino concluído), US-231/232 (programa selecionado) |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema**,

quero **determinar, de forma determinística, qual é o dia (letra) do programa que o usuário deve treinar hoje a partir do último dia efetivamente concluído**,

para **avançar corretamente a divisão (ex.: AB → se o último foi A, hoje é B; ABCDE → se o último foi C, hoje é D) sem repetir o mesmo estímulo por engano e sem depender de IA**.

---

## 3. Contexto

O usuário tem um `programKey` selecionado (US-231/232) e a divisão de cada programa está definida no split map (US-237). Falta a regra que, olhando o **histórico de treinos concluídos**, decide qual letra é a de hoje.

A regra é uma **rotação cíclica** sobre a sequência de dias do programa: o próximo dia é o seguinte ao último dia concluído; ao chegar no último, volta ao primeiro. Para `full_body` só existe um dia lógico (FB), então a "rotação" é trivial e o valor agregado passa a ser a variação de ênfase da US-239.

O ponteiro deve avançar **apenas com treinos efetivamente concluídos** (US-062), nunca com quests abandonadas, canceladas ou apenas geradas. Regenerar a quest no mesmo dia (US-048) **não** avança o ponteiro. Trocar de programa reinicia a contagem para o primeiro dia do novo programa. Esta US **não** decide o alvo muscular (US-237) nem aplica recuperação (US-239); apenas resolve a letra do dia.

---

## 4. Objetivo

Calcular `resolvedDayKey` (a letra do dia de hoje) e `resolvedDayIndex` para o `programKey` ativo do usuário, com base no último `WorkoutSession` concluído do mesmo programa, tratando todos os casos de borda de forma determinística.

---

## 5. Escopo

### Entra nesta US

- Leitura da sequência de dias do programa (US-237).
- Leitura do último treino concluído do usuário no programa atual (US-062).
- Regra de rotação cíclica `next(dayKey)`.
- Casos de borda: sem histórico, troca de programa, regeneração no mesmo dia, dias pulados, quest não concluída, histórico de outro programa.
- Persistência do ponteiro resolvido para auditoria e idempotência do dia.

### Fora desta US

- Definição do alvo muscular do dia (US-237).
- Recuperação e anti-sobrecarga / variação de full body (US-239).
- Composição do conjunto elegível e volume (US-240).
- Registro do treino concluído em si (US-062).
- Seleção e prescrição de exercícios (EPIC-006).

---

## 6. Algoritmo de rotação (determinístico)

```txt
entrada:
  programKey                      // programa ativo do usuário (US-231/232)
  days = [d1, d2, ..., dN]        // sequência ordenada do split map (US-237)
  lastCompleted                   // último WorkoutSession CONCLUÍDO no MESMO programKey (US-062), ou vazio
  todayHasCompletedWorkout        // já concluiu um treino hoje?

passos:
1. Se days tem 1 elemento (full_body): resolvedDayKey = d1 ("FB"); FIM.
2. Se não há lastCompleted (primeiro treino no programa): resolvedDayKey = d1 (primeira letra); FIM.
3. Seja lastKey = lastCompleted.dayKey.
   Se lastKey não pertence a days (troca de programa / dado legado): resolvedDayKey = d1; FIM.
4. resolvedDayKey = next(lastKey) = days[(indexOf(lastKey) + 1) mod N].
5. resolvedDayIndex = indexOf(resolvedDayKey) + 1.
FIM.
```

Regras de apoio:

- **Rotação cíclica**: depois do último dia volta ao primeiro. Ex.: ABCDE, último = E → hoje = A.
- **O ponteiro é do último concluído, não do calendário**: se o usuário ficou dias sem treinar, ao voltar ele continua de onde parou (último concluído + 1). Não há "pular" letras por dias de folga.
- **Regeneração no mesmo dia (US-048)**: como nenhum treino novo foi concluído, `lastCompleted` não muda; a letra resolvida permanece a mesma (idempotência do dia).
- **Segundo treino no mesmo dia**: se `todayHasCompletedWorkout = true` e o usuário gera outra quest, a rotação avança normalmente a partir do treino concluído hoje (dia seguinte), pois a base é sempre o último concluído.

---

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O dia de hoje é sempre o **sucessor cíclico** do último dia efetivamente concluído no mesmo programa. |
| RN-002 | Apenas `WorkoutSession` com status concluído (US-062) avança o ponteiro; abandonadas/canceladas/apenas geradas não contam. |
| RN-003 | Sem histórico no programa atual, o dia resolvido é o **primeiro** dia do programa. |
| RN-004 | Trocar de programa reinicia a rotação: o primeiro treino no novo programa é o primeiro dia dele, ignorando o histórico do programa anterior. |
| RN-005 | Para `full_body`, o dia resolvido é sempre o único dia lógico (FB); a diferenciação entre sessões vem da US-239. |
| RN-006 | Regenerar a quest no mesmo dia (US-048) não avança o ponteiro; o dia resolvido é idempotente enquanto não houver novo treino concluído. |
| RN-007 | Dias de folga (sem treino) não pulam letras; a rotação continua do último concluído. |
| RN-008 | O cálculo é 100% determinístico e reproduzível a partir do histórico; sem IA e sem aleatoriedade. |
| RN-009 | O dia resolvido deve ser registrado no `Quest`/`WorkoutSession` gerado, para auditoria e para servir de base à próxima rotação. |

---

## 8. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Sistema | Calcula e persiste o dia resolvido. |
| Usuário final | Recebe a quest do dia correspondente; vê a letra/rótulo do dia (US-237). |

---

## 9. Fluxo principal

1. Sistema identifica o `programKey` ativo do usuário.
2. Carrega a sequência de dias do split map (US-237).
3. Busca o último treino concluído do usuário no mesmo programa (US-062).
4. Aplica o algoritmo da seção 6.
5. Persiste `resolvedDayKey`/`resolvedDayIndex` no artefato do dia.
6. Entrega o dia resolvido à US-240 (composição do elegível).

---

## 10. Fluxos alternativos

### 10.1. Primeiro treino do usuário

Sem histórico → primeiro dia do programa (RN-003).

### 10.2. Troca de programa

Histórico do programa anterior é ignorado para rotação → primeiro dia do novo programa (RN-004).

### 10.3. Volta após folga longa

Continua do último concluído + 1, independentemente de quantos dias se passaram (RN-007).

### 10.4. Quest do dia não concluída

Se ontem a quest foi gerada mas não concluída, ela **não** avançou o ponteiro; hoje o dia resolvido é o mesmo que seria ontem.

### 10.5. Programa sem split clássico

`perfect_2`/`system` (sem split map obrigatório na US-237) usam resolução própria e não são cobertos por esta rotação.

---

## 11. Estados esperados

- dia resolvido (primeiro dia);
- dia resolvido (sucessor cíclico);
- dia resolvido idempotente (regeneração);
- reinício por troca de programa;
- full body (dia único).

---

## 12. Impacto no Frontend Flutter

- A quest do dia exibe a letra/rótulo do dia resolvido (ex.: "Dia B — Puxar"), a partir do split map (US-237).
- Indireto: histórico pode mostrar a sequência de letras concluídas.

---

## 13. Impacto no Backend

- Serviço `DailyProgramDayResolver.resolve(userId)` → `{ programKey, resolvedDayKey, resolvedDayIndex }`.
- Consulta do último `WorkoutSession` concluído por `programKey`.
- Gravação do dia resolvido no artefato de geração.

---

## 14. Impacto no Banco de Dados

- `WorkoutSession` (existente, US-062): garantir os campos `ProgramKey` e `DayKey` no registro de conclusão, para servir de base à rotação.
- `Quest`/geração: gravar `ResolvedProgramKey`, `ResolvedDayKey`, `ResolvedDayIndex`, `SplitMapVersion`.
- Índice por `(UserId, ProgramKey, CompletedAt desc)` para buscar o último concluído.

---

## 15. Impacto em Gamificação

- Rotação correta garante progressão coerente do programa, reforçando a sensação de evolução do Hunter.

---

## 16. Impacto em Monetização

- Indireto: divisão que "faz sentido dia após dia" aumenta a aderência e a percepção de valor no trial.

---

## 17. Impacto em Internacionalização

- Cálculo interno; a exibição da letra/rótulo do dia usa as chaves i18n do split map (US-237).

---

## 18. Contrato de API sugerido

```txt
GET /api/quests/resolve-day
```

Response conceitual:

```json
{
  "programKey": "abcde",
  "lastCompletedDayKey": "C",
  "resolvedDayKey": "D",
  "resolvedDayIndex": 4,
  "splitMapVersion": "v1",
  "reason": "cyclic_successor"
}
```

Valores de `reason`: `first_workout`, `cyclic_successor`, `program_changed`, `full_body_single_day`, `regeneration_idempotent`.

---

## 19. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| program_day_resolved | Quando o dia do programa é resolvido para uma geração. |
| program_rotation_reset | Quando a rotação reinicia por troca de programa. |

---

## 20. Critérios de aceite

### CA-001 — Sucessor cíclico

Dado um usuário no programa ABCDE cujo último dia concluído foi C,

Quando a quest de hoje for gerada,

Então o dia resolvido deve ser D.

### CA-002 — Retorno ao primeiro dia

Dado um usuário no programa AB cujo último dia concluído foi B,

Quando gerar a próxima quest,

Então o dia resolvido deve ser A.

### CA-003 — Primeiro treino

Dado um usuário sem histórico no programa atual,

Quando gerar a primeira quest,

Então o dia resolvido deve ser o primeiro dia do programa.

### CA-004 — Regeneração não avança

Dado que o usuário regenera a quest no mesmo dia sem concluir treino,

Quando a quest for regenerada,

Então o dia resolvido deve permanecer o mesmo.

### CA-005 — Troca de programa reinicia

Dado que o usuário troca de ABC para ABCD,

Quando gerar a primeira quest no novo programa,

Então o dia resolvido deve ser o dia A do ABCD, ignorando o histórico anterior.

### CA-006 — Full body

Dado um usuário em `full_body`,

Quando gerar qualquer quest,

Então o dia resolvido deve ser sempre o dia único (FB).

---

## 21. Critérios de teste para QA

### Backend

- rotação cíclica correta para AB, ABC, ABCD, ABCDE;
- apenas treinos concluídos avançam o ponteiro;
- sem histórico → primeiro dia;
- troca de programa reinicia no primeiro dia;
- regeneração no mesmo dia é idempotente;
- folga longa continua do último concluído;
- full body sempre resolve o dia único.

---

## ✅ Decisão registrada

> O dia do programa é o **sucessor cíclico do último dia efetivamente concluído** no mesmo programa, calculado de forma 100% determinística sobre o histórico de treinos concluídos (US-062), sem IA. Regenerações não avançam o ponteiro, trocas de programa reiniciam a rotação e `full_body` sempre resolve o dia único, deixando a diferenciação entre sessões para a US-239.
