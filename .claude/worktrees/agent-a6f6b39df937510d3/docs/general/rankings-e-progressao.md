# AWAKEN — Rankings e Progressão

> Documento independente sobre o sistema de Rankings do AWAKEN.  
> Este arquivo trata apenas da progressão de Rank, pontuação, curva exponencial e tempo médio estimado para evolução de E até SSS.

---

## 1. Objetivo do documento

Este documento define:

- quais são os Ranks do AWAKEN;
- como a pontuação de Rank deve funcionar;
- como o Rank inicial se relaciona com o onboarding;
- por que o onboarding só pode levar até o início do Rank B;
- como a progressão deve ser exponencial;
- quanto tempo, em média, o usuário deve levar para atingir cada Rank;
- como calibrar o sistema para que o Rank SSS leve cerca de 3 anos de treinamento constante;
- quais regras impedem progressão artificial, abuso ou evolução rápida demais.

---

## 2. Lista oficial de Rankings

O AWAKEN deve suportar os seguintes Ranks:

```txt
Rank E
Rank D
Rank C
Rank B
Rank A
Rank S
Rank SS
Rank SSS
```

A ordem de progressão é:

```txt
E → D → C → B → A → S → SS → SSS
```

---

## 3. Relação entre Level e Rank

O **Level** representa a progressão geral do jogador.

O **Rank** representa o patamar de evolução física acumulada.

Regra central:

```txt
Todo usuário começa no Level 1.
O Rank inicial pode variar conforme o onboarding.
O Rank inicial máximo pelo onboarding é B.
Ranks A, S, SS e SSS só podem ser conquistados treinando.
```

Exemplos:

```txt
Level 1 — Rank E
Level 1 — Rank D
Level 1 — Rank C
Level 1 — Rank B
```

Mesmo um usuário que começa em Rank B pelo onboarding ainda começa no:

```txt
Level 1
```

---

## 4. Conceito de RankScore

O Rank deve ser calculado a partir de uma pontuação chamada:

```txt
RankScore
```

O `RankScore` representa a soma da evolução relevante do personagem para fins de Ranking.

No AWAKEN, o RankScore deve estar ligado aos atributos do usuário, porque os atributos representam evolução real.

Atributos do personagem:

```txt
Força
Agilidade
Resistência
Vitalidade
Foco
Sabedoria
```

Regra conceitual:

```txt
RankScore = soma dos pontos reais de atributos válidos para Rank
```

Exemplo:

```txt
Força: 8
Agilidade: 7
Resistência: 6
Vitalidade: 8
Foco: 6
Sabedoria: 5

RankScore = 40
```

---

## 5. Limite inicial pelo onboarding

Apesar de o sistema completo suportar até Rank SSS, o onboarding não deve permitir que um usuário comece em Rank alto demais.

Regra:

```txt
O Rank máximo possível pelo onboarding é Rank B.
O RankScore máximo possível pelo onboarding é 48.
```

Isso significa:

```txt
Cenário máximo inicial:
Força: 8
Agilidade: 8
Resistência: 8
Vitalidade: 8
Foco: 8
Sabedoria: 8

RankScore: 48
Rank: B
Level: 1
```

O usuário não pode começar em:

```txt
Rank A
Rank S
Rank SS
Rank SSS
```

Esses Ranks precisam ser conquistados com treino real.

---

## 6. Por que o onboarding para no Rank B

O onboarding é baseado em declaração do usuário.

Ele pode indicar:

- experiência;
- tempo treinando;
- dores;
- limitações;
- tipo de corpo;
- objetivo;
- disponibilidade;
- dados físicos.

Mas ele não comprova:

- consistência real;
- execução correta;
- evolução semanal;
- aderência;
- performance;
- disciplina;
- recuperação;
- progressão de carga;
- histórico validado pelo app.

Por isso:

```txt
O onboarding pode reconhecer um usuário mais avançado,
mas não deve premiar com Rank alto sem histórico dentro do AWAKEN.
```

Decisão:

```txt
Rank B é o teto inicial.
Rank A ou superior exige treino real registrado no app.
```

---

## 7. Tabela oficial de Ranks

A progressão deve seguir uma curva aproximadamente exponencial.

| Rank | RankScore necessário | Interpretação |
|---|---:|---|
| Rank E | 6 a 17 | Início da jornada. |
| Rank D | 18 a 29 | Primeira adaptação. |
| Rank C | 30 a 47 | Base física em formação. |
| Rank B | 48 a 83 | Boa base inicial ou evolução consistente. |
| Rank A | 84 a 155 | Usuário consistente e acima da média. |
| Rank S | 156 a 299 | Alto nível de comprometimento. |
| Rank SS | 300 a 587 | Evolução longa, estável e avançada. |
| Rank SSS | 588+ | Elite do sistema, cerca de 3 anos de treino constante. |

---

## 8. Curva exponencial

A curva de Ranking deve ficar cada vez mais difícil.

A lógica é:

```txt
E → D: salto pequeno
D → C: salto pequeno/moderado
C → B: salto moderado
B → A: salto grande
A → S: salto muito grande
S → SS: salto extremamente grande
SS → SSS: salto de elite
```

Escala:

```txt
E:   6–17
D:   18–29
C:   30–47
B:   48–83
A:   84–155
S:   156–299
SS:  300–587
SSS: 588+
```

A progressão não é linear.

O usuário deve sentir avanço mais rápido no começo e avanço mais lento nos Ranks altos.

---

## 9. Tamanho aproximado dos saltos

| Transição | Pontos aproximados necessários | Dificuldade |
|---|---:|---|
| E → D | 12 pontos | Baixa |
| D → C | 12 pontos | Baixa |
| C → B | 18 pontos | Moderada |
| B → A | 36 pontos | Alta |
| A → S | 72 pontos | Muito alta |
| S → SS | 144 pontos | Extrema |
| SS → SSS | 288 pontos | Elite |

Essa estrutura faz com que cada fase avançada exija aproximadamente o dobro de esforço da anterior.

---

## 10. Definição de treinamento constante

Para calcular o tempo médio de progressão, considerar como treinamento constante:

```txt
4 treinos por semana
45 minutos médios por treino
48 semanas ativas por ano
boa taxa de conclusão das quests
progressão segura
sem longas pausas
sem treinar com dor forte
sem tentar burlar XP
```

Isso equivale aproximadamente a:

```txt
16 treinos por mês
192 treinos por ano
576 treinos em 3 anos
```

---

## 11. Ganho médio de RankScore

Para calibrar o sistema, considerar ganho médio de:

```txt
12 a 18 RankScore por mês
```

Média usada para estimativa:

```txt
15 RankScore por mês
```

Essa média considera:

- treinos completos;
- treinos parciais;
- semanas mais fortes;
- semanas mais fracas;
- regressões;
- progressão segura;
- ausência de treino em alguns dias;
- evolução realista.

---

## 12. Tempo médio para atingir cada Rank

A tabela abaixo considera um usuário que começa entre Rank E e Rank D e mantém treinamento constante.

| Rank | RankScore necessário | Tempo médio aproximado | Interpretação |
|---|---:|---:|---|
| Rank E | 6+ | imediato | Todo usuário começa aqui ou acima. |
| Rank D | 18+ | 0 a 1 mês | Primeira adaptação. |
| Rank C | 30+ | 1 a 2 meses | Base inicial consolidada. |
| Rank B | 48+ | 3 a 4 meses | Usuário já tem rotina clara. |
| Rank A | 84+ | 6 a 8 meses | Consistência real. |
| Rank S | 156+ | 10 a 14 meses | Alto comprometimento. |
| Rank SS | 300+ | 18 a 24 meses | Evolução longa e estável. |
| Rank SSS | 588+ | 33 a 39 meses | Cerca de 3 anos de treinamento constante. |

---

## 13. Tempo médio considerando Rank inicial

O tempo até o SSS depende do Rank inicial.

| Rank inicial | Tempo médio até SSS |
|---|---:|
| E | 36 a 39 meses |
| D | 35 a 38 meses |
| C | 34 a 37 meses |
| B | 33 a 36 meses |

Mesmo começando em Rank B, o usuário ainda precisa de anos de treino real para chegar ao SSS.

---

## 14. Progressão por perfil de usuário

### Usuário casual

Treina de forma irregular.

```txt
1 a 2 treinos por semana
muitas pausas
baixa consistência
```

Progressão esperada:

```txt
Pode ficar entre E, D, C ou B por muito tempo.
Pode levar mais de 5 anos para SSS.
Pode nunca chegar ao SSS se não houver consistência.
```

---

### Usuário regular

Treina com boa frequência.

```txt
3 treinos por semana
boa aderência
algumas pausas normais
```

Progressão esperada:

```txt
Chega ao Rank A em aproximadamente 1 ano.
Chega ao Rank S em 1,5 a 2 anos.
Chega ao Rank SS em 2,5 a 3 anos.
SSS pode levar 4 anos ou mais.
```

---

### Usuário constante

Treina de forma consistente.

```txt
4 treinos por semana
boa execução
progressão segura
poucas pausas longas
```

Progressão esperada:

```txt
Chega ao Rank B em 3 a 4 meses.
Chega ao Rank A em 6 a 8 meses.
Chega ao Rank S em 10 a 14 meses.
Chega ao Rank SS em 18 a 24 meses.
Chega ao Rank SSS em cerca de 3 anos.
```

---

### Usuário extremo

Treina muito e com alta frequência.

```txt
5 a 6 treinos por semana
alta aderência
alta performance
poucas falhas
```

Progressão esperada:

```txt
Pode evoluir mais rápido.
Ainda assim, o sistema deve impedir SSS rápido demais.
Diminishing returns deve proteger os Ranks altos.
```

---

## 15. Regra de proteção contra progressão rápida demais

O AWAKEN deve impedir que o usuário chegue ao SSS rápido demais.

Regra:

```txt
Rank SSS não deve ser alcançável em poucos meses.
Rank SSS deve representar anos de consistência.
```

Para isso, o sistema pode usar:

- limite mensal de RankScore;
- diminishing returns;
- bônus controlado de streak;
- bloqueio de XP por treino duplicado;
- detecção de abuso;
- redução de ganho em treinos repetidos artificialmente;
- exigência de variedade mínima;
- exigência de progressão real.

---

## 16. Diminishing returns por Rank

A partir do Rank A, o sistema pode aplicar multiplicadores de progresso para deixar a evolução mais difícil.

| Rank atual | Multiplicador sugerido |
|---|---:|
| E | 1.00 |
| D | 1.00 |
| C | 1.00 |
| B | 0.90 |
| A | 0.80 |
| S | 0.70 |
| SS | 0.60 |

Exemplo:

```txt
Usuário Rank S ganhou 10 pontos válidos em uma semana.
Multiplicador Rank S = 0.70

RankScore efetivo ganho:
10 * 0.70 = 7
```

Os atributos podem continuar evoluindo normalmente, mas o Rank avança mais devagar.

---

## 17. Limite mensal recomendado

Para manter a progressão equilibrada:

| Perfil | Ganho mensal saudável |
|---|---:|
| Casual | 3 a 8 RankScore/mês |
| Regular | 8 a 12 RankScore/mês |
| Constante | 12 a 18 RankScore/mês |
| Extremo | 18 a 24 RankScore/mês |

Regra:

```txt
Acima de 24 RankScore/mês, aplicar redução, validação ou diminishing returns.
```

---

## 18. Bônus de streak

O streak deve ajudar, mas não quebrar a economia.

### Regra recomendada

```txt
Streak dá bônus pequeno de RankScore.
Streak não pode ser a principal fonte de RankScore.
Streak deve premiar consistência, não intensidade artificial.
```

### Bônus sugerido

| Streak | Bônus |
|---|---:|
| 7 dias | +1 RankScore |
| 30 dias | +3 RankScore |
| 90 dias | +8 RankScore |
| 180 dias | +15 RankScore |
| 365 dias | +35 RankScore |

Esses bônus ajudam, mas não permitem que o usuário pule anos de progressão.

---

## 19. Bônus por Master Quest

Master Quests podem acelerar a progressão, mas com limite.

| Tipo de Master Quest | Bônus sugerido |
|---|---:|
| Semanal simples | +1 a +2 RankScore |
| Semanal perfeita | +3 RankScore |
| Mensal especial | +5 a +8 RankScore |
| Evento raro | +10 a +15 RankScore |

Regra:

```txt
Bônus de evento não deve permitir alcançar SSS sem consistência real.
```

---

## 20. Regras contra abuso

O sistema deve impedir ganho artificial de RankScore.

### Não deve gerar RankScore alto quando:

```txt
Usuário repete treino muito curto várias vezes só para farmar.
Usuário marca conclusão sem execução real.
Usuário ignora dor forte.
Usuário faz sempre o mesmo exercício sem progressão.
Usuário pula grande parte da quest.
Usuário usa treino incompatível com o perfil apenas por XP.
```

### Deve reduzir ganho quando:

```txt
Treino foi parcial.
Treino foi fácil demais repetidas vezes.
Treino não teve progressão.
Treino foi repetido artificialmente.
Houve dor forte.
Houve baixa qualidade de execução.
```

---

## 21. Relação entre Rank e segurança

Rank alto não deve liberar automaticamente exercícios perigosos.

Mesmo um usuário Rank S pode ter:

- dor;
- limitação;
- lesão;
- fadiga;
- baixa mobilidade em determinado padrão;
- restrição temporária.

Regra:

```txt
Segurança sempre supera Rank.
```

Exemplo:

```txt
Usuário Rank S
Dor lombar atual

Resultado:
Bloquear exercícios de alto estresse lombar,
mesmo que o Rank permita exercícios avançados.
```

---

## 22. Relação entre Rank e geração de treino

O Rank pode influenciar:

- dificuldade das quests;
- nome visual da categoria;
- recompensas;
- emblemas;
- efeitos visuais;
- desbloqueio de desafios opcionais;
- tipos de Master Quest;
- nível de complexidade sugerida.

Mas o Rank não pode ignorar:

- objetivo;
- dores;
- limitações;
- tempo disponível;
- equipamento;
- nível efetivo por padrão de movimento.

---

## 23. Desbloqueios por Rank

Sugestão de desbloqueios:

| Rank | Desbloqueios |
|---|---|
| E | Quest diária básica, card inicial, atributos visíveis. |
| D | Primeiros desafios opcionais, histórico simples. |
| C | Variações de treino, pequenas Master Quests. |
| B | Treinos mais específicos, metas semanais, emblemas melhores. |
| A | Master Quests avançadas, efeitos visuais especiais. |
| S | Desafios de elite, card animado especial, recompensas raras. |
| SS | Eventos avançados, quests lendárias, aura visual de perfil. |
| SSS | Status máximo, cosméticos raros, título especial, card premium lendário. |

---

## 24. Nomes narrativos opcionais

Os Ranks podem ter nomes narrativos para reforçar o tom anime/RPG.

| Rank | Nome narrativo opcional |
|---|---|
| E | Desperto |
| D | Aprendiz |
| C | Caçador |
| B | Elite |
| A | Ascendente |
| S | Despertado |
| SS | Monarca |
| SSS | Lenda Viva |

Esses nomes podem ser usados visualmente, mas o Rank principal deve continuar claro:

```txt
Rank E
Rank D
Rank C
Rank B
Rank A
Rank S
Rank SS
Rank SSS
```

---

## 25. Fórmula final recomendada

### Durante o onboarding

```txt
RankScore inicial = soma dos atributos iniciais
RankScore inicial máximo = 48
Rank inicial máximo = B
Level inicial = 1
```

### Após o onboarding

```txt
RankScore evolui com treino real
RankScore segue curva exponencial
Ranks altos têm progressão mais lenta
SSS exige cerca de 3 anos de treino constante
```

---

## 26. Pseudocódigo de Rank

```txt
if rankScore <= 17:
  rank = "E"
else if rankScore <= 29:
  rank = "D"
else if rankScore <= 47:
  rank = "C"
else if rankScore <= 83:
  rank = "B"
else if rankScore <= 155:
  rank = "A"
else if rankScore <= 299:
  rank = "S"
else if rankScore <= 587:
  rank = "SS"
else:
  rank = "SSS"
```

---

## 27. Pseudocódigo com teto de onboarding

```txt
if source == "onboarding":
  if rankScore > 48:
    rankScore = 48

rank = calculateRank(rankScore)
level = 1
```

---

## 28. Pseudocódigo de ganho mensal saudável

```txt
monthlyGain = sum(validRankScoreGainInMonth)

if monthlyGain <= 18:
  apply full gain

else if monthlyGain <= 24:
  apply partial diminishing returns

else:
  apply strong diminishing returns
  flag for validation if behavior seems abusive
```

---

## 29. Regras de negócio

| ID | Regra |
|---|---|
| RN-RANK-001 | O sistema deve suportar Ranks E, D, C, B, A, S, SS e SSS. |
| RN-RANK-002 | Todo usuário começa no Level 1. |
| RN-RANK-003 | O onboarding pode definir Rank inicial diferente para cada usuário. |
| RN-RANK-004 | O Rank máximo pelo onboarding é B. |
| RN-RANK-005 | O RankScore máximo pelo onboarding é 48. |
| RN-RANK-006 | Rank A ou superior só pode ser obtido com treino real. |
| RN-RANK-007 | A curva de Rank deve ser aproximadamente exponencial. |
| RN-RANK-008 | O Rank SSS deve exigir cerca de 3 anos de treino constante. |
| RN-RANK-009 | Treino constante deve considerar frequência, aderência e progressão segura. |
| RN-RANK-010 | RankScore não pode ser comprado. |
| RN-RANK-011 | RankScore não deve ser concedido por ações sem esforço real. |
| RN-RANK-012 | Dores e limitações devem superar permissões de Rank. |
| RN-RANK-013 | A partir do Rank A, o sistema pode aplicar diminishing returns. |
| RN-RANK-014 | O sistema deve limitar progressão mensal anormal. |
| RN-RANK-015 | Streak pode dar bônus, mas não pode ser a principal fonte de RankScore. |
| RN-RANK-016 | Master Quests podem dar bônus controlado. |
| RN-RANK-017 | O Rank deve ser recalculado sempre que o RankScore mudar. |
| RN-RANK-018 | O Rank deve ser exibido como progresso, nunca como julgamento físico. |

---

## 30. Critérios de aceite

### CA-RANK-001 — Suporte a todos os Ranks

Dado que o sistema possui usuários em diferentes estágios,  
quando o RankScore for calculado,  
então o sistema deve suportar Ranks de E até SSS.

---

### CA-RANK-002 — Teto do onboarding

Dado que o usuário concluiu o onboarding,  
quando o Rank inicial for calculado,  
então o RankScore não pode ultrapassar 48.

---

### CA-RANK-003 — Rank inicial máximo

Dado que o RankScore inicial máximo é 48,  
quando o Rank for calculado,  
então o maior Rank inicial possível deve ser B.

---

### CA-RANK-004 — Rank A por treino real

Dado que um usuário está em Rank B,  
quando ele acumular RankScore suficiente por treino real,  
então poderá subir para Rank A.

---

### CA-RANK-005 — Rank SSS de longo prazo

Dado que o Rank SSS exige RankScore 588+,  
quando um usuário treinar de forma constante,  
então o tempo médio esperado deve ser cerca de 33 a 39 meses.

---

### CA-RANK-006 — Segurança supera Rank

Dado que um usuário Rank S possui dor lombar,  
quando o treino for gerado,  
então o sistema deve bloquear exercícios incompatíveis, mesmo com Rank alto.

---

### CA-RANK-007 — Diminishing returns

Dado que um usuário está em Rank S,  
quando ganhar pontos válidos de RankScore,  
então o sistema pode aplicar multiplicador reduzido para desacelerar progressão.

---

## 31. Eventos de analytics recomendados

| Evento | Quando dispara |
|---|---|
| `rank_score_changed` | Quando o RankScore muda. |
| `rank_changed` | Quando o usuário sobe ou desce de Rank. |
| `rank_cap_applied_onboarding` | Quando o teto 48 é aplicado no onboarding. |
| `rank_diminishing_returns_applied` | Quando há redução de progresso por Rank alto. |
| `rank_progress_monthly_limit_reached` | Quando usuário atinge limite mensal saudável. |
| `rank_master_quest_bonus_applied` | Quando bônus de Master Quest é aplicado. |
| `rank_streak_bonus_applied` | Quando bônus de streak é aplicado. |
| `rank_abuse_suspected` | Quando padrão anormal é detectado. |

---

## 32. Decisão final

O sistema de Rankings do AWAKEN deve funcionar assim:

```txt
O onboarding define o ponto de partida.
O ponto de partida máximo é Rank B com RankScore 48.
O sistema completo suporta E, D, C, B, A, S, SS e SSS.
A progressão deve ser exponencial.
Ranks baixos evoluem mais rápido.
Ranks altos evoluem mais devagar.
SSS deve exigir cerca de 3 anos de treino constante.
Rank alto não pode ignorar dores, limitações ou segurança.
```

---

## 33. Checklist de implementação

```txt
[ ] Criar tabela completa de Ranks E a SSS.
[ ] Criar cálculo de RankScore.
[ ] Criar teto de RankScore 48 no onboarding.
[ ] Criar função calculateRank(rankScore).
[ ] Criar curva exponencial de thresholds.
[ ] Criar estimativa média de tempo por Rank.
[ ] Criar ganho mensal saudável.
[ ] Criar diminishing returns por Rank.
[ ] Criar bônus controlado de streak.
[ ] Criar bônus controlado de Master Quest.
[ ] Criar proteção contra abuso de RankScore.
[ ] Criar eventos de analytics de Rank.
[ ] Criar testes para Rank E.
[ ] Criar testes para Rank D.
[ ] Criar testes para Rank C.
[ ] Criar testes para Rank B.
[ ] Criar testes para Rank A.
[ ] Criar testes para Rank S.
[ ] Criar testes para Rank SS.
[ ] Criar testes para Rank SSS.
[ ] Criar teste de teto do onboarding.
[ ] Criar teste de SSS em aproximadamente 3 anos.
```

---

*Documento independente de Rankings e Progressão do AWAKEN.*
