# UI kit — Caixa

Recriação do módulo **M3 · Caixa e Comandas**. Terminal fixo, densidade alta:
**todas as mesas em uma tela**, conta já montada, sem digitação de itens.

## Telas
| Tela | Conteúdo | Requisitos |
|---|---|---|
| Mesas e comandas | KPIs do turno + grade de mesas com valor/tempo + conta lateral | RF-CXA-01/02 |
| Recebimento | Formas de pagamento em grade, múltiplas formas na mesma conta, teclado numérico de valor | RF-CXA-03/10 |
| Fechamento | Sistema × conferido por forma, divergência, sangria/suprimento, taxa de cartão | RF-CXA-06/07/08, RF-FIN-10 |

## Decisões copiadas da especificação
- Item cancelado aparece **riscado, nunca removido** — a conta é documento auditável (RF-AUD-02).
- Taxa de serviço é um checkbox: a retirada é registrada, não escondida (RN-010).
- Divergência acima do limite gera alerta com ação de justificativa (RF-CXA-08).
- Desconto acima do limite exigiria autorização de perfil superior (RF-CXA-05) — botão presente, fluxo de PIN de gerente não detalhado nesta recriação.
