---
title: US-033 — Salvar perfil inicial
sidebar_position: 33
---

# US-033 — Salvar perfil inicial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-033 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile completo |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **salvar meu perfil inicial**,

para **gerar minha primeira quest diária personalizada**.

---

## 3. Contexto

Após responder e revisar o onboarding, o perfil precisa ser consolidado como completo. Esse é o gatilho para liberar a geração da primeira quest compatível com a realidade do usuário.

---

## 4. Objetivo

Salvar o perfil inicial como concluído e preparar o usuário para a primeira quest.

---

## 5. Escopo

### Entra nesta US

- Validação final dos campos obrigatórios.
- Persistência do UserProfile.
- Marcação de `onboardingCompletedAt`.
- Redirecionamento para geração ou visualização da primeira quest.
- Mensagem de sucesso.

### Fora desta US

- Geração completa da quest.
- Edição pós-onboarding.
- Cálculo de XP.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Perfil só pode ser concluído com campos obrigatórios preenchidos. |
| RN-002 | Usuário sem acesso ativo não pode concluir onboarding. |
| RN-003 | Ao concluir, o sistema deve registrar data de conclusão. |
| RN-004 | Perfil concluído habilita geração da primeira quest. |
| RN-005 | Concluir onboarding não concede XP. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode salvar. |
| Usuário em Trial | Pode salvar. |
| Premium Mensal | Pode salvar. |
| Premium Anual | Pode salvar. |
| Trial expirado | Não pode salvar durante bloqueio. |
| Assinatura expirada | Não pode salvar durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário revisa o perfil.
2. Toca em concluir onboarding.
3. App envia dados finais ao backend.
4. Backend valida campos obrigatórios.
5. Backend salva perfil e marca onboarding como concluído.
6. App direciona para primeira quest.

---

## 9. Fluxos alternativos

### 9.1. Campo obrigatório faltando

Backend retorna campos pendentes e app direciona para correção.

### 9.2. Acesso expirou durante onboarding

App deve interromper conclusão e direcionar para paywall.

---

## 10. Estados esperados

- pronto para concluir;
- salvando;
- salvo com sucesso;
- campos pendentes;
- acesso expirado;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- CTA de concluir onboarding.
- Loading de salvamento.
- Tratamento de erros.
- Redirecionamento para quest.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar perfil completo.
- Salvar UserProfile.
- Registrar `onboardingCompletedAt`.
- Retornar próxima rota sugerida.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campos:

- onboardingCompletedAt;
- goal;
- experienceLevel;
- trainingLocation;
- equipmentAvailable;
- availableMinutesPerWorkout;
- availableDaysPerWeek;
- limitations.

---

## 14. Impacto em Gamificação

- Habilita geração da primeira quest.
- Dispara o cálculo de Rank/RankScore e atributos iniciais (US-156), com teto Rank B / RankScore 48 e Level 1.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Requer acesso ativo.
- Se trial expirar durante o fluxo, deve bloquear conclusão.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de sucesso e erro. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/users/me/profile/complete-onboarding
```

Response conceitual:

```json
{
  "onboardingCompleted": true,
  "nextRoute": "daily_quest"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_completed | Quando o perfil inicial é salvo com sucesso. |

---

## 19. Critérios de aceite

### CA-001 — Perfil salvo

Dado que o usuário preencheu campos obrigatórios,

Quando concluir onboarding,

Então o perfil deve ser salvo como completo.

### CA-002 — Quest habilitada

Dado que o perfil foi salvo,

Quando o usuário avançar,

Então deve seguir para primeira quest ou tela correspondente.

---

## 20. Critérios de teste para QA

- salvar perfil completo;
- tentar salvar com campo ausente;
- expirar acesso durante onboarding;
- falha de conexão;
- verificar `onboardingCompletedAt`;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Salvar perfil inicial conclui o onboarding e habilita a primeira quest personalizada, sem conceder XP diretamente.
