---
title: US-201 — Exigir claim válida de usuário autenticado
sidebar_position: 201
---

# US-201 — Exigir claim válida de usuário autenticado

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-201 |
| Épico | EPIC-020 — Hardening de Segurança e Fechamento de Vulnerabilidades |
| Prioridade | P0 |
| Fase | Bloqueador pré-produção / pré-teste aberto |
| Perfil principal | Usuário autenticado, backend e segurança |
| Plano | Todos |
| Idiomas impactados | PT-BR / EN / ES / FR |
| Dependência principal | CurrentUserService, JWT, handlers autenticados |
| Status | Planejada |

## 2. História do usuário

Como **usuário autenticado do AWAKEN**,

quero **que o backend só execute ações quando minha identidade estiver validamente presente na sessão**,

para **evitar que operações sejam registradas em usuário vazio ou incorreto**.

## 3. Contexto

O serviço atual de usuário corrente retorna `Guid.Empty` quando não encontra uma claim válida. Esse fallback pode mascarar erro de token, teste ou configuração e permitir que handlers operem com usuário vazio.

## 4. Objetivo

Garantir que qualquer rota autenticada sem identificador de usuário válido seja rejeitada com erro de sessão inválida.

## 5. Escopo

### Entra nesta US

- Alterar `CurrentUserService.UserId` para não retornar `Guid.Empty` silenciosamente.
- Criar método seguro `TryGetUserId` se necessário.
- Ajustar handlers que assumem `Guid.Empty`.
- Criar testes para token sem identificador, identificador inválido e token válido.
- Padronizar erro de sessão inválida.

### Fora desta US

- Trocar formato de token.
- MFA.
- Permissões avançadas por resource owner.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Rota autenticada deve possuir identificador de usuário válido. |
| RN-002 | Identificador ausente ou inválido deve gerar 401/403. |
| RN-003 | Nenhuma operação pode ser persistida com usuário `Guid.Empty`. |
| RN-004 | Logs devem registrar correlationId, não dados sensíveis do token. |
| RN-005 | Testes devem cobrir endpoints críticos de economia, assinatura, quest e perfil. |

## 7. Permissões e planos

| Perfil / Plano | Permissão |
|---|---|
| Visitante | Não acessa rotas autenticadas. |
| Usuário com sessão válida | Pode acessar conforme plano. |
| Usuário com token sem identificador válido | Deve receber 401/403. |
| Admin | Também precisa de identificador válido e role adequada. |

## 8. Fluxo principal

1. Requisição chega com token válido.
2. Backend extrai identificador do usuário.
3. Identificador é validado como GUID real.
4. Handler executa normalmente.

## 9. Fluxos alternativos

- Claim ausente: erro de sessão inválida.
- Claim inválida: erro de sessão inválida.
- Usuário inexistente: erro de sessão inválida.

## 10. Estados esperados

- usuário válido;
- identificador ausente;
- identificador inválido;
- usuário não encontrado;
- acesso negado;
- erro inesperado com correlationId.

## 11. Impacto no Frontend Flutter

- Tratar 401/403 como sessão expirada/inválida.
- Limpar sessão local quando backend indicar sessão inválida.

## 12. Impacto no Backend

- Alterar `CurrentUserService`.
- Ajustar handlers que chamam `currentUserService.UserId`.
- Adicionar testes de integração em endpoints autenticados.

## 13. Impacto no Banco de Dados

- Não deve haver registros novos com `Guid.Empty`.
- Avaliar migração/limpeza se existirem dados legados com usuário vazio.

## 14. Impacto em Gamificação

Impede XP, quest, inventário ou progresso atribuídos a usuário inválido.

## 15. Impacto em Monetização

Impede compra, assinatura ou carteira associada a usuário inválido.

## 16. Impacto em Internacionalização

Mensagens de sessão inválida devem existir em PT-BR, EN, ES e FR.

## 17. Contrato de API sugerido

Erro esperado:

```json
{
  "code": "SESSION_INVALID",
  "message": "Sua sessão expirou. Entre novamente.",
  "correlationId": "uuid"
}
```

## 18. Eventos de Analytics

| Evento | Quando dispara |
|---|---|
| session_invalid | Backend rejeita sessão sem usuário válido. |

## 19. Critérios de aceite

- Token sem identificador retorna 401/403.
- Token com identificador inválido retorna 401/403.
- Token válido continua funcionando.
- Nenhum handler crítico opera com `Guid.Empty`.
- App limpa sessão ao receber erro de sessão inválida.

## 20. Critérios de teste para QA

- endpoint de perfil com sessão válida;
- endpoint de economia com sessão inválida;
- endpoint de assinatura com claim ausente;
- endpoint admin sem identificador;
- regressão de login normal.

## ✅ Decisão registrada

`Guid.Empty` não representa usuário autenticado válido. Requisições autenticadas sem identificador real devem ser rejeitadas.