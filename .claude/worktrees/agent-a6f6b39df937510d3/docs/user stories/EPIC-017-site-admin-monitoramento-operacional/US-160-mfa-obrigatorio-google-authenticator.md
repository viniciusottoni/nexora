---
title: US-160 — Configurar MFA obrigatório com Google Authenticator
sidebar_position: 160
---

# US-160 — Configurar MFA obrigatório com Google Authenticator

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-160 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Prioridade | P0 |
| Fase | MVP Android Fitness Gamificado |
| Perfil principal | Admin e Segurança |
| Plataforma | Web Admin (React) + Backend .NET |
| Dependência | US-159 |
| Status | Planejada |

## 2. História do usuário

Como **admin do AWAKEN**, quero **usar MFA por aplicativo autenticador**, para **reduzir o risco de invasão mesmo se minha senha for comprometida**.

## 3. Objetivo

Obrigar MFA via TOTP compatível com Google Authenticator para todo acesso ao site admin.

## 4. Escopo

### Entra nesta US

- Setup inicial de MFA para admin sem fator configurado.
- Geração de segredo TOTP e QR Code compatível com Google Authenticator.
- Validação de código TOTP no login.
- Bloqueio de acesso ao dashboard até MFA válido.
- Armazenamento protegido do segredo MFA.
- Auditoria de setup, validação, falha e reset administrativo.

### Fora desta US

- SMS, email OTP ou push MFA.
- Autenticação biométrica.
- Gestão avançada de dispositivos confiáveis.
- Recuperação self-service sem validação interna.

## 5. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | MFA é obrigatório para todo admin. |
| RN-002 | Admin sem MFA configurado deve concluir setup antes de acessar o painel. |
| RN-003 | Código TOTP deve ter janela curta de validade. |
| RN-004 | Falhas repetidas de MFA geram bloqueio temporário e auditoria. |
| RN-005 | Segredo MFA nunca deve ser exibido novamente após setup concluído. |
| RN-006 | Reset de MFA só pode ser feito por fluxo administrativo seguro. |

## 6. Fluxo principal

1. Admin informa senha válida.
2. Backend identifica que MFA é obrigatório.
3. Admin sem MFA configura QR Code e confirma primeiro código.
4. Admin com MFA configurado informa código TOTP.
5. Backend valida o código e libera a sessão administrativa.

## 7. Impacto Frontend React

- Tela de setup MFA com QR Code e campo de código.
- Tela de desafio MFA após login.
- Estados de erro, bloqueio e expiração de sessão parcial.

## 8. Impacto Backend

- Geração e validação TOTP.
- Proteção criptográfica do segredo MFA.
- Estado de autenticação parcial antes do MFA.
- Rate limit específico para validação de código.

## 9. Impacto DB

- Campos para segredo MFA protegido, status de setup e data de ativação.
- Registro de bloqueios e resets.

## 10. Critérios de aceite

### CA-001 — Setup obrigatório

Dado que o admin não tem MFA configurado,
quando fizer login com senha correta,
então deve ser direcionado ao setup e não ao dashboard.

### CA-002 — Código válido libera acesso

Dado que o admin informa código TOTP válido,
quando confirmar o MFA,
então a sessão administrativa deve ser liberada.

### CA-003 — Falhas auditadas

Dado que o admin erra códigos repetidamente,
quando exceder o limite,
então deve haver bloqueio temporário e registro de auditoria.

## 11. Decisão registrada

> O site admin exige MFA por TOTP compatível com Google Authenticator; senha sozinha nunca é suficiente para acessar o painel.
