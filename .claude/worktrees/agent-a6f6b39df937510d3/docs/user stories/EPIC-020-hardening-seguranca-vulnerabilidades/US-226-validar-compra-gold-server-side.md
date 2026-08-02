---
title: US-226 — Validar compra de Gold server-side antes de creditar carteira
sidebar_position: 226
---

# US-226 — Validar compra de Gold server-side antes de creditar carteira

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-226 |
| Épico | EPIC-020 — Hardening de Segurança, Performance e Escalabilidade do MVP |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção com compra de Gold |
| Perfil principal | Usuário comprador, Backend, RevenueCat, Loja, Economia e QA |
| Plano | Trial, Mensal e Anual |
| Dependência principal | RevenueCat, ShopProduct, ShopOrder, GoldWallet, GoldLedgerEntry |
| Status | Planejada |

## 2. História do usuário

Como **usuário que compra Gold com dinheiro real**,

quero **receber o Gold apenas após validação segura no servidor**,

para **ter uma compra confiável, sem duplicidade, sem fraude e com saldo correto**.

## 3. Contexto

A compra de Gold será um produto monetizado e poderá ser usada para adquirir itens dentro do app. Isso torna o Gold um ativo sensível da economia interna. O app mobile nunca deve informar ao backend quanto Gold creditar; ele deve apenas iniciar a compra e enviar a referência necessária para validação server-side. O backend deve validar a transação com o provedor configurado e só então creditar a carteira.

## 4. Objetivo

Garantir que todo crédito de Gold comprado com dinheiro real seja validado, idempotente, auditado e rastreável.

## 5. Escopo

### Entra nesta US

- Criar fluxo específico para produtos de Gold comprados com dinheiro real.
- Validar transação no servidor antes de creditar Gold.
- Resolver quantidade de Gold exclusivamente pelo catálogo server-side.
- Bloquear qualquer quantidade enviada pelo app.
- Garantir idempotência por transação externa, loja/provider, ambiente e usuário.
- Criar ledger de crédito com referência ao pedido validado.
- Registrar auditoria segura.
- Tratar compra pendente, aprovada, negada, expirada e repetida.

### Fora desta US

- Promoções dinâmicas complexas.
- Marketplace entre usuários.
- Transferência de Gold entre usuários.
- Antifraude avançado com machine learning.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | O app nunca define a quantidade de Gold a creditar. |
| RN-002 | Quantidade de Gold vem do produto ativo no backend. |
| RN-003 | Transação não validada não gera crédito. |
| RN-004 | Mesma transação externa não pode creditar Gold duas vezes. |
| RN-005 | Transação validada para outro usuário não pode creditar a carteira atual. |
| RN-006 | Produto inativo, removido ou divergente deve bloquear crédito. |
| RN-007 | Crédito deve gerar ShopOrder, GoldLedgerEntry e audit log. |
| RN-008 | Payload sensível do provider não pode ser exposto no app ou logs. |

## 7. Fluxo principal

1. Usuário compra pacote de Gold no app.
2. App envia ao backend apenas a referência da compra e o produto selecionado.
3. Backend consulta o provider/RevenueCat para validar a transação.
4. Backend confere produto, usuário, loja, ambiente e status.
5. Backend resolve a quantidade de Gold pelo catálogo server-side.
6. Backend cria ou recupera pedido idempotente.
7. Backend credita Gold na carteira e grava ledger.
8. Backend retorna saldo atualizado e status seguro.

## 8. Fluxos alternativos

- Transação repetida: retorna resultado já processado sem novo crédito.
- Transação pendente: retorna status pendente, sem crédito.
- Transação negada: retorna erro controlado, sem crédito.
- Produto divergente: bloqueia crédito e registra alerta operacional.
- Provider indisponível: retorna pendente/erro recuperável, sem crédito.

## 9. Impacto no Backend

- Criar comando específico para compra de pacote de Gold.
- Adaptar `ShopProduct` para identificar produto que concede Gold.
- Persistir status de validação e referência externa.
- Garantir idempotência forte da transação.
- Criar testes negativos de fraude.

## 10. Impacto no Flutter

- App deve tratar estados: pendente, aprovado, negado, já processado e erro temporário.
- App não deve enviar quantidade de Gold.
- App deve sempre sincronizar saldo com backend após compra.

## 11. Impacto no Banco

- Índice único por transação externa, loja/provider e ambiente.
- Campos para status de validação e produto validado, se necessário.
- Ledger de Gold permanece append-only.

## 12. Critérios de aceite

- Compra validada credita Gold corretamente.
- Compra inventada não credita Gold.
- Compra repetida não duplica crédito.
- Quantidade enviada pelo app é ignorada ou rejeitada.
- Produto divergente bloqueia crédito.
- Falha do provider não credita Gold indevidamente.
- Ledger e auditoria são gravados.

## 13. Critérios de teste para QA

- compra aprovada;
- compra pendente;
- compra negada;
- transação repetida;
- produto inativo;
- produto divergente;
- tentativa com quantidade adulterada;
- falha temporária do provider.

## ✅ Decisão registrada

Compra de Gold com dinheiro real deve ser tratada como fluxo financeiro sensível: backend valida, backend define quantidade, backend credita e backend audita.