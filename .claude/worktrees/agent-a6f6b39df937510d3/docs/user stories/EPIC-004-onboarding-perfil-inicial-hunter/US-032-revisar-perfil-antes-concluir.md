---
title: US-032 — Revisar perfil antes de concluir
sidebar_position: 32
---

# US-032 — Revisar perfil antes de concluir

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-032 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Respostas do onboarding |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **revisar meu perfil antes de concluir**,

para **corrigir erros antes que o sistema gere minha primeira quest**.

---

## 3. Contexto

Antes de salvar o perfil inicial como concluído, o usuário deve revisar objetivo, nível, local, equipamentos, tempo, dias disponíveis e limitações para evitar treinos incompatíveis.

---

## 4. Objetivo

Exibir um resumo das respostas do onboarding e permitir voltar para editar qualquer etapa antes da conclusão.

---

## 5. Escopo

### Entra nesta US

- Tela de revisão do perfil.
- Exibição das respostas principais.
- Ação para editar uma etapa específica.
- CTA para confirmar perfil.
- Validação de campos obrigatórios antes da conclusão.

### Fora desta US

- Edição de perfil pós-onboarding.
- Geração da quest.
- Histórico de alterações.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A revisão deve exibir todas as respostas obrigatórias. |
| RN-002 | O usuário deve poder voltar para corrigir uma etapa. |
| RN-003 | Perfil não pode ser concluído com campo obrigatório ausente. |
| RN-004 | Limitações e equipamentos devem aparecer de forma clara. |
| RN-005 | Dados sensíveis devem aparecer apenas para o próprio usuário. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode revisar. |
| Usuário em Trial | Pode revisar. |
| Premium Mensal | Pode revisar. |
| Premium Anual | Pode revisar. |
| Trial expirado | Não pode revisar durante bloqueio. |
| Assinatura expirada | Não pode revisar durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário conclui as perguntas obrigatórias.
2. App exibe tela de resumo.
3. Usuário revisa as respostas.
4. Se necessário, toca em editar etapa.
5. Usuário confirma o perfil.
6. App segue para salvamento final.

---

## 9. Fluxos alternativos

### 9.1. Campo obrigatório ausente

O app deve destacar o campo pendente e impedir conclusão.

### 9.2. Usuário edita uma etapa

Após editar, deve voltar para revisão com dados atualizados.

---

## 10. Estados esperados

- carregando resumo;
- resumo completo;
- campo pendente;
- editando etapa;
- confirmando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de resumo.
- Cards de cada resposta.
- Botões de editar.
- CTA de confirmar.
- Textos localizados.

---

## 12. Impacto no Backend

- Pode validar perfil parcial antes da conclusão.
- Pode retornar dados consolidados do perfil.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campos revisados:

- goal;
- experienceLevel;
- trainingLocation;
- equipmentAvailable;
- availableMinutesPerWorkout;
- availableDaysPerWeek;
- limitations.

---

## 14. Impacto em Gamificação

- Prepara perfil Hunter e primeira quest.
- Não concede XP.

---

## 15. Impacto em Monetização

- Disponível apenas com acesso ativo.
- Ajuda o trial entregar personalização confiável.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Resumo e CTAs. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/users/me/profile/onboarding-summary
```

Response conceitual:

```json
{
  "isComplete": true,
  "missingFields": []
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_review_viewed | Quando tela de revisão é exibida. |
| onboarding_step_edited | Quando usuário volta para editar uma etapa. |

---

## 19. Critérios de aceite

### CA-001 — Resumo completo

Dado que o usuário respondeu campos obrigatórios,

Quando chegar à revisão,

Então deve ver todas as respostas principais.

### CA-002 — Editar etapa

Dado que o usuário quer corrigir resposta,

Quando tocar em editar,

Então deve voltar para a etapa correspondente.

---

## 20. Critérios de teste para QA

- revisar perfil completo;
- editar objetivo;
- editar equipamentos;
- campo obrigatório ausente;
- confirmar perfil;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A revisão do perfil é obrigatória antes de concluir o onboarding para reduzir erros e aumentar confiança na primeira quest.
