---
title: US-096 — Rastrear eventos de onboarding
sidebar_position: 96
---

# US-096 — Rastrear eventos de onboarding

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-096 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Produto, Engenharia e QA |
| Integrações | Firebase Analytics |
| Status | Planejada |

## 2. História do usuário

Como **time de produto**, quero **rastrear eventos do onboarding**, para **entender onde usuários avançam, abandonam ou encontram erro antes de criar o Perfil Hunter**.

## 3. Contexto

O onboarding define dados físicos, objetivo, nível e restrições do usuário. Sem eventos, não será possível medir ativação nem identificar fricções nas 8 etapas do EPIC-004.

## 4. Objetivo

Registrar início, avanço, conclusão, erro e abandono do onboarding com payload mínimo, sem expor dados sensíveis.

## 5. Escopo

### Entra nesta US

- Evento de início do onboarding.
- Evento por etapa visualizada.
- Evento de avanço/volta de etapa.
- Evento de validação/erro.
- Evento de conclusão do Perfil Hunter.
- Propriedades de etapa, sem valores sensíveis.

### Fora desta US

- Envio de peso, idade, dores ou limitações em analytics.
- Funil avançado em data warehouse.
- Teste A/B.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Eventos não devem expor dados sensíveis de saúde, peso, idade, dores ou limitações. |
| RN-002 | Toda etapa do onboarding deve ter evento de visualização. |
| RN-003 | Conclusão do onboarding deve ser rastreada. |
| RN-004 | Erros de validação devem ser rastreados por código genérico. |
| RN-005 | Payload deve usar nomes padronizados. |

## 7. Eventos sugeridos

| Evento | Quando dispara |
|---|---|
| onboarding_started | Quando usuário inicia onboarding. |
| onboarding_step_viewed | Quando etapa é exibida. |
| onboarding_step_completed | Quando etapa é concluída. |
| onboarding_validation_failed | Quando validação falha. |
| onboarding_completed | Quando Perfil Hunter é criado. |

## 8. Payload mínimo

```json
{
  "step_number": 1,
  "step_key": "goal",
  "flow_version": "v1",
  "source": "post_paywall"
}
```

## 9. Impacto Flutter

- Serviço central de analytics.
- Disparo em cada tela do onboarding.
- Padronização de eventos e propriedades.
- Garantir que campos sensíveis não sejam enviados.

## 10. Impacto Backend

- Pode registrar logs de conclusão do onboarding.
- Não precisa receber dados de analytics do app no MVP.

## 11. Impacto QA

- Validar eventos em todas as 8 etapas.
- Validar evento de conclusão.
- Validar ausência de dados sensíveis.
- Validar erro de validação sem valor pessoal.

## 12. Critérios de aceite

### CA-001 — Etapas rastreadas

Dado que o usuário avança no onboarding,
Quando cada etapa for exibida,
Então o evento `onboarding_step_viewed` deve ser enviado.

### CA-002 — Sem dados sensíveis

Dado que o usuário informa dados físicos ou dores,
Quando eventos forem enviados,
Então esses valores não devem aparecer no payload.

## 13. Decisão registrada

O onboarding deve ser mensurável sem transformar dados físicos e sensíveis do usuário em eventos de analytics.
