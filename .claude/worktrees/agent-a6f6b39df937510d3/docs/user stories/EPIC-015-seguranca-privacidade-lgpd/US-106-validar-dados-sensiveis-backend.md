---
title: US-106 — Validar dados sensíveis no backend
sidebar_position: 106
---

# US-106 — Validar dados sensíveis no backend

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-106 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário autenticado |
| Planos impactados | Trial, Mensal e Anual |
| Dependência principal | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Status | Planejada |

---

## 2. História do usuário

Como **sistema do AWAKEN**,

quero **validar dados pessoais, físicos, dores e limitações no backend**,

para **proteger o usuário, evitar dados inválidos e impedir que o app dependa apenas de validação no frontend**.

---

## 3. Contexto

O onboarding coleta dados que influenciam treinos, catálogo, limitações e cálculo de progressão. O frontend ajuda na experiência, mas a regra de integridade precisa estar no backend.

---

## 4. Objetivo

Centralizar validações obrigatórias no backend para dados usados em personalização, treino e segurança do usuário.

---

## 5. Escopo

### Entra nesta US

- Validar idade, altura, peso e sexo biológico informado.
- Validar objetivo, nível e tempo treinando.
- Validar tipo corporal.
- Validar limitações e dores cadastradas.
- Rejeitar payloads inválidos ou incoerentes.
- Retornar erros funcionais claros.
- Evitar expor dados sensíveis em logs e analytics.

### Fora desta US

- Diagnóstico clínico.
- Validação médica avançada.
- Prescrição fisioterapêutica.
- Score de risco clínico.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Dados de entrada devem ser validados no backend. |
| RN-002 | Payload inválido deve ser recusado com erro funcional. |
| RN-003 | O backend não deve confiar apenas no frontend. |
| RN-004 | Logs não devem expor dados físicos detalhados desnecessariamente. |
| RN-005 | Dados usados para gerar treino devem estar em formato válido. |
| RN-006 | Valores fora de faixa aceitável devem ser bloqueados ou exigir correção. |
| RN-007 | Limitações e dores devem ser validadas contra lista/taxonomia permitida, quando aplicável. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não envia dados sensíveis de perfil. |
| Usuário em Trial | Pode enviar dados se acesso ativo e aceite válido. |
| Premium Mensal | Pode enviar dados se acesso ativo. |
| Premium Anual | Pode enviar dados se acesso ativo. |
| Trial expirado | Não atualiza dados para gerar treino. |
| Assinatura expirada | Não atualiza dados para gerar treino. |

---

## 8. Fluxo principal

1. Usuário preenche onboarding ou edita perfil.
2. App envia dados ao backend.
3. Backend valida campos obrigatórios e formatos.
4. Backend valida faixas e taxonomias permitidas.
5. Se válido, salva dados.
6. Se inválido, retorna erro funcional para correção.

---

## 9. Fluxos alternativos

### 9.1. Payload inválido

Backend recusa a requisição e retorna código de erro específico.

### 9.2. Campo obrigatório ausente

Backend informa campo faltante de forma segura.

### 9.3. Dados incoerentes

Backend rejeita ou solicita revisão conforme regra definida.

---

## 10. Estados esperados

- validando;
- dados aceitos;
- campo obrigatório ausente;
- formato inválido;
- faixa inválida;
- taxonomia inválida;
- acesso bloqueado.

---

## 11. Impacto Flutter

- Exibir erros retornados pelo backend.
- Manter validação local para UX, mas sem depender só dela.
- Não enviar dados sensíveis para analytics.

---

## 12. Impacto Backend

- Validators para onboarding/perfil.
- Responses padronizadas de erro.
- Sanitização de logs.
- Validação antes de gerar quest.

---

## 13. Impacto DB

Entidades:

- UserProfile;
- UserLimitation;
- UserPainPoint.

Campos devem ser salvos apenas após validação.

---

## 14. Impacto Gamificação

- Impede geração de treino e atributos com dados inválidos.
- Não concede XP.

---

## 15. Impacto Monetização

- Usuário bloqueado por acesso expirado não deve usar atualização de perfil para gerar treino.

---

## 16. Contrato API sugerido

```txt
PUT /api/users/me/profile
```

Erro conceitual:

```json
{
  "code": "INVALID_PROFILE_DATA",
  "message": "Revise os dados informados.",
  "correlationId": "uuid"
}
```

---

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| profile_validation_failed | Quando validação falha. |

Payload deve conter apenas código genérico, sem valores físicos ou limitações.

---

## 18. Critérios de aceite

### CA-001 — Validação no backend

Dado que o usuário envia dados de perfil,
Quando o payload chegar ao backend,
Então todos os campos obrigatórios e formatos devem ser validados antes de salvar.

### CA-002 — Dados inválidos recusados

Dado que o payload possui valor fora da faixa aceita,
Quando a requisição for processada,
Então o backend deve recusar e retornar erro funcional.

---

## 19. Critérios de teste QA

- payload válido;
- campo obrigatório ausente;
- peso inválido;
- altura inválida;
- tipo corporal inválido;
- limitação não reconhecida;
- acesso expirado;
- logs sem dados sensíveis.

---

## 20. Decisão registrada

> Dados físicos e limitações precisam ser validados no backend antes de influenciar treinos, quests ou progressão.
