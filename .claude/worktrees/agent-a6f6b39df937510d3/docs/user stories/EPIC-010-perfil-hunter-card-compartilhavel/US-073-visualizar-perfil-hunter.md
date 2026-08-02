---
title: US-073 — Visualizar perfil Hunter
sidebar_position: 73
---

# US-073 — Visualizar perfil Hunter

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-073 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | HunterProgress e Subscription |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com acesso ativo**,

quero **visualizar meu Perfil Hunter**,

para **acompanhar minha evolução e sentir que estou progredindo dentro do AWAKEN**.

---

## 3. Contexto

O Perfil Hunter é a representação gamificada do usuário. Ele centraliza identidade, progresso e evolução, funcionando como reforço visual da jornada.

---

## 4. Objetivo

Criar uma tela de perfil que exiba informações principais do Hunter com visual dark, épico, legível e compatível com o MVP.

---

## 5. Escopo

### Entra nesta US

- Tela de Perfil Hunter.
- Exibição de nome ou apelido do usuário.
- Exibição de rank, level, XP, streak e atributos.
- Estado para usuário em trial.
- Estado para assinante mensal/anual.
- Estado limitado para acesso expirado.

### Fora desta US

- Ranking social.
- Feed interno.
- Avatar 3D.
- Customização avançada.
- Card compartilhável, tratado em US-077.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Perfil completo só deve ser exibido para usuário com acesso ativo. |
| RN-002 | Trial ativo pode visualizar perfil funcional. |
| RN-003 | Assinante mensal/anual pode visualizar perfil completo. |
| RN-004 | Acesso expirado deve ver estado limitado com CTA para assinatura. |
| RN-005 | Dados sensíveis como peso, idade e limitações não devem aparecer no perfil público ou card. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode visualizar perfil. |
| Usuário em Trial | Pode visualizar perfil funcional. |
| Premium Mensal | Pode visualizar perfil completo. |
| Premium Anual | Pode visualizar perfil completo. |
| Trial expirado | Visualiza estado limitado. |
| Assinatura expirada | Visualiza estado limitado. |

---

## 8. Fluxo principal

1. Usuário acessa a aba ou rota de perfil.
2. App valida status de acesso.
3. App carrega dados agregados do Hunter.
4. Perfil é renderizado com progresso atual.
5. Usuário pode visualizar evolução e acessar card compartilhável quando disponível.

---

## 9. Fluxos alternativos

### 9.1. Acesso expirado

O app deve exibir estado limitado com CTA de assinatura.

### 9.2. Dados de progresso indisponíveis

O app deve exibir estado de erro controlado e permitir tentar novamente.

---

## 10. Estados esperados

- carregando;
- perfil carregado;
- sem progresso inicial;
- acesso limitado;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Tela de Perfil Hunter.
- Componentes de header, rank, XP, level, streak e atributos.
- Estado visual para trial, assinante e bloqueado.
- Textos localizados.

---

## 12. Impacto no Backend

- Endpoint de perfil agregado.
- Validação de acesso.
- Retorno de progresso atual.
- Tratamento de erro padronizado.

---

## 13. Impacto no Banco de Dados

Entidades principais:

- User;
- UserProfile;
- HunterProgress;
- HunterAttributes;
- Subscription.

---

## 14. Impacto em Gamificação

- Exibe evolução do usuário.
- Reforça rank, level, XP, streak e atributos.
- Não concede XP apenas por visualizar.

---

## 15. Impacto em Monetização

- Trial vê perfil funcional.
- Assinantes podem ter visual mais completo.
- Acesso expirado mostra CTA para assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Labels do perfil. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

```txt
GET /api/hunter/profile
```

Response conceitual:

```json
{
  "displayName": "Vinícius",
  "rank": "E",
  "level": 3,
  "xp": 240,
  "xpToNextLevel": 500,
  "streakDays": 4,
  "accessStatus": "trial_active"
}
```

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_profile_viewed | Quando perfil Hunter é exibido. |

---

## 19. Critérios de aceite

### CA-001 — Perfil com acesso ativo

Dado que o usuário possui trial ou assinatura ativa,

Quando acessar o perfil,

Então deve visualizar o Perfil Hunter com dados atuais.

### CA-002 — Acesso expirado

Dado que o acesso expirou,

Quando acessar o perfil,

Então deve ver estado limitado com CTA de assinatura.

---

## 20. Critérios de teste para QA

- perfil com trial ativo;
- perfil com assinatura mensal;
- perfil com assinatura anual;
- acesso expirado;
- erro de conexão;
- ausência de dados sensíveis;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O Perfil Hunter é a tela central de identidade e progresso do usuário, disponível de forma funcional no trial e completa para assinantes ativos.
