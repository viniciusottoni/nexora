---
title: US-002 — Navegar por uma estrutura base de telas
sidebar_position: 2
---

# US-002 — Navegar por uma estrutura base de telas

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-002 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários do app mobile |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | App Shell e roteamento Flutter |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **navegar por uma estrutura base de telas**,

para **usar o app sem confusão e ser direcionado corretamente conforme meu estado de acesso**.

---

## 3. Contexto

O AWAKEN terá múltiplos estados de usuário: visitante, usuário em trial, assinante mensal, assinante anual, trial expirado e assinatura expirada. A navegação precisa direcionar cada perfil para a tela correta sem loops, telas indevidas ou quebra de experiência.

---

## 4. Objetivo

Criar uma estrutura de navegação base que suporte os fluxos principais do MVP: abertura, trial, login, onboarding, home, quest, perfil, paywall e estados bloqueados.

---

## 5. Escopo

### Entra nesta US

- Definir rotas principais do MVP.
- Criar app shell inicial.
- Criar guards de navegação por estado de usuário.
- Definir comportamento para visitante.
- Definir comportamento para usuário com acesso ativo.
- Definir comportamento para usuário com acesso expirado.
- Preparar navegação para telas futuras sem implementá-las completamente.

### Fora desta US

- Implementação completa das telas de negócio.
- Lógica completa de assinatura.
- Lógica completa de autenticação.
- Deep links externos avançados.
- Navegação iOS específica.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Visitante deve ser direcionado para a experiência inicial de proposta/trial. |
| RN-002 | Usuário autenticado sem onboarding completo deve ser direcionado para onboarding, desde que tenha acesso ativo. |
| RN-003 | Usuário com acesso ativo e onboarding completo deve ser direcionado para Home/Quest. |
| RN-004 | Usuário com trial ou assinatura expirada deve ser direcionado para estado bloqueado/paywall. |
| RN-005 | O app não deve entrar em loop de navegação. |
| RN-006 | Rotas protegidas não devem ser acessadas por visitante. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode acessar rotas públicas. |
| Usuário em Trial | Pode acessar rotas protegidas do MVP. |
| Premium Mensal | Pode acessar rotas protegidas do MVP. |
| Premium Anual | Pode acessar rotas protegidas do MVP. |
| Trial expirado | Pode acessar rotas limitadas e paywall. |
| Assinatura expirada | Pode acessar rotas limitadas e paywall. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário abre o app.
2. O app carrega o estado local inicial.
3. O app identifica se existe sessão.
4. O app identifica status de acesso quando aplicável.
5. O app identifica se onboarding foi concluído.
6. O app direciona o usuário para a rota correta.

---

## 9. Fluxos alternativos

### 9.1. Usuário visitante

1. Não existe sessão válida.
2. App direciona para tela de proposta/trial ou login inicial.

### 9.2. Usuário com acesso expirado

1. Existe usuário autenticado.
2. Status comercial está expirado.
3. App direciona para paywall ou tela de bloqueio.

### 9.3. Falha ao determinar status

1. App não consegue confirmar status.
2. Exibe estado de erro ou tentativa novamente.
3. Não libera rota protegida até confirmação.

---

## 10. Estados de tela ou estados esperados

- carregando rota inicial;
- rota pública;
- rota protegida;
- rota bloqueada;
- erro de permissão;
- erro de conexão;
- erro inesperado.

---

## 11. Impacto no Frontend Flutter

- Configurar go_router ou solução equivalente.
- Criar app shell.
- Criar guards de rota.
- Criar rotas nomeadas para fluxos P0.
- Tratar estado de loading antes de redirecionar.
- Evitar navegação duplicada ou loops.
- Preparar estrutura para tabs futuras.

---

## 12. Impacto no Backend

Não há endpoint exclusivo desta US.

A navegação dependerá futuramente de endpoints de autenticação, status de acesso e perfil, definidos em outros épicos.

---

## 13. Impacto no Banco de Dados

Não há impacto direto em banco de dados nesta US.

---

## 14. Impacto em Gamificação

- Não altera XP, rank, level, atributos ou streak.
- Garante que o usuário chegue às telas de quest e perfil quando estiver apto.

---

## 15. Impacto em Monetização

- Deve respeitar trial ativo, assinatura ativa e acesso expirado.
- Usuário bloqueado deve ser direcionado ao paywall.
- Visitante deve entender o trial antes de investir tempo no onboarding.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Nomes, labels e mensagens de erro devem ser localizados. |
| EN | Preparar chaves equivalentes. |
| ES | Preparar chaves equivalentes. |

---

## 17. Contrato de API sugerido

Não aplicável diretamente.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| app_opened | Quando o app abre. |
| access_blocked | Quando o app direciona usuário expirado para tela bloqueada. |

---

## 19. Critérios de aceite

### CA-001 — Visitante direcionado corretamente

Dado que não existe sessão ativa,

Quando o app abrir,

Então o usuário deve ser direcionado para uma rota pública inicial.

### CA-002 — Usuário com acesso ativo direcionado corretamente

Dado que existe sessão e acesso ativo,

Quando o app abrir,

Então o usuário deve ser direcionado para a rota protegida adequada.

### CA-003 — Usuário expirado bloqueado

Dado que o trial ou assinatura expirou,

Quando o usuário tentar acessar rota protegida,

Então deve ser direcionado para paywall ou estado bloqueado.

### CA-004 — Sem loop de navegação

Dado qualquer estado válido de usuário,

Quando o app resolver a rota inicial,

Então não deve ocorrer loop infinito de redirecionamento.

---

## 20. Critérios de teste para QA

- abrir app como visitante;
- abrir app com trial ativo;
- abrir app com assinatura ativa;
- abrir app com trial expirado;
- abrir app com assinatura expirada;
- testar rota protegida sem sessão;
- testar perda de conexão durante resolução de rota;
- testar mensagens localizadas.

---

## ✅ Decisão registrada

> A estrutura de navegação deve ser criada antes dos fluxos de negócio para garantir que cada perfil seja direcionado corretamente e que o modelo de trial/assinatura seja respeitado desde o início.
