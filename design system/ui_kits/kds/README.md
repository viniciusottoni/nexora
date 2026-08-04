# UI kit — KDS Cozinha

Recriação do módulo **M2 · KDS**. Superfície escura (`data-surface="kds"`), cartões
grandes, legível a 1,5 m, **operação só por teclado numérico** — a regra de ouro é
"mãos sujas, sem mouse, sem digitação".

Interação real no protótipo: digite o número de um pedido e pressione **Enter** para
concluí-lo (RF-KDS-04); Backspace apaga.

## Composição da tela
| Bloco | Conteúdo | Requisitos |
|---|---|---|
| Cabeçalho | Praça, contagem da fila, atrasados, tempo médio da última hora, sync | RF-KDS-06, RF-BI-02, RF-OFF-05 |
| Fila | `OrderTicket` com cronômetro verde/amarelo/vermelho, modificadores em amarelo, fire time | RF-KDS-02/03/09 |
| Forno | Posições ocupadas, tempo restante e alerta de ociosidade com fila | RF-KDS-08, doc otimização §3.2 |
| All-day | Contagem consolidada de itens iguais para montar em lote | RF-KDS-07 |
| Comando | Buffer numérico + atalhos "falta insumo" e "refazer" | RF-KDS-04/10/11 |

## Decisões copiadas da especificação
- Modificadores **sempre em amarelo** — é o que mais gera erro de produção.
- Cartão atrasado ganha contorno vermelho e o cronômetro pulsa; nada mais muda de layout.
- Fila ordenada por urgência calculada, não por ordem de chegada (RF-KDS-12).
