---
title: US-004 — Usar app em PT-BR com estrutura para EN e ES
sidebar_position: 4
---

# US-004 — Usar app em PT-BR com estrutura para EN e ES

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-004 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários do app mobile |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Estrutura de internacionalização Flutter |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário brasileiro**,

quero **usar o AWAKEN em português do Brasil**,

para **entender todas as telas, mensagens e ações sem barreira de idioma**.

---

## 3. Contexto

Uma das vantagens competitivas do AWAKEN é nascer com PT-BR nativo. Mesmo assim, a base técnica deve estar preparada para inglês e espanhol, evitando textos fixos espalhados pelo app.

---

## 4. Objetivo

Configurar a base de internacionalização do app para PT-BR como idioma padrão inicial, com estrutura pronta para EN e ES.

---

## 5. Escopo

### Entra nesta US

- Configurar internacionalização no Flutter.
- Criar arquivos base de idioma.
- Definir PT-BR como idioma padrão.
- Preparar EN e ES.
- Localizar textos das telas P0 do EPIC-001.
- Definir fallback de idioma.

### Fora desta US

- Tradução completa de todas as User Stories futuras.
- Conteúdo de treino gerado por IA.
- Nomes completos de exercícios.
- Notificações push futuras.
- E-mails transacionais.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | PT-BR deve ser o idioma padrão do MVP. |
| RN-002 | Nenhuma tela P0 deve depender de texto hardcoded. |
| RN-003 | Quando idioma do dispositivo não for suportado, o app deve usar PT-BR. |
| RN-004 | As chaves de tradução devem ser nomeadas de forma clara. |
| RN-005 | Mensagens de erro e empty states também devem ser localizados. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Usa idioma suportado. |
| Usuário em Trial | Usa idioma suportado. |
| Premium Mensal | Usa idioma suportado. |
| Premium Anual | Usa idioma suportado. |
| Trial expirado | Usa idioma suportado. |
| Assinatura expirada | Usa idioma suportado. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário abre o app.
2. O app identifica idioma do dispositivo ou preferência salva.
3. Se o idioma for suportado, aplica o idioma correspondente.
4. Se não for suportado, aplica PT-BR.
5. Textos da interface aparecem no idioma definido.

---

## 9. Fluxos alternativos

### 9.1. Idioma não suportado

Se o idioma do dispositivo não for PT-BR, EN ou ES, o app deve usar PT-BR.

### 9.2. Chave ausente

Se uma chave estiver ausente, o app não deve quebrar. Deve usar fallback controlado e o problema deve ser identificável em QA.

---

## 10. Estados de tela ou estados esperados

- idioma carregado;
- fallback PT-BR;
- chave ausente controlada;
- texto longo;
- erro localizado.

---

## 11. Impacto no Frontend Flutter

- Configurar ARB ou solução equivalente.
- Criar arquivos `pt_BR`, `en` e `es`.
- Usar chaves de tradução em telas P0.
- Testar textos longos em componentes.
- Preparar fallback.

---

## 12. Impacto no Backend

Não há endpoint obrigatório nesta US.

Futuramente, backend pode receber idioma preferido para conteúdo dinâmico.

---

## 13. Impacto no Banco de Dados

Sem impacto obrigatório no MVP.

Pode haver campo futuro de preferência de idioma no perfil do usuário.

---

## 14. Impacto em Gamificação

- Mensagens futuras de XP, rank, level e streak devem usar a estrutura de idioma.
- Não altera cálculo de gamificação.

---

## 15. Impacto em Monetização

- Textos futuros de trial, paywall e planos devem ser localizados.
- Não altera regras comerciais.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Idioma padrão e completo para MVP. |
| EN | Estrutura criada e chaves preparadas. |
| ES | Estrutura criada e chaves preparadas. |

---

## 17. Contrato de API sugerido

Não aplicável nesta US.

---

## 18. Eventos de Analytics

Não há evento obrigatório específico.

---

## 19. Critérios de aceite

### CA-001 — PT-BR padrão

Dado que o usuário abre o app sem preferência salva,

Quando o idioma do dispositivo não exigir outro idioma suportado,

Então o app deve exibir PT-BR.

### CA-002 — Fallback

Dado que o idioma do dispositivo não é suportado,

Quando o app iniciar,

Então deve usar PT-BR.

### CA-003 — Sem texto hardcoded

Dado que uma tela P0 é implementada,

Quando seus textos forem revisados,

Então devem usar chaves de internacionalização.

### CA-004 — EN e ES preparados

Dado que a estrutura de idioma existe,

Quando o projeto for revisado,

Então arquivos base de EN e ES devem estar presentes.

---

## 20. Critérios de teste para QA

- validar PT-BR como padrão;
- validar EN;
- validar ES;
- validar fallback para idioma não suportado;
- validar textos longos;
- validar mensagens de erro localizadas;
- revisar ausência de textos hardcoded em telas P0.

---

## ✅ Decisão registrada

> O AWAKEN deve nascer em PT-BR, mas com base técnica pronta para EN e ES desde o MVP.
