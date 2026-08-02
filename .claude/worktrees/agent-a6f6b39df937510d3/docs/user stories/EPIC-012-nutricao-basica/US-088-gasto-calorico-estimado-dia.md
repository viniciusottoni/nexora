---
title: US-088 — Ver gasto calórico estimado do dia até o momento
sidebar_position: 88
---

# US-088 — Ver gasto calórico estimado do dia até o momento

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-088 |
| Épico | EPIC-012 — Nutrição Básica |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **ver meu gasto calórico estimado do dia até o momento**, para **entender aproximadamente quanta energia meu corpo já gastou hoje**.

## 3. Contexto

O AWAKEN não fará prescrição nutricional no MVP. O gasto calórico deve ser apresentado como estimativa simples, acumulada desde a meia-noite até o horário atual.

## 4. Objetivo

Calcular e exibir o gasto calórico estimado do dia até o momento, usando TMB, fator de atividade, fator de tipo corporal e fração do dia transcorrida.

## 5. Escopo

### Entra nesta US

- Calcular TMB por sexo biológico, idade, peso e altura.
- Aplicar fator de atividade.
- Aplicar fator de tipo corporal.
- Estimar gasto acumulado desde meia-noite até o horário atual.
- Exibir valor aproximado em kcal.
- Deixar claro que é estimativa.

### Fora desta US

- Calorias consumidas.
- Déficit/superávit calórico.
- Macros.
- Dieta personalizada.
- Integração com apps de nutrição.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Homem: TMB = `10 × pesoKg + 6,25 × alturaCm - 5 × idade + 5`. |
| RN-002 | Mulher: TMB = `10 × pesoKg + 6,25 × alturaCm - 5 × idade - 161`. |
| RN-003 | Fator de atividade: sedentário 1,20; levemente ativo 1,35; moderadamente ativo 1,55; muito ativo 1,75; atleta/intenso 1,90. |
| RN-004 | Fator de tipo corporal: magro 0,95; normal 1,00; atlético 1,07; gordo 0,92. |
| RN-005 | Gasto diário estimado = TMB × fator de atividade × fator de tipo corporal. |
| RN-006 | Gasto até o momento = gasto diário estimado × fração do dia transcorrida desde meia-noite. |
| RN-007 | Se dados físicos estiverem incompletos, exibir estado de perfil incompleto. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode visualizar. |
| Usuário em Trial | Pode visualizar com trial ativo. |
| Premium Mensal | Pode visualizar. |
| Premium Anual | Pode visualizar. |
| Trial expirado | Visualiza bloqueio/CTA. |
| Assinatura expirada | Visualiza bloqueio/CTA. |

## 8. Fluxo principal

1. Usuário abre Home.
2. Backend carrega idade, sexo biológico, altura, peso, nível de atividade e tipo corporal.
3. Sistema calcula TMB.
4. Sistema aplica fatores.
5. Sistema calcula a fração do dia transcorrida.
6. App exibe kcal estimadas até o momento.

## 9. Fluxos alternativos

### 9.1. Dados incompletos

Exibir CTA para atualizar perfil.

### 9.2. Sexo biológico não interpretável

Usar estado de perfil incompleto ou regra segura definida pelo produto; não inventar cálculo silenciosamente.

## 10. Estados esperados

- calculando;
- estimativa exibida;
- perfil incompleto;
- acesso bloqueado;
- erro.

## 11. Impacto Flutter

- Indicador de kcal estimadas.
- Texto “estimado” visível.
- Barra de status visual.
- Estado de perfil incompleto.

## 12. Impacto Backend

- Calcular ou retornar gasto estimado.
- Validar acesso ativo.
- Usar timezone do usuário para fração do dia.
- Não tratar o valor como diagnóstico ou prescrição.

## 13. Impacto DB

Entidades:

- UserProfile;
- NutritionLog.

Campos usados:

- age;
- sexBiological;
- heightCm;
- weightKg;
- bodyType;
- activityLevel.

## 14. Impacto Gamificação

- Não concede XP.
- Ajuda o usuário a enxergar gasto energético como status diário.

## 15. Impacto Monetização

- P1 disponível para acesso ativo.
- Acesso expirado direciona ao paywall.

## 16. Contrato API sugerido

```txt
GET /api/nutrition/basic/today
```

Response parcial:

```json
{
  "caloriesSpentEstimatedToday": 1820,
  "caloriesSpentEstimatedUntilNow": 910,
  "calculationStatus": "estimated"
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| calories_estimate_viewed | Quando gasto estimado é exibido. |

## 18. Critérios de aceite

### CA-001 — Cálculo válido

Dado que o usuário possui dados físicos completos,
Quando abrir a Home,
Então deve ver o gasto calórico estimado até o momento.

### CA-002 — Fração do dia

Dado que é meio-dia,
Quando calcular gasto até o momento,
Então o valor deve representar aproximadamente metade do gasto diário estimado.

## 19. Critérios de teste QA

- cálculo masculino;
- cálculo feminino;
- tipos corporais diferentes;
- fatores de atividade diferentes;
- início do dia;
- fim do dia;
- dados incompletos;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O gasto calórico do EPIC-012 é estimado, motivacional e acumulado desde meia-noite; não substitui avaliação nutricional profissional.
