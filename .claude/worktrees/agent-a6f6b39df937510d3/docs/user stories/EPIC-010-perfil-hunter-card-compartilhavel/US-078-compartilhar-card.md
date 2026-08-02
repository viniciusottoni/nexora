---
title: US-078 — Compartilhar card
sidebar_position: 78
---

# US-078 — Compartilhar card

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-078 |
| Épico | EPIC-010 — Perfil do Hunter e Card Compartilhável |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Usuário em Trial ou assinante |
| Plano | Trial, Mensal e Anual |
| Idiomas impactados | PT-BR, EN, ES, FR |
| Dependência principal | Card gerado e compartilhamento nativo |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário com card gerado**,

quero **compartilhar meu card em apps externos**,

para **mostrar minha evolução e divulgar o AWAKEN organicamente**.

---

## 3. Contexto

O compartilhamento ajuda motivação e aquisição orgânica. O card deve ser compartilhado como imagem, usando recursos nativos do Android.

---

## 4. Objetivo

Permitir compartilhar a imagem do card em apps como WhatsApp, Instagram, Telegram e outros disponíveis no dispositivo.

---

## 5. Escopo

### Entra nesta US

- Ação de compartilhar card.
- Uso da imagem gerada na US-077.
- Integração com share nativo.
- Mensagem opcional de acompanhamento.
- Tratamento de cancelamento.

### Fora desta US

- Feed interno.
- Ranking social.
- Publicação automática.
- Agendamento de post.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Compartilhamento deve usar imagem gerada do card. |
| RN-002 | O usuário deve escolher o app externo no share nativo. |
| RN-003 | Cancelamento do compartilhamento não deve gerar erro crítico. |
| RN-004 | Card compartilhado não pode conter dados sensíveis. |
| RN-005 | Acesso expirado não deve compartilhar card completo. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não pode compartilhar. |
| Usuário em Trial | Pode compartilhar card funcional. |
| Premium Mensal | Pode compartilhar card completo. |
| Premium Anual | Pode compartilhar card completo. |
| Trial expirado | Não pode compartilhar card completo. |
| Assinatura expirada | Não pode compartilhar card completo. |

---

## 8. Fluxo principal

1. Usuário gera o card.
2. Toca em compartilhar.
3. App abre o share nativo do dispositivo.
4. Usuário escolhe app externo.
5. Card é enviado como imagem.

---

## 9. Fluxos alternativos

### 9.1. Usuário cancela

O app deve retornar para a tela anterior sem erro crítico.

### 9.2. Falha de compartilhamento

O app deve exibir mensagem amigável e permitir tentar novamente.

---

## 10. Estados esperados

- card pronto;
- abrindo compartilhamento;
- compartilhado;
- cancelado;
- erro de compartilhamento;
- acesso limitado.

---

## 11. Impacto no Frontend Flutter

- Botão de compartilhar.
- Integração com share nativo.
- Tratamento de cancelamento e erro.
- Mensagens localizadas.

---

## 12. Impacto no Backend

Não há endpoint obrigatório.

Pode registrar evento analítico via Firebase/Analytics.

---

## 13. Impacto no Banco de Dados

Sem impacto direto obrigatório.

---

## 14. Impacto em Gamificação

- Reforça recompensa visual.
- Estimula orgulho e consistência.

---

## 15. Impacto em Monetização

- Trial pode compartilhar card funcional.
- Visual premium pode incentivar assinatura.

---

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Texto do botão e mensagem opcional. |
| EN | Chaves equivalentes. |
| ES | Chaves equivalentes. |

---

## 17. Contrato de API sugerido

Não aplicável.

---

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| hunter_card_share_started | Quando usuário toca em compartilhar. |
| hunter_card_shared | Quando compartilhamento é concluído ou retornado como sucesso. |

---

## 19. Critérios de aceite

### CA-001 — Compartilhamento iniciado

Dado que o card foi gerado,

Quando o usuário tocar em compartilhar,

Então o share nativo deve abrir.

### CA-002 — Cancelamento controlado

Dado que o usuário cancela o compartilhamento,

Quando voltar ao app,

Então nenhuma mensagem de erro crítico deve aparecer.

---

## 20. Critérios de teste para QA

- compartilhar via WhatsApp;
- compartilhar via Instagram/Stories quando disponível;
- cancelar compartilhamento;
- falha de compartilhamento;
- validar imagem sem dados sensíveis;
- textos em PT-BR, EN e ES.

---

## ✅ Decisão registrada

> O card deve ser compartilhado como imagem via share nativo, dando ao usuário controle sobre onde publicar.
