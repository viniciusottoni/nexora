---
title: US-087 — Registrar consumo de água
sidebar_position: 87
---

# US-087 — Registrar consumo de água

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-087 |
| Épico | EPIC-012 — Nutrição Básica |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Status | Planejada |

## 2. História do usuário

Como **usuário com acesso ativo**, quero **registrar meu consumo de água ao longo do dia**, para **acompanhar minha hidratação diária dentro do AWAKEN**.

## 3. Contexto

O registro de água deve ser rápido. O usuário não deve preencher formulários longos; a ação principal deve ser adicionar uma quantidade padrão baseada no copo configurado.

## 4. Objetivo

Permitir registrar consumo diário de água, somando o volume consumido no dia atual e atualizando a barra de status.

## 5. Escopo

### Entra nesta US

- Adicionar água ao dia atual.
- Somar consumo em ml.
- Reset diário por data local do usuário.
- Exibir total consumido.
- Permitir desfazer ou reduzir último registro como P1 opcional.

### Fora desta US

- Histórico avançado de hidratação.
- Lembretes de água.
- Integração com garrafas inteligentes.
- Recomendação clínica.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Consumo de água deve ser registrado por dia. |
| RN-002 | Registro deve somar volume em ml. |
| RN-003 | Dia deve considerar timezone do usuário. |
| RN-004 | Usuário sem acesso ativo não pode registrar novo consumo. |
| RN-005 | O total consumido deve atualizar a barra imediatamente. |
| RN-006 | Valores negativos não podem deixar consumo abaixo de zero. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode registrar. |
| Usuário em Trial | Pode registrar com trial ativo. |
| Premium Mensal | Pode registrar. |
| Premium Anual | Pode registrar. |
| Trial expirado | Não pode registrar. |
| Assinatura expirada | Não pode registrar. |

## 8. Fluxo principal

1. Usuário visualiza card de nutrição básica.
2. Usuário toca em adicionar água.
3. App usa volume padrão do copo atual.
4. Backend registra consumo no NutritionLog do dia.
5. App atualiza total e barra de progresso.

## 9. Fluxos alternativos

### 9.1. Sem log do dia

Backend cria NutritionLog do dia antes de somar água.

### 9.2. Acesso expirado

App bloqueia ação e exibe CTA para assinatura.

### 9.3. Falha de conexão

App exibe erro e não deve mostrar consumo falso como persistido.

## 10. Estados esperados

- pronto para registrar;
- registrando;
- registrado;
- acesso bloqueado;
- erro de conexão.

## 11. Impacto Flutter

- Botão rápido de adicionar água.
- Feedback visual após registro.
- Atualização da barra.
- Estado de loading e erro.

## 12. Impacto Backend

- Endpoint para registrar água.
- Criar/atualizar NutritionLog por usuário e data.
- Validar acesso ativo.
- Respeitar timezone/data local.
- Usar o offset de timezone enviado pelo app para fechar o dia correto.

## 13. Impacto DB

Entidade: NutritionLog.

Campos:

- userId;
- date;
- waterMl;
- updatedAt.

Restrição sugerida:

- único por userId + date.

## 14. Impacto Gamificação

- Não concede XP no MVP.
- Pode futuramente alimentar conquistas de consistência.

## 15. Impacto Monetização

- Recurso P1 disponível para usuários com acesso ativo.

## 16. Contrato API sugerido

```txt
POST /api/nutrition/water
```

Request conceitual:

```json
{
  "amountMl": 250
}
```

Header de contexto esperado:

```txt
X-Timezone-Offset-Minutes: -180
```

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| water_logged | Quando consumo de água é registrado. |

## 18. Critérios de aceite

### CA-001 — Registro soma água

Dado que o usuário adiciona um copo de 250 ml,
Quando o registro for salvo,
Então o consumo do dia deve aumentar em 250 ml.

### CA-002 — Reset diário

Dado que mudou o dia local,
Quando o usuário abrir o card,
Então o consumo deve começar em zero para o novo dia.

## 19. Critérios de teste QA

- registrar 250 ml;
- registrar múltiplas vezes;
- reset diário;
- acesso expirado;
- erro de conexão;
- impedir valor negativo inválido;
- textos PT-BR, EN e ES.

## 20. Decisão registrada

O registro de água deve ser diário, rápido e visual, usando ml como unidade base para precisão.
