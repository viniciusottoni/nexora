---
title: US-034 — Editar perfil após onboarding
sidebar_position: 34
---

# US-034 — Editar perfil após onboarding

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-034 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile existente |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **editar meu perfil após o onboarding**,

para **atualizar minha realidade sem refazer todo o fluxo inicial**.

---

## 3. Contexto

A rotina do usuário pode mudar: novo equipamento, menos tempo, dor, novo objetivo ou mudança de local. O AWAKEN deve permitir atualização do perfil para manter a personalização real.

---

## 4. Objetivo

Permitir edição controlada das principais informações do perfil após o onboarding.

---

## 5. Escopo

### Entra nesta US

- Acessar edição de perfil.
- Editar objetivo.
- Editar nível.
- Editar dados físicos básicos.
- Editar local, equipamentos, tempo, dias disponíveis, limitações e preferências.
- Salvar alterações.
- Usar novos dados em quests futuras.

### Fora desta US

- Reexecutar onboarding completo automaticamente.
- Histórico detalhado de alterações.
- Impacto retroativo em quests já concluídas.
- Alteração de conta/e-mail.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Apenas usuário com acesso ativo pode editar perfil. |
| RN-002 | Campos obrigatórios não podem ficar vazios. |
| RN-003 | Alterações devem afetar apenas quests futuras. |
| RN-004 | Alterar perfil não deve apagar progresso. |
| RN-005 | Limitações físicas continuam tendo prioridade sobre preferências. |
| RN-006 | Acesso expirado deve bloquear edição e direcionar para paywall. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode editar. |
| Usuário em Trial | Pode editar. |
| Premium Mensal | Pode editar. |
| Premium Anual | Pode editar. |
| Trial expirado | Não pode editar durante bloqueio. |
| Assinatura expirada | Não pode editar durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário acessa perfil/configurações.
2. Toca em editar perfil de treino.
3. App carrega dados atuais.
4. Usuário altera campos desejados.
5. App valida dados obrigatórios.
6. Backend salva alterações.
7. App confirma sucesso.

---

## 9. Fluxos alternativos

### 9.1. Acesso expirado

O app deve bloquear edição e direcionar para paywall.

### 9.2. Campo obrigatório removido

O app deve impedir salvamento e destacar o campo pendente.

### 9.3. Quest do dia já gerada

Alterações podem valer apenas para próximas quests ou exigir regeneração conforme regra do EPIC-006.

---

## 10. Estados esperados

- carregando perfil;
- editando;
- salvando;
- salvo com sucesso;
- campo inválido;
- acesso expirado;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de edição de perfil.
- Formulários reutilizáveis do onboarding.
- Validações.
- Mensagens de sucesso e erro.
- Estado bloqueado por acesso expirado.

---

## 12. Impacto no Backend

- Endpoint para atualizar perfil.
- Validação dos campos.
- Garantir que alterações não removam histórico.
- Registrar atualização de perfil.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campos editáveis:

- goal;
- experienceLevel;
- age;
- heightCm;
- weightKg;
- trainingLocation;
- equipmentAvailable;
- availableMinutesPerWorkout;
- availableDaysPerWeek;
- limitations;
- trainingPreferences.

---

## 14. Impacto em Gamificação

- Pode mudar quests futuras.
- Não altera XP, rank ou histórico já conquistado.
- Não deve apagar streak.

---

## 15. Impacto em Monetização

- Edição exige acesso ativo.
- Usuário bloqueado deve ser direcionado ao paywall.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Campos e mensagens de edição. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PUT /api/users/me/profile
```

Request conceitual:

```json
{
  "goal": "gain_strength",
  "availableMinutesPerWorkout": 30,
  "limitations": ["knee"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| profile_edit_started | Quando usuário abre edição. |
| profile_updated | Quando perfil é salvo. |
| profile_update_failed | Quando salvamento falha. |

---

## 19. Critérios de aceite

### CA-001 — Editar com acesso ativo

Dado que o usuário possui acesso ativo,

Quando alterar e salvar o perfil,

Então as alterações devem ser persistidas.

### CA-002 — Acesso expirado

Dado que o trial expirou,

Quando tentar editar perfil,

Então o app deve bloquear e direcionar para paywall.

### CA-003 — Histórico preservado

Dado que o usuário alterou perfil,

Quando consultar histórico,

Então progresso anterior deve permanecer.

---

## 20. Critérios de teste para QA

- editar objetivo;
- editar equipamentos;
- editar limitações;
- tentar salvar campo obrigatório vazio;
- editar com acesso expirado;
- validar que histórico permanece;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O usuário deve conseguir atualizar o perfil após onboarding, mas apenas com acesso ativo e sem afetar histórico ou progresso já conquistado.
