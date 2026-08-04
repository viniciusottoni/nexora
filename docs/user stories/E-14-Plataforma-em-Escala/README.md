# E-14 · Plataforma em Escala

|  |  |
|---|---|
| **Fase** | 5 — Produto replicável em escala |
| **Histórias** | 7 |
| **Pontos** | 47 |
| **Sprints previstas** | Fase 5 |
| **Aplicações afetadas** | web-platform, api-cloud, infra/cloud, infra/edge |
| **Pacotes do monorepo** | packages/config, infra |

---

## 1. Objetivo do épico

Transformar o produto de "sistema implantado em um cliente" em **produto replicável em escala**.

A Fase 0 criou a fundação multi-tenant; esta fase entrega as ferramentas que tornam a operação da Replay viável com N clientes: provisionamento autoatendido, monitoramento remoto do parque, domínio próprio, atualização controlada e acesso de suporte auditado.

A métrica que define o sucesso do épico é dura: **tempo de implantação de novo estabelecimento ≤ 5 dias úteis** (PRD, seção 7).

## 2. Valor entregue

- Implantação de novo cliente sem desenvolvimento e com pouco suporte
- Saúde de todas as instalações visível em uma tela
- Atualização do parque controlada, com rollback automático
- Domínio próprio por cliente, reforçando o white-label
- Acesso de suporte auditado e visível ao cliente

## 3. Histórias

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| [US-140](./US-140-Painel-de-instalacoes-com-saude.md) | Painel de instalacoes com saude | M | 8 | RF-PLT-07 |
| [US-141](./US-141-Provisionamento-autoatendido.md) | Provisionamento autoatendido | M | 8 | RF-PLT-05 |
| [US-142](./US-142-Modelos-por-tipo-de-negocio.md) | Modelos por tipo de negocio | S | 8 | RF-PLT-06 |
| [US-143](./US-143-Dominio-proprio-por-cliente.md) | Dominio proprio por cliente | S | 8 | RF-PLT-03 |
| [US-144](./US-144-Importacao-de-cardapio-por-planilha.md) | Importacao de cardapio por planilha | S | 5 | RF-CAT-12 |
| [US-145](./US-145-Acesso-de-suporte-auditado.md) | Acesso de suporte auditado | M | 5 | RF-PLT-08 |
| [US-146](./US-146-Atualizacao-controlada-do-parque.md) | Atualizacao controlada do parque | M | 5 | — |

## 4. Ordem de execução recomendada

1. US-140 — painel de instalações com saúde
2. US-145 — acesso de suporte auditado
3. US-146 — atualização controlada do parque
4. US-141 — provisionamento autoatendido
5. US-144 — importação de cardápio por planilha
6. US-142 — modelos por tipo de negócio
7. US-143 — domínio próprio por cliente

## 5. Dependências do épico

**Depende de:** E-00, E-06  
**Habilita:** —

## 6. Definition of Done do épico

- [ ] Implantação completa de um cliente novo em menos de 5 dias úteis
- [ ] Painel de saúde cobrindo todas as instalações, com alerta automático
- [ ] Atualização do parque com rollback automático testado
- [ ] Acesso de suporte sempre registrado e visível ao cliente
- [ ] Nenhuma etapa de implantação exigindo desenvolvimento

## 7. Riscos do épico

| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Suporte a várias instalações locais crescer além da capacidade | Média | Médio | Risco 14 da Visão Geral — monitoramento remoto e padronização de hardware desde a Fase 1 |
| Deriva de versão entre lojas | Média | Médio | Risco T7 do doc. 02 — atualização automática com janela e monitoramento de versão |
| Pressão comercial por customização de código | Alta | Alto | Governança do ADR-013 e trava no CI (US-007) |

---

*Épico E-14 · Pacote 004_DonaBetinha · Replay Studio.*