---
title: US-041 — Ver instruções simples do exercício com mídia
sidebar_position: 41
---

# US-041 — Ver instruções simples do exercício com mídia

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-041 |
| Épico | EPIC-005 — Catálogo de Exercícios e Regras de Compatibilidade |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário com acesso ativo |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | ExerciseCatalog |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **ver instruções simples e mídia do exercício**,

para **executar com mais segurança e confiança**.

---

## 3. Contexto

Durante a quest, o usuário precisa entender rapidamente como executar cada exercício. Instruções em PT-BR somadas a vídeo/GIF/imagem reduzem o risco de execução incorreta e reforçam a Sabedoria (consciência corporal).

---

## 4. Objetivo

Exibir nome PT-BR, instruções, dicas e mídia do exercício na tela de execução/instrução.

---

## 5. Escopo

### Entra nesta US

- Exibição de nome PT-BR, descrição e instruções.
- Exibição de mídia (vídeo, GIF ou imagem).
- Exibição de dicas quando houver.
- Indicação de variante (regressão/progressão) quando disponível.
- Textos localizados.

### Fora desta US

- Vídeos próprios de execução.
- Registro de conclusão e XP (EPIC-008/009).

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Só exercícios aprovados podem ser exibidos. |
| RN-002 | Deve haver pelo menos uma mídia (vídeo, GIF ou imagem). |
| RN-003 | As instruções devem estar no idioma do usuário, com fallback para PT-BR. |
| RN-004 | Iniciar a instrução antes do exercício pode conceder Sabedoria (EPIC-009), se a mecânica existir. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não acessa. |
| Usuário em Trial | Acessa. |
| Premium Mensal | Acessa. |
| Premium Anual | Acessa. |
| Trial/Assinatura expirados | Não acessam quest. |

---

## 8. Fluxo principal

1. Usuário abre um exercício na quest.
2. App exibe nome, instruções, dicas e mídia.
3. Usuário visualiza a variante, se houver.
4. Usuário inicia a execução.

---

## 9. Fluxos alternativos

### 9.1. Mídia indisponível

Se a mídia falhar ao carregar, exibir imagem/instrução textual como fallback.

### 9.2. Idioma sem tradução

Exibir PT-BR como fallback.

---

## 10. Estados esperados

- carregando;
- instrução pronta;
- mídia indisponível (fallback);
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela/componente de instrução do exercício.
- Player de vídeo/GIF e imagem.
- Indicação de variante.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint de detalhe do exercício aprovado.

---

## 13. Impacto no Banco de Dados

Entidade: `ExerciseCatalog`.

Campos: `NamePtBr`, `DescriptionPtBr`, `InstructionsPtBr`, `TipsPtBr`, `VideoUrl`, `ImageUrl`, `GifUrl`, `RegressionExerciseId`, `ProgressionExerciseId`.

---

## 14. Impacto em Gamificação

- Iniciar a instrução pode conceder +1 Sabedoria (EPIC-009), se a mecânica existir.

---

## 15. Impacto em Monetização

- Boa experiência de execução aumenta retenção no trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Instruções e dicas. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/exercises/{id}
```

Response conceitual:

```json
{
  "id": "exr_001",
  "namePtBr": "Agachamento livre",
  "instructionsPtBr": ["..."],
  "videoUrl": "Squat.mp4"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| exercise_started | Usuário inicia o exercício. |

---

## 19. Critérios de aceite

### CA-001 — Instruções exibidas

Dado que o usuário abre um exercício aprovado,

Quando a tela carregar,

Então devem aparecer nome PT-BR, instruções e mídia.

### CA-002 — Fallback de idioma

Dado que não há tradução no idioma do usuário,

Quando a tela carregar,

Então deve exibir PT-BR como fallback.

---

## 20. Critérios de teste para QA

### Frontend (Flutter)

- exibir instruções e mídia de um exercício aprovado;
- testar fallback de mídia indisponível;
- testar fallback de idioma;
- verificar indicação de variante;
- textos em PT-BR, EN e ES.

### Backend

- detalhe retorna apenas exercícios aprovados;
- exercício não aprovado retorna 404/erro adequado.

---

## ✅ Decisão registrada

> Instruções simples com mídia, em PT-BR e localizadas, são parte essencial da execução segura e reforçam a Sabedoria do Hunter.
