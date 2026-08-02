---
title: US-075 — Definir classe inicial
sidebar_position: 75
---

# US-075 — Definir classe inicial

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-075 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P1 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | UserProfile e HunterProgress |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **ter uma classe inicial no meu Perfil Hunter**,

para **sentir que minha jornada tem identidade e estilo próprio**.

---

## 3. Contexto

A classe inicial aumenta imersão, mas não é obrigatória para o funcionamento principal do MVP. Por isso, é P1 e deve ser simples, sem criar sistema avançado de classes.

---

## 4. Objetivo

Definir e exibir uma classe inicial simples no Perfil Hunter, baseada no perfil ou definida por regra inicial.

---

## 5. Escopo

### Entra nesta US

- Exibir classe inicial no perfil.
- Definir classe por regra simples ou valor padrão.
- Localizar o nome da classe.
- Permitir evolução futura sem acoplar regras complexas.

### Fora desta US

- Árvore de classes.
- Troca manual de classe.
- Skills por classe.
- Balanceamento avançado.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Classe inicial é P1 e não bloqueia o MVP. |
| RN-002 | Classe deve ser exibida apenas para usuário com acesso ativo. |
| RN-003 | Classe não deve alterar cálculo de XP no MVP. |
| RN-004 | Se não houver regra definida, usar classe padrão. |
| RN-005 | Classe deve ser localizada em PT-BR, EN e ES. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não vê classe. |
| Usuário em Trial | Pode ver classe inicial funcional. |
| Premium Mensal | Pode ver classe. |
| Premium Anual | Pode ver classe. |
| Trial expirado | Visual limitado. |
| Assinatura expirada | Visual limitado. |

---

## 8. Fluxo principal

1. Usuário acessa Perfil Hunter.
2. App carrega dados do perfil.
3. Sistema identifica classe inicial.
4. Classe é exibida no header ou card do perfil.

---

## 9. Fluxos alternativos

### 9.1. Classe não definida

O sistema deve exibir classe padrão, como “Hunter Iniciante”.

### 9.2. Acesso expirado

Classe pode ficar oculta ou limitada conforme estado bloqueado.

---

## 10. Estados esperados

- classe carregada;
- classe padrão;
- acesso limitado;
- erro de carregamento.

---

## 11. Impacto no Frontend Flutter

- Exibir badge de classe.
- Estilo visual discreto no perfil.
- Textos localizados.

---

## 12. Impacto no Backend

- Retornar classe atual ou padrão.
- Salvar classe se houver regra inicial definida.

---

## 13. Impacto no Banco de Dados

Entidades:

- UserProfile;
- HunterProgress.

Campo sugerido:

- hunterClass.

---

## 14. Impacto em Gamificação

- Aumenta imersão e identidade.
- Não altera atributos, XP ou rank no MVP.

---

## 15. Impacto em Monetização

- Pode receber visual premium em fases futuras.
- Não altera assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nome da classe. |
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
  "hunterClass": "beginner_hunter"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_profile_viewed | Quando classe aparece no perfil. |

---

## 19. Critérios de aceite

### CA-001 — Classe exibida

Dado que o usuário tem acesso ativo,

Quando acessar o perfil,

Então deve visualizar sua classe inicial se a feature estiver habilitada.

### CA-002 — Classe padrão

Dado que a classe não foi calculada,

Quando o perfil carregar,

Então deve ser exibida uma classe padrão.

---

## 20. Critérios de teste para QA

- usuário com classe definida;
- usuário sem classe definida;
- acesso expirado;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> Classe inicial é P1 e serve para imersão visual, sem impacto no cálculo de XP, rank ou atributos no MVP.
