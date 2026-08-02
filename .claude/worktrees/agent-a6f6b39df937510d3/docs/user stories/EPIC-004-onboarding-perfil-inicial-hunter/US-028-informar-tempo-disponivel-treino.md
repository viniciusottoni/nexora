---
title: US-028 — Informar tempo disponível por treino
sidebar_position: 28
---

# US-028 — Informar tempo disponível por treino

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-028 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.availableMinutesPerWorkout |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar quanto tempo tenho por treino**,

para **receber quests compatíveis com minha rotina**.

---

## 3. Contexto

Treinos longos demais aumentam abandono. O tempo disponível deve limitar volume, séries, descansos e quantidade de exercícios da quest diária.

---

## 4. Objetivo

Coletar tempo disponível por treino e usar essa informação para dimensionar a quest.

---

## 5. Escopo

### Entra nesta US

- Seleção de duração por treino.
- Opções simples como 10, 20, 30, 40 e 50 minutos.
- Validação obrigatória.
- Salvamento no perfil.
- Uso futuro na geração da quest.

### Fora desta US

- Agenda diária detalhada.
- Integração com calendário.
- Cronômetro avançado.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Tempo por treino é obrigatório. |
| RN-002 | O tempo deve influenciar quantidade e volume dos exercícios. |
| RN-003 | O sistema não deve gerar quest com duração muito acima do informado. |
| RN-004 | O usuário pode editar o tempo depois, conforme US-034. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode responder. |
| Usuário em Trial | Pode responder. |
| Premium Mensal | Pode responder. |
| Premium Anual | Pode responder. |
| Trial expirado | Não pode alterar durante bloqueio. |
| Assinatura expirada | Não pode alterar durante bloqueio. |

---

## 8. Fluxo principal

1. Usuário chega à etapa de tempo por treino.
2. App exibe opções de duração.
3. Usuário seleciona uma opção.
4. App salva no perfil.
5. App avança para próxima etapa.

---

## 9. Fluxos alternativos

### 9.1. Sem seleção

O app deve exibir validação e impedir avanço.

### 9.2. Tempo muito curto

O sistema deve aceitar tempo curto, mas gerar quest proporcional.

---

## 10. Estados esperados

- pronto para seleção;
- tempo selecionado;
- erro de validação;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela de duração.
- Cards ou botões de tempo.
- Validação obrigatória.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar duração permitida.
- Salvar no UserProfile.
- Disponibilizar para geração de quest.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- availableMinutesPerWorkout.

---

## 14. Impacto em Gamificação

- Influencia tamanho da quest.
- Pode influenciar XP estimado pela duração.
- Não concede XP diretamente.

---

## 15. Impacto em Monetização

- Ajuda o trial entregar valor real e compatível com rotina.
- Não altera assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels de duração. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
PATCH /api/users/me/profile/onboarding
```

Request:

```json
{
  "availableMinutesPerWorkout": 30
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando tempo é salvo. |

---

## 19. Critérios de aceite

### CA-001 — Tempo salvo

Dado que o usuário seleciona tempo,

Quando avançar,

Então o tempo deve ser salvo no perfil.

### CA-002 — Sem seleção

Dado que nenhum tempo foi selecionado,

Quando tentar avançar,

Então o app deve exibir validação.

---

## 20. Critérios de teste para QA

- selecionar 10 min;
- selecionar 30 min;
- selecionar 50 min;
- avançar sem seleção;
- falha de conexão;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Tempo disponível por treino é obrigatório para dimensionar a quest e evitar treinos incompatíveis com a rotina do usuário.
