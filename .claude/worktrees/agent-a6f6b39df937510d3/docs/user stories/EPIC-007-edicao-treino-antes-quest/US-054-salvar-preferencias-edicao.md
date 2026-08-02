---
title: US-054 — Salvar preferência de tipo de treino
sidebar_position: 54
---

# US-054 — Salvar preferência de tipo de treino

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-054 |
| Épico | EPIC-007 — Edição de Treino Antes da Quest |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserWorkoutPreference |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **salvar minha preferência de tipo de treino**,

para **que o AWAKEN considere minha escolha em quests futuras sem eu precisar trocar sempre**.

---

## 3. Contexto

Como a única alteração permitida é trocar o tipo do treino inteiro, a preferência salva deve se limitar ao tipo escolhido com frequência, não a exercícios, séries, repetições ou tempo.

---

## 4. Objetivo

Registrar preferência simples de tipo de treino para orientar futuras gerações sem substituir regras de segurança e compatibilidade.

---

## 5. Escopo

### Entra nesta US

- Salvar preferência por Personalizado Individual.
- Salvar preferência por Treino de Regeneração.
- Salvar preferência por Programa específico, como Caminho de Saitama ou Perfect 2.
- Usar preferência como sinal secundário em quests futuras.

### Fora desta US

- Preferência por exercício individual.
- Preferência por séries, repetições, tempo ou descanso.
- IA avançada de aprendizado.
- Histórico detalhado de edição.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Preferência de tipo de treino é P1. |
| RN-002 | Preferência não pode sobrescrever limitações físicas. |
| RN-003 | Preferência não pode forçar programa indisponível. |
| RN-004 | Preferência deve ser usada como sinal secundário. |
| RN-005 | Não salvar preferência de exercício ou volume manual. |
| RN-006 | Usuário com acesso expirado não pode salvar nova preferência. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Usuário em Trial | Pode salvar se feature estiver habilitada. |
| Premium Mensal | Pode salvar se feature estiver habilitada. |
| Premium Anual | Pode salvar se feature estiver habilitada. |
| Trial expirado | Não pode salvar. |
| Assinatura expirada | Não pode salvar. |
| Visitante | Não pode salvar. |

---

## 8. Fluxo principal

1. Usuário altera tipo do treino.
2. App oferece opção discreta de lembrar preferência, se habilitada.
3. Usuário confirma.
4. Backend salva preferência de tipo/programa.
5. Geração futura pode considerar essa preferência.

---

## 9. Fluxos alternativos

### 9.1. Feature desabilitada

A troca de tipo funciona normalmente, mas nenhuma preferência é salva.

### 9.2. Programa indisponível futuramente

Preferência deve ser ignorada e o sistema deve gerar treino compatível.

---

## 10. Estados esperados

- preferência salva;
- preferência ignorada;
- feature desabilitada;
- acesso bloqueado;
- programa indisponível.

---

## 11. Impacto no Frontend Flutter

- Opção “lembrar esse tipo de treino”.
- Não adicionar fricção ao fluxo P0.
- Textos localizados.

---

## 12. Impacto no Backend

- Persistir preferência de tipo de treino.
- Validar disponibilidade do programa.
- Usar como sinal secundário na geração futura.

---

## 13. Impacto no Banco de Dados

Entidade sugerida:

- UserWorkoutPreference.

Campos:

- userId;
- preferredTrainingType;
- preferredProgramId;
- updatedAt.

---

## 14. Impacto em Gamificação

- Melhora aderência futura.
- Não concede XP.
- Não permite manipular XP por volume manual.

---

## 15. Impacto em Monetização

- Pode aumentar percepção de personalização.
- Recurso disponível apenas com acesso ativo.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos de preferência. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
POST /api/users/me/workout-preferences/training-type
```

Request conceitual:

```json
{
  "preferredTrainingType": "program",
  "preferredProgramId": "perfect_2"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| workout_type_preference_saved | Quando preferência é salva. |

---

## 19. Critérios de aceite

### CA-001 — Preferência salva

Dado que a feature está habilitada,

Quando usuário salvar preferência de tipo,

Então o backend deve persistir o tipo/programa escolhido.

### CA-002 — Sem preferência manual de volume

Dado que o usuário tenta salvar preferência de séries ou repetições,

Quando chegar ao backend,

Então deve ser recusada ou ignorada.

---

## 20. Critérios de teste para QA

- salvar preferência por regeneração;
- salvar preferência por Caminho de Saitama;
- salvar preferência por Perfect 2;
- programa futuro indisponível;
- feature desabilitada;
- acesso expirado;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Preferências de edição no EPIC-007 se limitam ao tipo de treino; não existem preferências de exercício individual ou volume manual.
