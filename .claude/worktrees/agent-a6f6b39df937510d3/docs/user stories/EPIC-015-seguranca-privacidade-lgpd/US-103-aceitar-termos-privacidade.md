---
title: US-103 — Aceitar termos de uso e política de privacidade
sidebar_position: 103
---

# US-103 — Aceitar termos de uso e política de privacidade

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-103 |
| Épico | EPIC-015 — Segurança, Privacidade e LGPD |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Todos os usuários |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejada |

---

## 2. História do usuário

Como **usuário do AWAKEN**,

quero **aceitar os termos de uso e a política de privacidade antes de usar as funcionalidades principais**,

para **entender como meus dados serão tratados e quais responsabilidades existem no uso do app**.

---

## 3. Contexto

O AWAKEN coleta dados pessoais, físicos e informações de treino para personalizar a experiência. Antes do uso completo, o usuário precisa ter acesso aos termos e à política de privacidade e registrar aceite explícito.

---

## 4. Objetivo

Garantir aceite explícito dos termos de uso e política de privacidade antes do acesso às funcionalidades principais do app.

---

## 5. Escopo

### Entra nesta US

- Exibir termos de uso.
- Exibir política de privacidade.
- Exigir aceite explícito.
- Registrar data/hora do aceite.
- Registrar versão dos documentos aceitos.
- Bloquear funcionalidades principais enquanto não houver aceite.

### Fora desta US

- Redação jurídica final dos documentos.
- Painel jurídico interno.
- Gestão avançada de versões legais.

---

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Usuário deve aceitar termos antes de usar funcionalidades principais. |
| RN-002 | Aceite deve ser explícito, não presumido. |
| RN-003 | Sistema deve registrar `termsAcceptedAt` e `privacyAcceptedAt`. |
| RN-004 | Sistema deve registrar a versão dos documentos aceitos. |
| RN-005 | Se os documentos forem atualizados de forma relevante, o app deve poder solicitar novo aceite. |
| RN-006 | Usuário que não aceita não deve prosseguir para onboarding, geração de quest ou assinatura. |

---

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode visualizar termos e privacidade. |
| Usuário sem aceite | Não acessa funcionalidades principais. |
| Usuário em Trial | Deve ter aceite registrado. |
| Premium Mensal | Deve ter aceite registrado. |
| Premium Anual | Deve ter aceite registrado. |

---

## 8. Fluxo principal

1. Usuário cria conta ou acessa fluxo inicial.
2. App exibe links para termos de uso e política de privacidade.
3. Usuário marca aceite explícito.
4. App envia aceite ao backend.
5. Backend registra data/hora e versão dos documentos.
6. Usuário pode prosseguir para o próximo fluxo permitido.

---

## 9. Fluxos alternativos

### 9.1. Usuário não aceita

App mantém acesso bloqueado às funcionalidades principais e permite voltar/cancelar.

### 9.2. Erro ao registrar aceite

App exibe erro e não libera o fluxo até registro bem-sucedido.

---

## 10. Estados esperados

- aguardando aceite;
- documentos abertos;
- aceite enviado;
- aceite registrado;
- aceite recusado;
- erro de registro.

---

## 11. Impacto Flutter

- Tela ou modal de aceite.
- Checkbox ou ação explícita de concordância.
- Links para termos e política.
- Bloqueio de navegação sem aceite.
- Textos localizados PT-BR, EN e ES.

---

## 12. Impacto Backend

- Endpoint para registrar aceite.
- Persistência de timestamps e versão.
- Guard para funcionalidades principais sem aceite.
- Logs básicos da ação de aceite.

---

## 13. Impacto DB

Campos sugeridos:

- termsAcceptedAt;
- privacyAcceptedAt;
- termsVersion;
- privacyVersion;
- updatedAt.

---

## 14. Impacto Gamificação

- Sem impacto direto em XP.
- Bloqueia acesso à jornada gamificada enquanto não houver aceite.

---

## 15. Impacto Monetização

- Usuário não deve iniciar trial/assinatura sem aceite obrigatório registrado.

---

## 16. Contrato API sugerido

```txt
POST /api/users/me/legal-acceptance
```

Request conceitual:

```json
{
  "termsVersion": "1.0.0",
  "privacyVersion": "1.0.0",
  "accepted": true
}
```

---

## 17. Eventos Analytics

| Evento | Quando dispara |
|---|---|
| legal_terms_viewed | Quando usuário visualiza termos. |
| privacy_policy_viewed | Quando usuário visualiza política. |
| legal_acceptance_completed | Quando aceite é registrado. |

Eventos não devem conter dados pessoais sensíveis.

---

## 18. Critérios de aceite

### CA-001 — Aceite obrigatório

Dado que o usuário ainda não aceitou termos e privacidade,
Quando tentar acessar funcionalidades principais,
Então o app deve bloquear e solicitar aceite.

### CA-002 — Aceite registrado

Dado que o usuário aceita os documentos,
Quando o backend registrar a ação,
Então deve salvar data/hora e versão dos documentos aceitos.

---

## 19. Critérios de teste QA

- visualizar termos;
- visualizar política;
- aceitar com sucesso;
- tentar prosseguir sem aceitar;
- erro ao registrar aceite;
- novo aceite após versão alterada;
- textos PT-BR, EN e ES.

---

## 20. Decisão registrada

> O aceite de termos e política de privacidade é obrigatório antes do uso das funcionalidades principais do AWAKEN.
