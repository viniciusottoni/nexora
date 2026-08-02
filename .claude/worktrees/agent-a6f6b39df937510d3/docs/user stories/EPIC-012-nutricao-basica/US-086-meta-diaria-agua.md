---
title: US-086 — Ver meta diária simples de água
sidebar_position: 86
---

# US-086 — Ver meta diária simples de água

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-086 |
| Épico | EPIC-012 — Nutrição Básica |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **ver minha meta diária simples de água**, para **acompanhar minha hidratação de forma visual e motivadora**.

## 3. Contexto

A nutrição básica do AWAKEN não substitui orientação profissional. No MVP, ela deve ser simples e útil: mostrar quanto de água o usuário deveria consumir no dia, com mínimo aceitável e meta ideal.

## 4. Objetivo

Calcular e exibir a meta diária de água com base no peso corporal informado no onboarding/perfil.

## 5. Escopo

### Entra nesta US

- Calcular mínimo aceitável de água: 30 ml por kg corporal.
- Calcular meta ideal de água: 50 ml por kg corporal.
- Exibir progresso em barra visual.
- Exibir valores em ml ou litros.
- Indicar que é acompanhamento básico.

### Fora desta US

- Prescrição nutricional profissional.
- Plano alimentar.
- Macros avançados.
- Integração com wearables.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A meta mínima deve ser calculada como `pesoKg * 30ml`. |
| RN-002 | A meta ideal deve ser calculada como `pesoKg * 50ml`. |
| RN-003 | O peso deve vir do perfil do usuário. |
| RN-004 | Sem peso válido, a interface deve solicitar atualização do perfil. |
| RN-005 | A tela deve deixar claro que é acompanhamento básico. |
| RN-006 | Usuário sem acesso ativo não deve usar nutrição básica. |

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

1. Usuário acessa Home.
2. App carrega dados físicos do perfil.
3. Sistema calcula mínimo e meta ideal de água.
4. App exibe barra de água com progresso do dia.
5. Usuário entende quanto falta para atingir mínimo e ideal.

## 9. Fluxos alternativos

### 9.1. Peso ausente

Exibir estado pedindo atualização do perfil.

### 9.2. Acesso expirado

Exibir card bloqueado com CTA de assinatura.

## 10. Estados esperados

- carregando;
- meta calculada;
- perfil incompleto;
- acesso bloqueado;
- erro.

## 11. Impacto Flutter

- Barra de status de água.
- Labels para mínimo e ideal.
- Estado de perfil incompleto.
- Visual dark/RPG alinhado à Home.

## 12. Impacto Backend

- Retornar meta mínima e ideal na consulta de nutrição básica.
- Validar acesso ativo.
- Usar peso atual do perfil.
- Considerar o dia local do usuário via offset enviado pelo app.

## 13. Impacto DB

Entidades:

- UserProfile;
- NutritionLog.

Campos usados:

- weightKg;
- waterMl.

## 14. Impacto Gamificação

- Não concede XP no MVP.
- Funciona como reforço visual de autocuidado.

## 15. Impacto Monetização

- Disponível apenas para usuário com acesso ativo.
- Acesso expirado direciona para assinatura.

## 16. Contrato API sugerido

```txt
GET /api/nutrition/basic/today
```

Response parcial:

```json
{
  "waterMinimumMl": 2100,
  "waterIdealMl": 3500,
  "waterConsumedMl": 900
}
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| nutrition_basic_viewed | Quando o card de nutrição é exibido. |

## 18. Critérios de aceite

### CA-001 — Meta calculada

Dado que o usuário possui peso válido,
Quando abrir a Home,
Então deve ver mínimo e meta ideal de água calculados por kg corporal.

### CA-002 — Perfil incompleto

Dado que o usuário não possui peso válido,
Quando abrir nutrição básica,
Então deve ser orientado a atualizar o perfil.

## 19. Critérios de teste QA

- peso válido;
- peso ausente;
- meta mínima 30 ml/kg;
- meta ideal 50 ml/kg;
- acesso expirado;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

A meta de água do MVP é simples: mínimo de 30 ml/kg e ideal de 50 ml/kg, sempre apresentada como acompanhamento básico.
