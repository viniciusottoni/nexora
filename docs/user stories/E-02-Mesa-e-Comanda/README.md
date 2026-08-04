# E-02 · Mesa e Comanda

|  |  |
|---|---|
| **Fase** | 1 — MVP |
| **Histórias** | 9 |
| **Pontos** | 47 |
| **Sprints previstas** | Sprint 3 |
| **Aplicações afetadas** | web-menu, web-pos, api-edge |
| **Pacotes do monorepo** | packages/domain, packages/contracts, packages/ui |

---

## 1. Objetivo do épico

Entregar a camada de salão: o QR Code que abre o cardápio sem instalação, a sessão de mesa que agrega o consumo, o mapa que dá visão do salão ao garçom e as ações que o cliente pode disparar sozinho — chamar garçom, ver o que já consumiu, pedir a conta.

A dor que este épico ataca é a do garçom que **anda para lançar e conferir** (persona P2). Cada ida desnecessária até o balcão é tempo que não vira atendimento.

## 2. Valor entregue

- Cliente pede sem esperar o garçom, e o garçom deixa de ser gargalo do salão
- Consumo da mesa visível em tempo real ao cliente — reduz a pergunta "quanto já deu?"
- Mapa de mesas com tempo e valor, permitindo enxergar o salão de uma tela só
- Base de medição de giro de mesa, ocupação e tempo de permanência
- Divisão de conta resolvida no sistema, não na calculadora do celular

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-020](./US-020-Cadastrar-ambientes-mesas-e-gerar-QR-Code.md) | Cadastrar ambientes mesas e gerar QR Code | M | 5 | RF-SAL-01 |
| [US-021](./US-021-Cliente-acessa-cardapio-pelo-QR-Code.md) | Cliente acessa cardapio pelo QR Code | M | 8 | RF-SAL-02 |
| [US-022](./US-022-Abrir-mesa-por-garcom-ou-por-cliente.md) | Abrir mesa por garcom ou por cliente | M | 5 | RF-SAL-04 |
| [US-023](./US-023-Mapa-de-mesas-com-status-e-tempo.md) | Mapa de mesas com status e tempo | M | 8 | RF-SAL-05 |
| [US-024](./US-024-Consumo-da-mesa-em-tempo-real.md) | Consumo da mesa em tempo real | M | 5 | RF-SAL-06 |
| [US-025](./US-025-Chamar-garcom-pela-mesa.md) | Chamar garcom pela mesa | M | 3 | RF-SAL-07 |
| [US-026](./US-026-Solicitar-a-conta.md) | Solicitar a conta | M | 3 | RF-SAL-08 |
| [US-027](./US-027-Dividir-a-conta.md) | Dividir a conta | M | 8 | RF-SAL-10 |
| [US-028](./US-028-Repetir-item-com-um-toque.md) | Repetir item com um toque | S | 2 | RF-SAL-11 |

## 4. Ordem de execução recomendada

1. US-020 — mesas e QR Code, pré-requisito de tudo
2. US-022 — abertura de mesa
3. US-021 — acesso do cliente pelo QR Code
4. US-024 — consumo em tempo real
5. US-023 — mapa de mesas do garçom
6. US-025 e US-026 — chamadas do cliente
7. US-027 — divisão de conta
8. US-028 — repetir item

## 5. Dependências do épico

**Depende de:** E-00, E-01  
**Habilita:** E-03, E-05, E-07

## 6. Definition of Done do épico

- [ ] Cliente abre o cardápio pelo QR Code em menos de 2 s em 4G, sem instalar nada
- [ ] Mapa de mesas refletindo status e tempo em tempo real
- [ ] Sessão de mesa medindo abertura, permanência e liberação
- [ ] Divisão de conta validada nos três modos (por pessoa, por item, por valor)
- [ ] Fluxo completo de salão operando com internet caída

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Wi-Fi instável na área de mesas degradar a experiência do cliente | Alta | Alto | Risco T3 do doc. 02 — rede dedicada à operação, VLAN separada e fallback de polling |
| Cliente não aderir ao pedido por QR Code e sobrecarregar o garçom | Média | Médio | Os dois caminhos são equivalentes por desenho; medir adoção no piloto |
| Quantidade e disposição de mesas ainda não informadas pelo cliente | Média | Baixo | Item pendente da lista de materiais (Visão Geral 20.2) |

---

*Épico E-02 · Pacote 004_DonaBetinha · Replay Studio.*