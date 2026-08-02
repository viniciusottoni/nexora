---
title: US-142 — Informar limitações físicas para filtro do catálogo
sidebar_position: 142
---

# US-142 — Informar limitações físicas para filtro do catálogo

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-142 |
| Épico | EPIC-004 — Onboarding e Perfil Inicial do Hunter |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | EPIC-005 — Catálogo de exercícios |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **informar minhas limitações físicas**,

para **o AWAKEN filtrar exercícios do catálogo que não devo realizar com segurança**.

---

## 3. Contexto

A etapa 7/8 coleta limitações físicas. Diferente das dores físicas da US-030, limitações servem como filtro forte do catálogo de exercícios, evitando prescrições inadequadas.

---

## 4. Objetivo

Coletar limitações físicas relevantes e salvar no perfil para filtrar exercícios contraindicados durante a geração da quest.

---

## 5. Escopo

### Entra nesta US

- Tela 7/8 de limitações físicas.
- Seleção múltipla.
- Opção “não tenho limitações”.
- Opções a definir com o catálogo do EPIC-005.
- Aviso de que o app não substitui orientação profissional.
- Salvamento no perfil.

### Fora desta US

- Diagnóstico médico.
- Prescrição fisioterapêutica.
- Tratamento clínico.
- Definição final do catálogo, que pertence ao EPIC-005.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O usuário deve informar limitações ou escolher “não tenho limitações”. |
| RN-002 | Limitações físicas devem filtrar exercícios contraindicados. |
| RN-003 | Opções devem ser compatíveis com tags do catálogo de exercícios. |
| RN-004 | Limitações têm prioridade sobre objetivo, preferência e intensidade. |
| RN-005 | Dados não devem aparecer no card compartilhável. |
| RN-006 | A tela deve apresentar aviso de responsabilidade profissional. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode responder. |
| Usuário em Trial | Pode responder. |
| Premium Mensal | Pode responder. |
| Premium Anual | Pode responder. |
| Trial expirado | Não pode responder. |
| Assinatura expirada | Não pode responder. |

---

## 8. Fluxo principal

1. Usuário chega à etapa 7/8.
2. App exibe lista de limitações físicas.
3. Usuário seleciona limitações ou “não tenho limitações”.
4. App valida consistência.
5. App salva resposta no perfil.
6. App avança para etapa 8/8.

---

## 9. Fluxos alternativos

### 9.1. Sem limitações

Se o usuário escolher “não tenho limitações”, outras opções devem ser limpas.

### 9.2. Opções ainda não definitivas

As opções devem ser ajustadas quando o catálogo de exercícios definir tags finais.

---

## 10. Estados esperados

- pronto para seleção;
- limitações selecionadas;
- sem limitações;
- erro de consistência;
- salvando;
- erro de conexão.

---

## 11. Impacto no Frontend Flutter

- Tela 7/8 de seleção múltipla.
- Opção “não tenho limitações”.
- Aviso de responsabilidade.
- Textos localizados.

---

## 12. Impacto no Backend

- Validar tags de limitação.
- Salvar em UserProfile.
- Disponibilizar para filtro do catálogo.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campo:

- physicalLimitations.

Relacionamento lógico:

- Exercise.contraindicationTags.

---

## 14. Impacto em Gamificação

- Pode alterar exercícios e dificuldade das quests.
- Não concede XP diretamente.
- Evita que a gamificação incentive esforço inseguro.

---

## 15. Impacto em Monetização

- Demonstra personalização real e segura durante o trial.
- Não altera assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Limitações, aviso e validações. |
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
  "physicalLimitations": ["knee_problem", "no_impact"]
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| onboarding_step_completed | Quando etapa 7/8 é salva. |

---

## 19. Critérios de aceite

### CA-001 — Limitações salvas

Dado que o usuário seleciona limitações,

Quando avançar,

Então as limitações devem ser salvas no perfil.

### CA-002 — Filtro futuro

Dado que uma limitação está salva,

Quando a quest for gerada,

Então exercícios contraindicados devem ser filtrados conforme catálogo.

---

## 20. Critérios de teste para QA

- selecionar sem limitações;
- selecionar uma limitação;
- selecionar múltiplas limitações;
- validar aviso de responsabilidade;
- validar consistência com catálogo;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Limitações físicas são filtros fortes do catálogo e devem ter prioridade sobre preferências, objetivo e intensidade da quest.
