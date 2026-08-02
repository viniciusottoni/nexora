---
title: US-230 — Aplicar regras de uso e efeito dos itens
sidebar_position: 230
---

# US-230 — Aplicar regras de uso e efeito dos itens

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-230 |
| Épico | EPIC-021 — Refinamento de Loja, Programas e Perfil |
| Prioridade | P0 |
| Fase | Refinamento funcional pós-fundação de economia |
| Perfil principal | Usuário em Trial ou assinante |
| Dependências | EPIC-007, EPIC-008, EPIC-009, EPIC-019, EPIC-020 |
| Status | Planejada |

## 2. História do usuário

Como **usuário do AWAKEN**, quero **usar itens comprados ou conquistados com regras claras**, para **alterar a experiência de treino sem quebrar a economia nem gerar vantagem abusiva**.

## 3. Contexto

A migration cria os itens. Esta US define como cada item é consumido, limitado e aplicado pelo backend.

## 4. Objetivo

Criar handlers de efeito para os itens iniciais, com validação de posse, limite de uso e contexto permitido.

## 5. Escopo

### Entra nesta US

- Validar posse do item antes de usar.
- Consumir item quando aplicável.
- Aplicar efeito no contexto correto.
- Respeitar limite diário/semanal.
- Bloquear uso quando acesso estiver expirado.
- Registrar consumo no ledger/auditoria de economia.

### Fora desta US

- Troca entre usuários.
- Efeitos competitivos/social.
- Buffs permanentes de poder que criem pay-to-win.

## 6. Regras iniciais de efeito

| Item | Efeito sistêmico |
|---|---|
| Pergaminho da Reforja | Regenera a quest diária além do limite gratuito. |
| Pergaminho da Substituição | Substitui 1 exercício por outro compatível com perfil/equipamento. |
| Bússola da Dungeon | Troca a dungeon atual por outra. |
| Chave da Dungeon | Libera uma dungeon/treino especial do dia sem afetar quest principal. |
| Selo de Proteção | Protege o streak se o usuário falhar 1 dia. |
| Tônico de Recuperação | Marca 1 dia como recuperação ativa sem quebrar streak. |
| Amuleto de Retorno | Permite recuperar um streak perdido ontem apenas se treinar hoje. |
| Poção de Foco | +25% XP no próximo treino concluído. |
| Poção de Foco Grande | +50% XP no próximo treino concluído. |
| Poção da Sorte | Bônus de Gold encontrado na quest conforme regra de economia. |
| Pedra da Dungeon | Material para desbloquear dungeons especiais. |
| Pergaminho de Renomeação | Permite mudar nickname/codinome. |
| Pergaminho da Classe | Permite mudar classe. |

## 7. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O backend é a fonte de verdade de posse, consumo, limite e efeito. |
| RN-002 | Cliente nunca aplica efeito localmente. |
| RN-003 | Todo consumo deve ser idempotente por `useRequestId`. |
| RN-004 | Item consumível deve diminuir quantidade após uso confirmado. |
| RN-005 | Efeito com buff pendente deve ter validade e estado claro. |
| RN-006 | Limites diário/semanal são avaliados em UTC conforme regra de datas do produto. |
| RN-007 | Usuário sem acesso ativo não pode consumir item, salvo regra explícita futura. |
| RN-008 | Trial não deve usar item bloqueado pela política comercial vigente. |

## 8. Fluxo principal

1. Usuário toca em usar item no inventário ou em contexto permitido.
2. App envia solicitação ao backend.
3. Backend valida acesso, posse, limite e contexto.
4. Backend aplica efeito em transação.
5. Backend consome item quando aplicável.
6. App atualiza inventário/quest/perfil com resposta do backend.

## 9. Impacto Backend

- Criar `ItemEffectHandler` por tipo de efeito.
- Criar validação de contexto permitido.
- Integrar ao ledger/auditoria do EPIC-019.
- Garantir atomicidade com inventário e quest.

## 10. Impacto Flutter

- Botão de uso contextual.
- Estados: disponível, bloqueado, limite atingido, efeito ativo, consumido.
- Mensagens claras sem expor regra técnica demais.

## 11. Contrato API sugerido

```txt
POST /api/inventory/items/{inventoryItemId}/use
```

Request conceitual:

```json
{
  "contextType": "daily_quest",
  "contextId": "uuid",
  "useRequestId": "uuid"
}
```

## 12. Critérios de aceite

### CA-001 — Item usado com sucesso

Dado que o usuário possui um item válido,
quando usar em contexto permitido,
então o efeito deve ser aplicado pelo backend e o item consumido quando aplicável.

### CA-002 — Limite respeitado

Dado que o usuário já atingiu o limite diário/semanal,
quando tentar usar o item novamente,
então o backend deve bloquear a ação.

### CA-003 — Sem efeito local

Dado que o app está offline ou a API falha,
quando o usuário tentar usar item,
então nenhum efeito deve ser aplicado apenas no cliente.

## 13. Decisão registrada

Itens da loja são úteis e divertidos, mas o efeito sempre é validado no backend para preservar justiça, rastreabilidade e economia honesta.
