# UI kit — Painel do dono

Recriação do módulo **M10 · Controle e métrica total**. Organizado na hierarquia de
informação da especificação: **Pulso → Desempenho → Resultado**, e não por tipo de gráfico.

## Telas
| Tela | Responde | Requisitos |
|---|---|---|
| **Pulso** | "o que está acontecendo agora?" — 5 números, pedidos em produção com cronômetro, alertas com ação, meta do dia | RF-BI-01/11/14 |
| **Desempenho** | "como foi o dia/semana/mês?" — tempo por etapa vs. padrão, p90, mapa de calor, canal, pessoas, qualidade | RF-BI-02/03/04/06/08 |
| **Resultado e custo** | "estou ganhando dinheiro, em quê?" — CMV, prime cost, ponto de equilíbrio, engenharia de cardápio, composição do resultado | RF-BI-09, RF-FIN-05/06/08 |

## Decisões copiadas da especificação
- **Nenhum número sem comparativo ou meta** — todo `StatTile` traz `comparison` ou `target`.
- **Percentil 90 ao lado da média** — a média esconde o cliente insatisfeito (anti-padrão nº 4).
- **Alerta por exceção, com a ação embutida** — nunca uma lista de avisos sem botão.
- **`SyncStatus` no rodapé da navegação e na faixa de pulso** — dado defasado nunca se
  passa por tempo real (RF-BI-14).
- Toda linha de pedido tem `chevron_right`: do número até o evento de origem em ≤ 3 cliques.
