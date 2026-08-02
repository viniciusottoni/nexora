---
title: US-198 — Fortalecer Google Sign-In com audience obrigatória e e-mail verificado
sidebar_position: 198
---

# US-198 — Fortalecer Google Sign-In com audience obrigatória e e-mail verificado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-198 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Usuário autenticado via Google |
| Plano | Trial, Mensal, Anual |
| Idiomas impactados | PT-BR / EN / ES / FR |
| Dependência principal | Google Sign-In, User, AuthController, GoogleTokenValidator |
| Status | Planejada |

## 2. História do usuário

Como **usuário que entra com Google**,

quero **que somente tokens emitidos para o app AWAKEN sejam aceitos**,

para **proteger minha conta contra login indevido com credenciais de outro app**.

## 3. Contexto

O validador de Google permite validação sem audience quando o client id não está configurado. Além disso, o handler usa o payload retornado sem bloquear explicitamente e-mails não verificados. Em produção, o backend deve exigir audience esperada e e-mail verificado.

## 4. Objetivo

Garantir que Google Sign-In aceite apenas tokens válidos, destinados ao AWAKEN e com e-mail verificado.

## 5. Escopo

### Entra nesta US

- Tornar client IDs Google obrigatórios fora de Development.
- Validar audience contra lista permitida Android/iOS/Web, conforme configuração.
- Rejeitar payload com e-mail não verificado.
- Rejeitar provider diferente de Google no endpoint Google.
- Criar logs seguros e auditoria para falhas relevantes.
- Criar testes positivos e negativos.

### Fora desta US

- Apple Sign-In.
- MFA.
- Migração de contas existentes.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Token Google sem audience esperada deve ser rejeitado. |
| RN-002 | Token com e-mail não verificado deve ser rejeitado. |
| RN-003 | Produção não pode iniciar com login Google habilitado sem client id permitido. |
| RN-004 | Conta local pode ser vinculada ao Google apenas se o e-mail for verificado. |
| RN-005 | Falhas devem retornar erro genérico ao usuário, sem detalhes internos do token. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Pode tentar login Google válido. |
| Usuário existente | Pode vincular Google se e-mail verificado corresponder. |
| Trial/Premium | Pode renovar sessão via Google. |
| Admin interno | Não recebe exceção especial nesse fluxo. |

## 8. Fluxo principal

1. App recebe id token do Google.
2. App envia token ao backend.
3. Backend valida assinatura, emissor, audience e e-mail verificado.
4. Backend encontra ou cria usuário.
5. Backend emite sessão AWAKEN.

## 9. Fluxos alternativos

- Audience inválida: retorna 401.
- E-mail não verificado: retorna 401.
- Client id ausente em produção: API falha no startup pela US-197.
- Conta local com mesmo e-mail: vincula apenas se e-mail Google for verificado.

## 10. Estados esperados

- login Google válido;
- token inválido;
- audience inválida;
- e-mail não verificado;
- provider inválido;
- erro de rede;
- erro inesperado com `correlationId`.

## 11. Impacto no Frontend Flutter

- Exibir erro genérico de login Google.
- Não tentar contornar falha com provider alternativo sem ação do usuário.
- Localizar mensagem de falha.

## 12. Impacto no Backend

- Ajustar `GoogleTokenValidator`.
- Ajustar `GoogleSignInCommandHandler`.
- Criar configuração de audiences permitidas.
- Integrar validação de startup da US-197.

## 13. Impacto no Banco de Dados

Sem migration obrigatória. Pode haver auditoria de falha de login se já existir infraestrutura.

## 14. Impacto em Gamificação

Sem impacto direto.

## 15. Impacto em Monetização

Protege acesso pago vinculado à conta do usuário.

## 16. Impacto em Internacionalização

| Idioma | Impacto |
|---|---|
| PT-BR | Mensagem de falha no login Google. |
| EN | Mesma mensagem localizada. |
| ES | Mesma mensagem localizada. |
| FR | Mesma mensagem localizada. |

## 17. Contrato de API sugerido

```txt
POST /api/auth/google
```

Erro esperado:

```json
{
  "code": "GOOGLE_AUTH_FAILED",
  "message": "Não foi possível entrar com Google.",
  "correlationId": "uuid"
}
```

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| google_signin_started | Usuário inicia login. |
| google_signin_success | Login concluído. |
| google_signin_failed | Login falha. |

## 19. Critérios de aceite

- Token com audience correta e e-mail verificado autentica.
- Token com audience incorreta retorna 401.
- Token com e-mail não verificado retorna 401.
- Produção não inicia sem client id configurado.
- Mensagem ao usuário não expõe detalhes técnicos do token.

## 20. Critérios de teste para QA

- login Google válido;
- audience inválida;
- e-mail não verificado;
- provider inválido;
- conta local vinculada com e-mail verificado;
- mensagens PT-BR/EN/ES/FR.

## ✅ Decisão registrada

Google Sign-In só será aceito quando o token for destinado ao AWAKEN e o e-mail estiver verificado.