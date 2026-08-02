---
title: US-101 — Registrar logs com correlationId no backend
sidebar_position: 101
---

# US-101 — Registrar logs com correlationId no backend

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-101 |
| Épico | EPIC-014 — Analytics, Crash, Logs e Observabilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Engenharia e QA |
| Integrações | Logs de backend |
| Status | Planejada |

## 2. História do usuário

Como **time de engenharia**, quero **registrar logs estruturados com correlationId**, para **investigar erros de API e fluxos críticos sem expor dados pessoais desnecessários**.

## 3. Contexto

Quando um fluxo falha, como login, assinatura, geração de quest ou conclusão de treino, o time precisa localizar a requisição no backend e conectar erro, usuário e operação de forma segura.

## 4. Objetivo

Implementar rastreabilidade básica no backend usando correlationId, logs estruturados e tratamento padronizado de erro.

## 5. Escopo

### Entra nesta US

- Gerar ou propagar correlationId por request.
- Incluir correlationId na resposta de erro.
- Logar início/fim de operações críticas.
- Logar erro com código funcional.
- Evitar dados sensíveis nos logs.

### Fora desta US

- Observabilidade distribuída completa.
- Tracing avançado.
- Data lake de logs.
- Exposição de stack trace ao usuário.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda request crítica deve ter correlationId. |
| RN-002 | Erros de API devem retornar correlationId ao cliente. |
| RN-003 | Logs não devem expor dados pessoais desnecessários. |
| RN-004 | Logs de conclusão de quest e mudança comercial devem ser rastreáveis. |
| RN-005 | Código de erro funcional deve acompanhar o log quando aplicável. |

## 7. Impacto Backend

- Middleware de correlationId.
- Logs estruturados por request.
- Padronização de responses de erro.
- Integração com logs de assinatura, quest e recompensa.

## 8. Response de erro sugerido

```json
{
  "code": "QUEST_GENERATION_FAILED",
  "message": "Não foi possível gerar a quest agora.",
  "correlationId": "uuid"
}
```

## 9. Impacto Flutter

- Capturar correlationId em erros.
- Exibir mensagem amigável.
- Permitir suporte futuro usando correlationId.

## 10. Impacto QA

- Validar correlationId em erro 4xx/5xx.
- Validar ausência de dados sensíveis.
- Validar logs de quest, assinatura e onboarding.

## 11. Critérios de aceite

### CA-001 — correlationId presente

Dado que uma API crítica falha,
Quando o backend retornar erro,
Então a resposta deve conter correlationId.

### CA-002 — Log seguro

Dado que um erro ocorre em onboarding,
Quando o log for consultado,
Então não deve conter dados sensíveis como peso, dores ou limitações.

## 12. Decisão registrada

Logs de backend precisam permitir investigação rápida, mantendo privacidade e evitando vazamento de dados sensíveis.
