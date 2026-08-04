# E-05 · Caixa e Pagamento

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 9 |
| **Pontos** | 48 |
| **Sprints previstas** | Sprint 5 |
| **Aplicações afetadas** | web-pos, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/contracts |

---

## 1. Objetivo do épico

Fechar o ciclo operacional: da conta montada automaticamente ao caixa conferido no fim do turno.

A dor da persona P4 é *montar a conta na mão*. O sistema resolve isso pela origem — se cada item foi lançado com preço, modificador e fração corretos, a conta já existe pronta no momento em que o cliente pede.

Duas coisas exigem rigor aqui. **Dinheiro não admite arredondamento errado**: a soma dos pagamentos precisa bater com o total, sempre. E **toda exceção precisa de autorização registrada**: desconto acima do limite, divergência de fechamento, retirada de taxa de serviço — o que não é auditado vira buraco.

## 2. Valor entregue

- Conta montada automaticamente, sem digitação e sem erro de soma
- Múltiplas formas de pagamento na mesma conta, incluindo maquininha externa
- Desconto e divergência com autorização e trilha de auditoria
- Abertura e fechamento de caixa com conferência de valores
- Receita registrada automaticamente, alimentando o financeiro da Fase 3

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-050](./US-050-Painel-de-mesas-e-comandas-abertas.md) | Painel de mesas e comandas abertas | M | 5 | RF-CXA-01 |
| [US-051](./US-051-Conta-montada-automaticamente.md) | Conta montada automaticamente | M | 8 | RF-CXA-02 |
| [US-052](./US-052-Multiplas-formas-de-pagamento-na-mesma-conta.md) | Multiplas formas de pagamento na mesma conta | M | 8 | RF-CXA-03 |
| [US-053](./US-053-Taxa-de-servico-configuravel-com-retirada-registrada.md) | Taxa de servico configuravel com retirada registrada | M | 5 | RF-CXA-04 |
| [US-054](./US-054-Desconto-com-autorizacao.md) | Desconto com autorizacao | M | 5 | RF-CXA-05 |
| [US-055](./US-055-Abertura-e-fechamento-de-caixa.md) | Abertura e fechamento de caixa | M | 8 | RF-CXA-06, RF-CXA-08 |
| [US-056](./US-056-Sangria-e-suprimento.md) | Sangria e suprimento | S | 3 | RF-CXA-07 |
| [US-057](./US-057-Comprovante-nao-fiscal-de-consumo.md) | Comprovante nao fiscal de consumo | M | 3 | RF-CXA-12 |
| [US-058](./US-058-Registrar-pagamento-de-maquininha-externa.md) | Registrar pagamento de maquininha externa | M | 3 | RF-CXA-10 |

## 4. Ordem de execução recomendada

1. US-050 — painel de mesas e comandas abertas
2. US-051 — conta montada automaticamente
3. US-052 — múltiplas formas de pagamento
4. US-058 — pagamento em maquininha externa
5. US-053 — taxa de serviço
6. US-054 — desconto com autorização
7. US-055 — abertura e fechamento de caixa
8. US-056 — sangria e suprimento
9. US-057 — comprovante não fiscal

## 5. Dependências do épico

**Depende de:** E-00, E-02, E-03  
**Habilita:** E-06, E-07, E-12

## 6. Definition of Done do épico

- [ ] Conta montada corretamente em todos os cenários, incluindo meio a meio e adicionais
- [ ] Soma dos pagamentos igual ao total da conta, verificada por teste de propriedade
- [ ] Desconto e divergência exigindo autorização, com registro completo
- [ ] Fechamento de caixa com conferência e alerta de divergência
- [ ] Fluxo completo de fechamento operando com internet caída
- [ ] Receita gerando `financial_entry` automaticamente

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Modalidade de integração de pagamento indefinida (TEF versus gateway) | Alta | Alto | Pendência 4 do índice — o MVP registra a forma manualmente (US-058); a integração real fica na Fase 4 |
| Emissão fiscal não definida bloqueia o uso legal em produção | Alta | Crítico | Pendência 1 do índice — o MVP entrega comprovante não fiscal; NFC-e/SAT exige decisão do cliente e do contador |
| Erro de arredondamento em divisão de conta gerar divergência de caixa | Média | Alto | Invariante testada por propriedade; valores em centavos inteiros (ADR-017) |

---

*Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*