---
title: US-005 — Ver estados de carregamento, erro, vazio e sucesso
sidebar_position: 5
---

# US-005 — Ver estados de carregamento, erro, vazio e sucesso

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-005 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários do app mobile |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Componentes base de UI |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **ver estados claros de carregamento, erro, vazio e sucesso**,

para **entender o que está acontecendo no app e saber o que posso fazer em seguida**.

---

## 3. Contexto

O MVP precisa evitar sensação de bug ou travamento. Sempre que uma tela estiver carregando, vazia, bloqueada, concluída ou com erro, o app deve comunicar isso de forma clara e visualmente consistente.

---

## 4. Objetivo

Criar componentes e padrões de estados reutilizáveis para todas as telas P0 do AWAKEN.

---

## 5. Escopo

### Entra nesta US

- Estado de carregamento.
- Estado vazio.
- Estado de erro.
- Estado de sucesso.
- Estado de bloqueio por acesso expirado.
- Mensagens localizadas.
- Ação primária quando aplicável.
- Padrão visual consistente com tema dark.

### Fora desta US

- Tratamento específico de todos os erros de negócio.
- Tela completa de paywall.
- Tela completa de assinatura.
- Sistema avançado de logs.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Toda tela P0 deve possuir estado de carregamento quando houver espera. |
| RN-002 | Toda tela P0 deve possuir estado de erro quando uma operação falhar. |
| RN-003 | Empty state deve explicar por que não há conteúdo. |
| RN-004 | Estados de erro devem oferecer ação de recuperação quando possível. |
| RN-005 | Estado bloqueado deve orientar usuário para assinatura quando acesso expirar. |
| RN-006 | Mensagens devem ser localizadas em PT-BR, EN e ES. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar estados públicos. |
| Usuário em Trial | Pode visualizar estados das telas protegidas. |
| Premium Mensal | Pode visualizar estados das telas protegidas. |
| Premium Anual | Pode visualizar estados das telas protegidas. |
| Trial expirado | Pode visualizar estado bloqueado. |
| Assinatura expirada | Pode visualizar estado bloqueado. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário acessa uma tela P0.
2. A tela inicia uma operação ou consulta.
3. O estado de carregamento é exibido.
4. A operação retorna sucesso, vazio, erro ou bloqueio.
5. A tela exibe o estado correspondente com mensagem clara.

---

## 9. Fluxos alternativos

### 9.1. Erro de conexão

Se houver falha de conexão, o app deve informar o problema e oferecer tentativa novamente quando possível.

### 9.2. Conteúdo vazio

Se não houver dados para exibir, o app deve mostrar empty state com orientação útil.

### 9.3. Acesso expirado

Se o trial ou assinatura estiver expirado, o app deve mostrar estado bloqueado com CTA para assinatura.

---

## 10. Estados de tela ou estados esperados

- carregando;
- vazio;
- sucesso;
- erro de validação;
- erro de conexão;
- erro de permissão;
- erro de assinatura;
- erro inesperado;
- bloqueado por acesso expirado.

---

## 11. Impacto no Frontend Flutter

- Criar componentes reutilizáveis de estado.
- Criar padrão de título, descrição, ícone e ação primária.
- Integrar estados ao tema dark.
- Preparar textos localizados.
- Garantir responsividade.

---

## 12. Impacto no Backend

Não há endpoint específico nesta US.

Os estados serão alimentados por respostas de APIs de outros épicos.

---

## 13. Impacto no Banco de Dados

Não há impacto direto em banco de dados.

---

## 14. Impacto em Gamificação

- Estados de sucesso podem reforçar sensação de conclusão.
- Estados de erro não devem punir o usuário visualmente.
- Não altera XP, rank, level ou streak.

---

## 15. Impacto em Monetização

- Estado bloqueado deve respeitar trial e assinatura.
- Mensagem de bloqueio deve ser transparente e sem dark pattern.
- Não implementa o paywall completo.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagens principais localizadas. |
| EN | Mensagens equivalentes preparadas. |
| ES | Mensagens equivalentes preparadas. |

---

## 17. Contrato de API sugerido

Não aplicável diretamente.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| access_blocked | Quando estado bloqueado for exibido por trial ou assinatura expirada. |

---

## 19. Critérios de aceite

### CA-001 — Loading

Dado que uma tela P0 está buscando dados,

Quando a operação ainda não terminou,

Então deve exibir estado de carregamento.

### CA-002 — Empty state

Dado que não há dados para exibir,

Quando a tela carregar,

Então deve exibir mensagem de estado vazio.

### CA-003 — Erro

Dado que uma operação falhou,

Quando a tela receber o erro,

Então deve exibir mensagem clara e ação de recuperação quando possível.

### CA-004 — Bloqueio comercial

Dado que o acesso expirou,

Quando usuário tentar acessar recurso protegido,

Então deve ver estado bloqueado com CTA de assinatura.

---

## 20. Critérios de teste para QA

- validar loading;
- validar empty state;
- validar erro de conexão;
- validar erro inesperado;
- validar estado bloqueado;
- validar CTA de tentativa novamente;
- validar textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O AWAKEN deve comunicar claramente qualquer estado de tela, evitando que o usuário confunda espera, erro, vazio ou bloqueio com bug.
