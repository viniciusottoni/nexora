# ADR-022 — Inventário e loja: escopo mínimo funcional

Status: Aceito

## Contexto

A US-048 (regenerar quest dentro de limites) exige que, ao esgotar o limite diário gratuito de regenerações, o app verifique se o usuário possui o item "Pergaminho da Reforja" no inventário e, com confirmação explícita, permita consumi-lo para regenerar mesmo assim. Sem o item, o app deve oferecer ir à loja comprá-lo.

Antes desta US, o projeto não possuía nenhuma modelagem de inventário, itens ou loja no backend — apenas telas de mockup estático no Flutter (`features/inventory`, `features/shop`) usadas para prototipagem visual, sem dados reais nem persistência. Construir um sistema completo de economia (moeda, catálogo extenso, integração de pagamento real via RevenueCat/IAP para consumíveis) está fora do escopo do EPIC-006 e exigiria sua própria US/ADR dedicados.

## Decisão

Implementar o menor recorte de inventário e loja que sustente o fluxo do "Pergaminho da Reforja", reutilizável para itens futuros:

- Domínio: `InventoryItem` (`UserId`, `ItemKey`, `Quantity`) — registro genérico de quantidade de um item por usuário. `ItemKeys.ReforgeScroll` (`reforja_scroll`) é a primeira chave estável.
- `ShopCatalog`: lista estática (em código) de itens com efeito real no backend e seu preço em moeda interna ("Gold"), hoje contendo apenas o Pergaminho da Reforja.
- Endpoints minimos:
  - `GET /api/inventory/items/{itemKey}` — quantidade do item no inventário do usuário autenticado.
  - `GET /api/shop/items` — catálogo de itens com efeito real.
  - `POST /api/shop/items/{itemKey}/purchase` — compra **mock**: adiciona 1 unidade ao inventário, sem dedução de moeda nem integração de pagamento real.
- O consumo do item ocorre apenas dentro do fluxo de regeneração de quest (`RegenerateDailyQuestCommandHandler`), nunca exposto como endpoint genérico de "consumir item" — evita introduzir semântica genérica antes de haver outros itens consumíveis reais.
- As telas Flutter de inventário/loja (mock visual, com itens decorativos não funcionais) ganham o Pergaminho da Reforja como item real adicional, identificável por `itemKey`; os demais itens da loja seguem ilustrativos.

## Fora de escopo (explicitamente deferido)

- Moeda real/IAP para consumíveis (a compra é mock; deduzir "Gold" de um saldo real e a emissão desse saldo ficam para uma US de economia futura).
- Catálogo completo de itens, equipáveis, raridades com efeito de jogo, e tela de inventário com dados reais do backend.
- Endpoint administrativo de auditoria de geração de quest (US-049 seção 17) — os dados de auditoria são persistidos na entidade `Quest`, mas a exposição via API para times internos não foi implementada nesta US; é um candidato a follow-up quando houver um sistema de papéis/admin (hoje o backend não possui RBAC).

## Consequências

O fluxo do Pergaminho da Reforja funciona ponta a ponta (verificação, confirmação explícita, consumo, compra mock) sem represar a US-048/US-049 esperando por um sistema de economia completo. A modelagem de `InventoryItem`/`ItemKeys`/`ShopCatalog` é extensível: novos itens reais bastam uma nova chave e entrada no catálogo, sem alterar o schema.

## Critérios de aceite

- Existe uma tabela `inventory_items` com índice único `(UserId, ItemKey)`.
- `GET /api/inventory/items/{itemKey}` retorna `quantity: 0` quando o usuário nunca obteve o item (sem erro).
- A regeneração de quest além do limite só ocorre com confirmação explícita do usuário para consumir o item (nunca consome automaticamente).
- A compra mock incrementa o inventário e não exige nenhuma integração de pagamento.
- Nenhum outro endpoint genérico de consumo de item foi criado além do necessário para US-048.
