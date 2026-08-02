---
title: US-076 — Usar avatar básico ou imagem de perfil
sidebar_position: 76
---

# US-076 — Usar avatar básico ou imagem de perfil

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-076 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile.avatar |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **usar um avatar básico ou imagem de perfil**,

para **personalizar meu Perfil Hunter e card compartilhável**.

---

## 3. Contexto

Avatar aumenta identificação, mas não é essencial para o MVP. No primeiro momento, pode existir avatar padrão, iniciais do nome ou upload simples, conforme viabilidade.

---

## 4. Objetivo

Permitir uma representação visual básica do usuário no Perfil Hunter e no card compartilhável.

---

## 5. Escopo

### Entra nesta US

- Exibir avatar padrão.
- Exibir iniciais do usuário como fallback.
- Permitir imagem de perfil simples, se habilitado.
- Usar avatar no perfil e card.
- Garantir fallback quando imagem falhar.

### Fora desta US

- Avatar 3D.
- Editor avançado.
- Customização de roupas/equipamentos.
- Moderação avançada de imagem.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Avatar básico é P1. |
| RN-002 | Se não houver imagem, usar fallback visual. |
| RN-003 | Imagem de perfil não deve expor dados sensíveis. |
| RN-004 | O avatar pode aparecer no card compartilhável. |
| RN-005 | Falha ao carregar imagem não deve quebrar o perfil. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode configurar. |
| Usuário em Trial | Pode visualizar avatar padrão. |
| Premium Mensal | Pode visualizar/configurar se habilitado. |
| Premium Anual | Pode visualizar/configurar se habilitado. |
| Trial expirado | Visual limitado. |
| Assinatura expirada | Visual limitado. |

---

## 8. Fluxo principal

1. Usuário acessa Perfil Hunter.
2. App verifica se há imagem de perfil.
3. Se houver, exibe imagem.
4. Se não houver, exibe avatar padrão ou iniciais.
5. Card compartilhável usa a mesma representação.

---

## 9. Fluxos alternativos

### 9.1. Imagem falha

O app deve exibir fallback sem quebrar a tela.

### 9.2. Upload desabilitado no MVP

O app deve usar apenas avatar padrão ou iniciais.

---

## 10. Estados esperados

- avatar padrão;
- imagem carregada;
- fallback por falha;
- acesso limitado;
- erro controlado.

---

## 11. Impacto no Frontend Flutter

- Componente de avatar.
- Fallback com iniciais.
- Uso no perfil e card.
- Cache de imagem, se aplicável.

---

## 12. Impacto no Backend

- Retornar URL ou identificador de avatar quando houver.
- Persistir referência da imagem se upload for habilitado.

---

## 13. Impacto no Banco de Dados

Entidade: UserProfile.

Campos sugeridos:

- avatarUrl;
- avatarType.

---

## 14. Impacto em Gamificação

- Aumenta identidade do Hunter.
- Não altera XP, rank ou atributos.

---

## 15. Impacto em Monetização

- Pode virar recurso premium visual no futuro.
- No MVP, não deve bloquear perfil básico.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens de avatar/imagem. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/profile
```

Response parcial:

```json
{
  "avatarUrl": "https://cdn.awaken.app/avatar.png",
  "avatarType": "uploaded"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| avatar_viewed | Quando avatar aparece no perfil. |

---

## 19. Critérios de aceite

### CA-001 — Avatar exibido

Dado que o usuário acessa o perfil,

Quando houver avatar ou fallback,

Então uma representação visual deve ser exibida.

### CA-002 — Falha de imagem

Dado que a imagem não carrega,

Quando o perfil renderizar,

Então o fallback deve ser exibido.

---

## 20. Critérios de teste para QA

- avatar padrão;
- iniciais do nome;
- imagem válida;
- falha de imagem;
- card com avatar;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Avatar básico é P1 e deve oferecer identidade visual simples sem depender de customização avançada.
