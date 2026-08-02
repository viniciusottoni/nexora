---
title: US-229 — Monitorar economia Gold, carteira, ledger e compras internas
sidebar_position: 229
---

# US-229 — Monitorar economia Gold, carteira, ledger e compras internas

## 1. Identificação

| Campo | Valor |
|---|---|
| ID | US-229 |
| Épico | EPIC-017 — Site Admin e Monitoramento Operacional |
| Origem | US-226, US-227, US-228 |
| Prioridade | P0 |
| Fase | Bloqueador do MVP em produção com economia Gold |
| Perfil principal | Admin, Produto, Segurança, Suporte e Engenharia |
| Plataforma | Web Admin React + Backend Admin API |
| Status | Planejada |

## 2. História do usuário

Como **admin responsável pela economia do AWAKEN**,

quero **monitorar saldo de Gold, ledger, compras internas, créditos e alertas de divergência**,

para **detectar problemas na economia antes que afetem usuários ou sejam explorados**.

## 3. Contexto

Gold será comprado com dinheiro real e usado dentro do app para comprar itens do jogo. O Admin precisa mostrar a saúde dessa economia: compras aprovadas, créditos, débitos, pedidos concedidos, saldo por usuário, alertas de divergência, volume anormal e reconciliação.

## 4. Objetivo

Criar uma tela administrativa de diagnóstico da economia Gold.

## 5. Escopo

### Entra nesta US

- Cards de Gold comprado, Gold gasto, pedidos internos e alertas abertos.
- Busca por usuário, pedido, produto, referência e período.
- Lista de movimentações de Gold com direção, motivo, referência e saldo resultante.
- Lista de pedidos internos com status e produto.
- Alertas de divergência gerados pela reconciliação.
- Detalhe seguro de carteira, ledger e pedido.
- Link para audit log, usuário e alerta relacionado.
- Exportação segura de relatório operacional.

### Fora desta US

- Edição manual de saldo no MVP.
- Concessão manual de Gold pelo Admin no MVP.
- Marketplace entre usuários.
- Correção automática de divergência.

## 6. Regras de negócio

| ID | Regra |
|---|---|
| RN-001 | Admin não pode alterar saldo de Gold manualmente no MVP. |
| RN-002 | Toda visualização deve ser somente leitura, exceto marcação de alerta como analisado. |
| RN-003 | Dados de pagamento/provider devem permanecer mascarados ou ausentes. |
| RN-004 | Divergência de saldo deve aparecer como crítico. |
| RN-005 | Pedido concedido sem débito ou crédito sem origem deve aparecer como bloqueador operacional. |
| RN-006 | Toda exportação deve ser auditada. |

## 7. Indicadores mínimos

- Total de Gold comprado no período.
- Total de Gold gasto no período.
- Saldo total em circulação.
- Pedidos internos concedidos, pendentes e falhos.
- Alertas de divergência abertos.
- Top produtos comprados com Gold.
- Usuários com volume anormal.
- Última reconciliação executada.

## 8. Fluxo principal

1. Admin acessa Economia Gold.
2. Sistema exibe indicadores agregados.
3. Admin filtra por período, produto, usuário ou status.
4. Admin abre detalhe de carteira, pedido ou alerta.
5. Admin registra análise ou navega para audit log.

## 9. Impacto no Frontend

- Nova página `Economia Gold` no Admin.
- Cards, tabela de movimentações, tabela de pedidos e lista de alertas.
- Drawer/modal de detalhe seguro.
- Links cruzados para usuário, audit log e alertas.

## 10. Impacto no Backend

- Endpoints admin somente leitura para economia Gold.
- Endpoints agregados por período/status/produto.
- Integração com alertas da reconciliação.
- Paginação/cursor para ledger e pedidos.

## 11. Critérios de aceite

- Admin visualiza Gold comprado, gasto e em circulação.
- Admin visualiza ledger paginado por usuário/período.
- Admin visualiza compras internas por status.
- Alertas de divergência ficam destacados.
- Admin não consegue editar saldo pelo painel.
- Exportação gera auditoria.
- Não há exposição de dados sensíveis de pagamento.

## 12. Critérios de teste para QA

- carteira sem movimentação;
- carteira com crédito comprado;
- compra interna com Gold;
- pedido falho;
- divergência aberta;
- usuário com volume anormal;
- exportação segura;
- admin sem permissão.

## ✅ Decisão registrada

A economia Gold deve ter visibilidade operacional no Admin, mas sem permitir alteração manual de saldo no MVP.