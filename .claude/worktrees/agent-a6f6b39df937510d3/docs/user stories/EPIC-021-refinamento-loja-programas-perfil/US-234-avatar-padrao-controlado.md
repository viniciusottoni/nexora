---
title: US-234 — Usar avatar padrão e permitir apenas avatares disponíveis
sidebar_position: 234
---

# US-234 — Usar avatar padrão e permitir apenas avatares disponíveis

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-234 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P1 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | EPIC-002, EPIC-010, EPIC-019 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **ter um avatar padrão quando não houver imagem do Google e poder escolher apenas avatares disponíveis no sistema**, para **manter o Perfil Hunter bonito, seguro e consistente com a identidade do app**.

## 3. Contexto

O perfil pode receber imagem do Google no login. Quando não houver imagem, o app deve usar um avatar padrão. A edição manual não deve permitir upload externo no MVP; apenas seleção entre avatares internos disponíveis.

## 4. Objetivo

Definir fallback de avatar e fluxo de edição controlada por catálogo interno.

## 5. Escopo

### Entra nesta US

- Usar imagem do Google quando existir e for válida.
- Usar avatar padrão quando não existir imagem do Google.
- Permitir editar avatar escolhendo catálogo interno.
- Bloquear upload de imagem externa.
- Suportar avatares liberados por packs ou regras futuras.
- Refletir avatar no Perfil Hunter e card compartilhável.

### Fora desta US

- Upload livre de imagem.
- Recorte/crop de foto.
- Avatar 3D.
- Geração de avatar por IA.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Se Google retornar imagem válida, ela pode ser usada como avatar inicial. |
| RN-002 | Se não houver imagem Google, usar avatar padrão do sistema. |
| RN-003 | Usuário só pode escolher avatares internos disponíveis para ele. |
| RN-004 | Upload externo fica bloqueado no MVP. |
| RN-005 | Avatares de pack só aparecem se o usuário possuir/liberar o pack. |
| RN-006 | Avatar selecionado deve aparecer no Perfil Hunter e card compartilhável. |

## 7. Fluxo principal

1. Usuário cria conta ou acessa perfil.
2. Sistema verifica imagem Google válida.
3. Se existir, usa como avatar inicial.
4. Se não existir, usa avatar padrão.
5. Usuário abre edição de avatar.
6. App mostra catálogo interno disponível.
7. Usuário seleciona avatar.
8. Backend salva seleção.

## 8. Impacto Backend

- Catálogo de avatares internos.
- Validação de posse/disponibilidade.
- Campo `selectedAvatarKey` no perfil.
- Bloqueio de URL externa enviada pelo cliente.

## 9. Impacto Flutter

- Fallback visual para avatar padrão.
- Tela/modal de seleção de avatar.
- Grid de avatares disponíveis.
- Estado bloqueado para avatar não liberado.
- Remover opção de upload externo.

## 10. Contrato API sugerido

```txt
GET /api/users/me/avatars
PUT /api/users/me/avatar
```

Request conceitual:

```json
{
  "avatarKey": "striker_default"
}
```

## 11. Critérios de aceite

### CA-001 — Avatar padrão

Dado que o usuário não possui imagem Google,
quando abrir o Perfil Hunter,
então deve ver o avatar padrão do sistema.

### CA-002 — Seleção controlada

Dado que o usuário escolhe avatar disponível,
quando salvar,
então o backend deve aceitar e o perfil deve atualizar.

### CA-003 — Upload bloqueado

Dado que o usuário tenta enviar URL ou arquivo externo,
quando salvar avatar,
então o backend deve rejeitar.

## 12. Decisão registrada

Avatar do AWAKEN é controlado: Google pode fornecer imagem inicial, mas edição manual usa apenas catálogo interno disponível.
