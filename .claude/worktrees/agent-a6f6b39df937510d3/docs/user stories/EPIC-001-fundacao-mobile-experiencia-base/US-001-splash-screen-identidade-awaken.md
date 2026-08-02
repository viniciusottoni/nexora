---
title: US-001 — Visualizar splash screen com identidade AWAKEN
sidebar_position: 1
---

# US-001 — Visualizar splash screen com identidade AWAKEN

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-001 |
| Épico | EPIC-001 — Fundação Mobile e Experiência Base |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Visitante |
| Plano | Visitante, Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Identidade visual inicial do AWAKEN |
| Status | Planejada |

---

## 2. História do usuário

Como **visitante**,

quero **visualizar uma splash screen com a identidade AWAKEN**,

para **entender que estou entrando em uma experiência épica, imersiva e diferente de um app fitness tradicional**.

---

## 3. Contexto

A splash screen é o primeiro contato do usuário com o AWAKEN. Ela deve comunicar rapidamente o tom do produto: dark, épico, energético, moderno e inspirado em progressão de anime/game, sem parecer genérico ou pesado demais.

O objetivo não é criar uma abertura longa, mas sim uma entrada curta, memorável e performática que prepare o usuário para a experiência de trial, onboarding, quest e evolução.

---

## 4. Objetivo

Exibir uma splash screen inicial com marca, fundo dark e transição rápida para a próxima tela do app, respeitando performance, clareza e estabilidade.

---

## 5. Escopo

### Entra nesta US

- Exibir logo ou símbolo AWAKEN na abertura do app.
- Aplicar fundo dark coerente com a identidade visual.
- Aplicar animação leve, curta e performática, se tecnicamente viável no MVP.
- Encaminhar o usuário para a próxima rota após carregamento inicial.
- Garantir fallback visual caso a animação não carregue.
- Preparar textos ou assets para localização quando houver texto visível.

### Fora desta US

- Tela comercial de trial.
- Tela de login.
- Onboarding.
- Animação cinematográfica longa.
- Vídeo de abertura.
- Carregamento de dados de usuário autenticado em profundidade.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | A splash deve aparecer ao abrir o app. |
| RN-002 | A splash não deve bloquear o usuário além do tempo necessário para inicialização. |
| RN-003 | Caso o usuário seja visitante, após a splash deve seguir para a experiência inicial do app. |
| RN-004 | Caso o usuário já tenha sessão válida, após a splash deve seguir para a rota adequada conforme status de acesso. |
| RN-005 | Caso ocorra falha no carregamento visual, deve ser exibido fallback simples sem crash. |
| RN-006 | A splash não deve exibir paywall, preço ou mensagem comercial. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar. |
| Usuário em Trial | Pode visualizar. |
| Premium Mensal | Pode visualizar. |
| Premium Anual | Pode visualizar. |
| Trial expirado | Pode visualizar. |
| Assinatura expirada | Pode visualizar. |
| Admin interno | Não aplicável no app mobile do MVP. |
| Suporte interno | Não aplicável no app mobile do MVP. |

---

## 8. Fluxo principal

1. Usuário abre o aplicativo AWAKEN.
2. O app inicializa recursos mínimos necessários.
3. A splash screen é exibida com a identidade AWAKEN.
4. O app verifica estado inicial necessário para roteamento.
5. O usuário é encaminhado para a próxima tela correta.

---

## 9. Fluxos alternativos

### 9.1. Falha ao carregar animação

1. O app tenta carregar animação ou asset visual.
2. O asset falha ou demora além do esperado.
3. O app exibe versão estática da splash.
4. O usuário segue normalmente para a próxima rota.

### 9.2. Usuário sem conexão

1. O usuário abre o app sem internet.
2. A splash é exibida normalmente.
3. O app encaminha para uma tela capaz de tratar ausência de conexão conforme o estado de sessão.

---

## 10. Estados de tela ou estados esperados

- inicializando;
- exibindo splash;
- fallback visual;
- redirecionando;
- erro inesperado controlado.

---

## 11. Impacto no Frontend Flutter

- Criar tela ou configuração nativa de splash.
- Aplicar assets da marca AWAKEN.
- Aplicar fundo dark.
- Configurar transição para próxima rota.
- Garantir compatibilidade com Android.
- Evitar animações pesadas que prejudiquem tempo de abertura.
- Preparar assets em diferentes densidades.

---

## 12. Impacto no Backend

Não há endpoint obrigatório para esta US.

Pode depender indiretamente de serviços futuros de configuração remota, mas isso está fora do MVP desta US.

---

## 13. Impacto no Banco de Dados

Não há impacto direto em banco de dados.

---

## 14. Impacto em Gamificação

- Reforça a identidade épica do produto.
- Prepara emocionalmente o usuário para a ideia de sistema, evolução e jornada.
- Não concede XP, rank, streak ou atributos.

---

## 15. Impacto em Monetização

- Não exibe preço nem paywall.
- Não inicia trial.
- Não altera assinatura.
- Serve como etapa anterior à tela de proposta/trial.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Se houver texto, deve estar localizado. |
| EN | Preparar chave equivalente. |
| ES | Preparar chave equivalente. |

---

## 17. Contrato de API sugerido

Não aplicável nesta US.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| splash_viewed | Quando a splash screen é exibida com sucesso. |
| app_opened | Quando o app é aberto. Pode ser tratado globalmente. |

---

## 19. Critérios de aceite

### CA-001 — Exibição da splash

Dado que o usuário abriu o app,

Quando a inicialização começar,

Então a splash screen do AWAKEN deve ser exibida.

### CA-002 — Redirecionamento após splash

Dado que a splash foi exibida,

Quando o app concluir a verificação inicial,

Então o usuário deve ser redirecionado para a próxima tela adequada.

### CA-003 — Fallback visual

Dado que o asset animado falhou,

Quando a splash for carregada,

Então o app deve exibir uma versão estática sem travar.

### CA-004 — Sem paywall na splash

Dado que a splash foi exibida,

Quando o usuário visualizar a tela,

Então nenhum preço, plano ou paywall deve aparecer nessa etapa.

---

## 20. Critérios de teste para QA

- abrir app em instalação limpa;
- abrir app com sessão existente;
- abrir app sem internet;
- validar splash em Android mínimo;
- validar que não ocorre crash;
- validar fallback de asset;
- validar evento `splash_viewed`;
- validar textos localizados, se houver texto.

---

## ✅ Decisão registrada

> A splash screen do AWAKEN deve ser curta, estável, dark e imersiva. Ela apresenta a identidade do produto, mas não deve atrasar o acesso, vender assinatura ou substituir a tela de proposta/trial.
