---
title: US-003 — Usar interface dark, legível e imersiva
sidebar_position: 3
---

# US-003 — Usar interface dark, legível e imersiva

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-003 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários do app mobile |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Design tokens e tema Flutter |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **uma interface dark, legível e imersiva**,

para **sentir a proposta anime/gamificada sem perder clareza e facilidade de uso**.

---

## 3. Contexto

O AWAKEN precisa ter identidade visual própria, com estética dark, épica e moderna. A experiência deve ser imersiva, mas não pode dificultar leitura, navegação ou compreensão das ações principais.

---

## 4. Objetivo

Definir e aplicar o tema visual base do MVP, garantindo consistência entre telas, componentes e estados visuais.

---

## 5. Escopo

### Entra nesta US

- Tema dark global.
- Paleta inicial de cores.
- Tipografia base.
- Tokens de espaçamento, borda e radius.
- Componentes base reutilizáveis.
- Contraste adequado para leitura.
- Responsividade mínima para Android.

### Fora desta US

- Design system final completo.
- Animações avançadas.
- Customização de tema pelo usuário.
- Componentes específicos de quest, assinatura ou perfil.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O tema padrão do app deve ser dark. |
| RN-002 | Textos essenciais devem ter contraste suficiente. |
| RN-003 | Componentes P0 devem usar tokens centralizados. |
| RN-004 | O visual deve evitar aparência genérica de app fitness tradicional. |
| RN-005 | O app deve continuar legível em telas pequenas. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Usa tema dark. |
| Usuário em Trial | Usa tema dark. |
| Premium Mensal | Usa tema dark. |
| Premium Anual | Usa tema dark. |
| Trial expirado | Usa tema dark em telas limitadas. |
| Assinatura expirada | Usa tema dark em telas limitadas. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário acessa qualquer tela do app.
2. O tema global é aplicado.
3. Componentes usam tokens visuais definidos.
4. Textos, botões e cards aparecem de forma legível.
5. Usuário interage sem perda de clareza visual.

---

## 9. Fluxos alternativos

### 9.1. Fonte aumentada

Se o usuário usar fonte maior no sistema, as telas P0 devem continuar utilizáveis.

### 9.2. Tela pequena

Se o usuário usar dispositivo Android mínimo, os componentes devem manter leitura e espaçamento aceitáveis.

---

## 10. Estados de tela ou estados esperados

- tema carregado;
- componente padrão;
- componente desabilitado;
- tela pequena;
- texto aumentado;
- erro visual controlado.

---

## 11. Impacto no Frontend Flutter

- Criar tema global.
- Definir tokens de cor, tipografia e espaçamento.
- Criar componentes base.
- Aplicar responsividade mínima.
- Garantir contraste adequado.
- Preparar textos localizados.

---

## 12. Impacto no Backend

Não há impacto direto no backend.

---

## 13. Impacto no Banco de Dados

Não há impacto direto em banco de dados.

---

## 14. Impacto em Gamificação

- Reforça a sensação de jornada e evolução.
- Prepara a base visual para XP, rank, perfil e recompensas.
- Não altera regras de progressão.

---

## 15. Impacto em Monetização

- O tema deve ser aplicado também em trial, paywall e assinatura.
- Não altera trial, planos ou cobrança.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Textos devem caber nos componentes. |
| EN | Componentes devem suportar labels em inglês. |
| ES | Componentes devem suportar labels em espanhol. |

---

## 17. Contrato de API sugerido

Não aplicável nesta US.

---

## 18. Eventos de Analytics

Não há evento obrigatório específico para o tema.

---

## 19. Critérios de aceite

### CA-001 — Tema dark aplicado

Dado que o usuário acessa o app,

Quando qualquer tela base for exibida,

Então o tema dark deve estar aplicado.

### CA-002 — Legibilidade

Dado que a tela possui textos essenciais,

Quando o usuário visualizar a interface,

Então os textos devem estar legíveis.

### CA-003 — Tokens centralizados

Dado que componentes base são usados,

Quando o frontend renderizar a tela,

Então os componentes devem usar tokens de design centralizados.

### CA-004 — Responsividade mínima

Dado que o app roda em tela Android mínima,

Quando uma tela P0 for exibida,

Então os componentes não devem quebrar visualmente.

---

## 20. Critérios de teste para QA

- validar tema dark em telas públicas;
- validar tema dark em telas protegidas;
- validar contraste de textos;
- validar botões habilitados e desabilitados;
- validar fonte aumentada;
- validar tela pequena;
- validar labels em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> A interface do AWAKEN deve ser dark, imersiva e épica, mas sempre legível e funcional.
