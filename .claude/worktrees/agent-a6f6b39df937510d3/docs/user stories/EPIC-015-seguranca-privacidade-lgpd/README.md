---
title: EPIC-015 — Segurança, Privacidade e LGPD
sidebar_position: 15
---

# EPIC-015 — Segurança, Privacidade e LGPD

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | EPIC-015 |
| Fase | MVP Android Fitness Gamificado |
| Prioridade | P0 |
| Perfil principal | Todos os usuários |
| Planos impactados | Trial, Mensal e Anual |
| Status | Planejado |

## 2. Objetivo

Garantir que o AWAKEN trate dados pessoais, dados físicos, limitações informadas e informações de assinatura com clareza, segurança, rastreabilidade e respeito à LGPD.

## 3. Escopo

### Entra neste épico

- Termos de uso.
- Política de privacidade.
- Aviso de responsabilidade profissional.
- Proteção de sessão.
- Validação de dados no backend.
- Exclusão de conta como P1.
- Auditoria de ações sensíveis como P1.

### Fora deste épico

- Compliance médico avançado.
- Prontuário de saúde.
- Certificações externas.
- Painel jurídico interno.

## 4. User Stories relacionadas

| ID | Título | Prioridade | Documento |
|---|---|---|---|
| US-103 | Aceitar termos de uso e política de privacidade | P0 | [Abrir](./US-103-aceitar-termos-privacidade.md) |
| US-104 | Entender que o app não substitui orientação médica | P0 | [Abrir](./US-104-aviso-nao-substitui-orientacao-medica.md) |
| US-105 | Proteger sessão do usuário | P0 | [Abrir](./US-105-proteger-sessao-usuario.md) |
| US-106 | Validar dados sensíveis no backend | P0 | [Abrir](./US-106-validar-dados-sensiveis-backend.md) |
| US-107 | Solicitar exclusão de conta | P1 | [Abrir](./US-107-solicitar-exclusao-conta.md) |
| US-108 | Auditar ações sensíveis | P1 | [Abrir](./US-108-auditar-acoes-sensiveis.md) |

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-EPIC-015-001 | Usuário deve aceitar termos antes de usar funcionalidades principais. |
| RN-EPIC-015-002 | O app deve informar que não substitui médico, nutricionista, educador físico ou fisioterapeuta. |
| RN-EPIC-015-003 | Dados físicos e limitações não devem aparecer em card compartilhável. |
| RN-EPIC-015-004 | Dados de entrada devem ser validados no backend. |
| RN-EPIC-015-005 | Exclusão de conta deve seguir regra de privacidade definida. |
| RN-EPIC-015-006 | Ações sensíveis devem ser rastreáveis quando aplicável. |

## 6. Impactos técnicos

### Flutter

- Tela de termos e privacidade.
- Checkbox ou aceite explícito.
- Mensagens de aviso sobre uso responsável.
- Proteção de sessão.
- Tela ou fluxo de exclusão de conta como P1.

### Backend

- Registro de aceite.
- Validação de dados físicos e limitações.
- Regras de exclusão ou anonimização.
- Auditoria de ações sensíveis.

### Banco de dados

- termsAcceptedAt.
- privacyAcceptedAt.
- accountDeletedAt.
- auditLog.

### QA

- Validar aceite obrigatório.
- Validar aviso de responsabilidade.
- Validar exclusão de conta quando implementada.
- Validar que card não expõe dados físicos sensíveis.
- Validar mensagens em PT-BR, EN e ES.

## 7. Dependências

- EPIC-002 para conta.
- EPIC-003 para assinatura.
- EPIC-004 para dados físicos e limitações.
- EPIC-010 para card compartilhável.

## 8. Critérios de aceite do épico

- Usuário aceita termos e privacidade.
- App mostra aviso de responsabilidade.
- Dados sensíveis são validados.
- Card não expõe dados sensíveis.
- Exclusão de conta está prevista como P1.

## 9. Decisão registrada

Segurança e privacidade são obrigatórias no MVP porque o AWAKEN coleta dados físicos e limitações pessoais para personalizar treinos.
