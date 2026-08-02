---
title: EPIC-012 — Nutrição Básica
sidebar_position: 12
---

# EPIC-012 — Nutrição Básica

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-012 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P1 |
| Perfil principal | Usuário em Trial ou assinante |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Oferecer acompanhamento nutricional básico de água e calorias gastas no dia até o momento, apresentado como barras de status dentro da estética RPG do AWAKEN e visível na Home logo abaixo do card de rank e antes das quests.

## 3. Contexto de produto

Nutrição básica ajuda o usuário a perceber que a evolução física vai além do treino. No MVP, o foco é simplicidade: água e gasto calórico estimado ao longo do dia, sem cálculo completo de macros. A água é acompanhada entre o mínimo aceitável e a meta ideal, e as calorias levam em conta IMC, tipo do corpo e o horário atual desde a meia-noite.

## 4. Escopo

### Entra neste épico

- Meta diária simples de água (mínimo igual a 30ml por kilo corporal e valor ideal 50ml por kilo corporal).
- Registro de consumo de água.
- Gasto calórico estimado do dia até o momento (Gasto diário = TMB × Fator de atividade × Fator do tipo corporal; Homem: TMB = 10 × pesoKg + 6,25 × alturaCm - 5 × idade + 5; Mulher: TMB = 10 × pesoKg + 6,25 × alturaCm - 5 × idade - 161; Fator de atividade: Sedentário = 1,20 | Levemente ativo = 1,35 | Moderadamente ativo = 1,55 | Muito ativo = 1,75 | Atleta/intenso = 1,90; Fator do tipo corporal: Magro = 0,95 | Normal = 1,00 | Atlético = 1,07 | Gordo = 0,92).
- Barras de status visuais.
- Visualização na Home, logo abaixo do card de rank e antes das quests.
- Apresentação da água em copos ajustáveis, com o volume por copo alterando a quantidade exibida.

### Fora deste épico

- Dieta personalizada.
- Macros avançados.
- Scanner de alimentos.
- Integração com apps nutricionais.

## 5. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-086 | Ver meta diária simples de água | P1 | [Abrir](./US-086-meta-diaria-agua.md) |
| US-087 | Registrar consumo de água | P1 | [Abrir](./US-087-registrar-consumo-agua.md) |
| US-088 | Ver gasto calórico estimado do dia até o momento | P1 | [Abrir](./US-088-gasto-calorico-estimado-dia.md) |
| US-089 | Visualizar nutrição básica na Home | P1 | [Abrir](./US-089-nutricao-basica-home.md) |
| US-090 | Visualizar água em copos ajustáveis | P1 | [Abrir](./US-090-agua-copos-ajustaveis.md) |

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-012-001 | Nutrição básica só fica disponível para usuário com acesso ativo. |
| RN-EPIC-012-002 | A água deve ser calculada com mínimo aceitável de 30 ml por kg e meta ideal de 50 ml por kg. |
| RN-EPIC-012-003 | O consumo de água deve ser registrado por dia. |
| RN-EPIC-012-004 | As calorias devem ser estimadas a partir de IMC, tipo do corpo e horário atual, acumulando o gasto desde a meia-noite até o momento atual. |
| RN-EPIC-012-005 | A interface deve deixar claro que é acompanhamento básico. |
| RN-EPIC-012-006 | A visualização de água deve usar copos ajustáveis; quando o tamanho do copo muda, a quantidade exibida deve mudar junto. |
| RN-EPIC-012-007 | A nutrição básica deve aparecer na Home logo abaixo do card de rank e antes das quests. |

## 7. Impactos técnicos

### Flutter

- Card de nutrição básica na Home.
- Componentes de barra de status.
- Indicadores de copos ajustáveis para água.
- Indicadores de gasto calórico estimado.

### Backend

- Endpoint para consultar nutrição básica.
- Endpoint para registrar consumo de água.
- Cálculo diário de gasto calórico estimado a partir de IMC, tipo do corpo e horário atual.
- Validação por data e usuário.
- Dia local da nutrição calculado pelo offset de timezone enviado pelo app.

### Banco de dados

Entidade principal: NutritionLog.

Campos relevantes:

- userId.
- date.
- waterMl.
- caloriesSpentEstimated.

### Analytics

- Eventos podem ser adicionados no pós-MVP se a nutrição virar métrica central.

### QA

- Registrar água.
- Validar gasto calórico estimado ao longo do dia.
- Atualizar barras.
- Ver reset diário.
- Bloquear com acesso expirado.
- Ver a nutrição básica na Home logo abaixo do card de rank e antes das quests.
- Validar a apresentação de água em copos ajustáveis e a mudança da quantidade quando o volume do copo é alterado.

## 8. Dependências

- EPIC-003 para status de acesso.
- EPIC-004 para IMC, tipo do corpo e dados físicos usados no cálculo calórico.
- EPIC-001 para componentes visuais.

## 9. Critérios de aceite do épico

- Usuário registra água.
- Usuário visualiza gasto calórico estimado do dia.
- Usuário ajusta o tamanho do copo e vê a quantidade de água recalculada.
- Barras atualizam corretamente.
- Registros são diários.
- Acesso expirado bloqueia uso.
- Textos não prometem orientação médica ou nutricional profissional.

## 10. Decisão registrada

Nutrição básica é P1 no MVP. Deve ser simples, visual e motivacional, sem competir com apps especializados de dieta. O foco é água e gasto calórico estimado no dia, com visualização na Home e apresentação em copos ajustáveis.
