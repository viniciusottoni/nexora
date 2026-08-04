# UI kit — Mesa (PWA do cliente)

Recriação do módulo **M1 · Mesa / Pedido no salão**. Aberto pelo QR Code da mesa, no
navegador do próprio cliente: sem instalar app e sem cadastro (RF-SAL-02).

**Tenant:** `data-tenant="dona-betinha"` no `<body>` — todo o app assume a cor do
estabelecimento, não a da Nexora. É a demonstração viva da camada white-label.

## Telas
| Arquivo | Tela | Requisitos |
|---|---|---|
| `MesaApp.jsx` › `Cardapio` | Cardápio por categoria, prazo calculado pela fila, item esgotado | RF-CAT-01/07, RF-PED-07 |
| `MesaApp.jsx` › `Produto` | Meio a meio, grupos de modificadores, observação livre | RF-CAT-03/04/05, RF-PED-08 |
| `MesaApp.jsx` › `Pedido` | Carrinho, sugestão contextual, envio à cozinha | RF-SAL-03 |
| `MesaApp.jsx` › `Acompanhar` | Etapas T0→T5 com cronômetro e previsão dinâmica | RF-KDS-05, RF-PED-07 |
| `MesaApp.jsx` › `Consumo` | Consumo em tempo real, taxa opcional, divisão, pedir a conta | RF-SAL-06/08/10, RN-010 |

## Decisões copiadas da especificação
- Alvos de 48–64px em tudo que é toque; corpo de texto em 16px (leitura em movimento).
- `SyncStatus` sempre visível: em modo local o cliente vê que a operação continua.
- Foto de produto é **placeholder declarado** — nenhuma imagem foi fornecida.
