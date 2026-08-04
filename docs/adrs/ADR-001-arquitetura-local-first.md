# ADR-001 · Arquitetura local-first com servidor na loja

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-007, ADR-011, ADR-027, ADR-033, ADR-034 |
| **Requisitos afetados** | RF-OFF-01 a 08, RF-PED-09, RNF-DIS-03, RNF-PER-01 |

---

## Contexto

O cliente foi categórico na descoberta:

> *"É necessário funcionar sem internet rodando em rede local as mesas, caixa e KDS. Se internet cair, produção local continua funcionando. Sistema sempre deve publicar no servidor remoto, mas se localmente ficar sem internet, mantém tudo local, e depois sobe para o servidor web."*

Uma pizzaria em horário de pico não tolera interrupção. Em ponto comercial no Brasil, instabilidade de internet é regra, não exceção — link único, sem redundância, compartilhado com o Wi-Fi dos clientes. Um sistema que para junto com a internet seria abandonado na primeira sexta-feira à noite.

Há ainda uma segunda força: o requisito de pedido chegar ao KDS em menos de 2 segundos (RNF-PER-01). Um trajeto mesa → nuvem → KDS depende de latência e disponibilidade que não controlamos.

## Forças em jogo

| Força | Descrição |
|---|---|
| Continuidade | A operação não pode parar por causa da internet |
| Latência | Pedido → KDS em menos de 2 s |
| Coordenação | Vários dispositivos precisam ver o mesmo estado, mesmo offline |
| Consolidação | Gestão, financeiro e delivery precisam de visão central |
| Custo | Hardware por loja é custo real e recorrente |

## Decisão

**Cada loja recebe um servidor local (edge) que é a autoridade operacional.**

O edge roda API, banco de dados e WebSocket em containers, na rede interna da loja. Mesa, garçom, KDS e caixa falam com ele — nunca diretamente com a nuvem. A nuvem consolida os dados, serve os canais externos (delivery, painel do dono, financeiro) e administra a plataforma.

## Detalhamento

### Divisão de autoridade

| Domínio | Autoridade | Justificativa |
|---|---|---|
| Pedido, item, mesa, comanda | **Edge** | Criado durante o serviço; não pode parar |
| KDS e produção | **Edge** | Latência crítica |
| Caixa e pagamento presencial | **Edge** | Não pode parar |
| Catálogo, preços, configuração | **Nuvem** | Editado pela gestão, apenas lido pela operação |
| Ficha técnica | **Nuvem** | Idem |
| Movimentos de estoque | Ambos (ver ADR-008) | Baixa nasce no edge, compra na nuvem |
| Financeiro, métricas, delivery | **Nuvem** | Não é operação crítica de tempo real |

### Regra de ouro da autoridade

> **Um dado tem um único dono.** Quem não é dono apenas lê. Onde isso não é possível (estoque), aplica-se ADR-008 — sincronizam-se movimentos, não saldos.

### Topologia

```
   Internet ──► NUVEM (multi-tenant)
                  ▲
                  │ sync bidirecional (ADR-007)
                  ▼
   ══════════════════════════════════
   LOJA ─► EDGE SERVER (autoridade operacional)
              │ LAN
      ┌───────┼────────┬────────┐
    Mesa   Garçom     KDS     Caixa
```

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| 100% nuvem com cache offline no navegador | Sem hardware; deploy único | IndexedDB não coordena dispositivos entre si | Dois garçons offline não veriam o pedido um do outro; o KDS não receberia nada |
| Peer-to-peer entre dispositivos | Sem servidor na loja | Consenso distribuído sem fonte de verdade | Complexidade desproporcional; qual tablet é a verdade? |
| Nuvem com modo degradado só de leitura | Simples | Não permite criar pedido offline | Não atende ao requisito central |
| Edge apenas como cache/proxy | Menos estado local | Ainda depende da nuvem para escrever | Não atende ao requisito central |

## Consequências

**Positivas**

- A operação nunca para — diferencial comercial real, não apenas técnico
- Latência de pedido → KDS abaixo de 2 s, sem depender de link externo
- Funciona em estabelecimento com internet ruim, que é a maioria do público-alvo
- A loja continua vendendo mesmo em queda total de provedor

**Negativas**

- Hardware por loja: custo, logística de compra, instalação e manutenção física
- A sincronização passa a ser o componente mais complexo do sistema (ADR-007)
- Suporte remoto a N instalações físicas distribuídas
- Novo modo de falha: o servidor local pode quebrar
- Atualização do parque instalado exige estratégia própria (ADR-019, ADR-029)

**Mitigações**

- Hardware padronizado e documentado, com nobreak obrigatório
- Equipamento reserva pré-configurado (cold standby) — ADR-033
- Monitoramento remoto de cada instalação desde a Fase 1 (RNF-OBS-05/06)
- Runbook de contingência ensaiado antes do piloto autônomo
- Cache de contingência nos dispositivos (ADR-027) para falha do próprio edge

## Como validar

- Teste de caos C-01: corte de internet durante serviço simulado — operação segue integralmente
- Teste de caos C-07: reinício do edge no pico — volta em menos de 60 s, sem perder pedido
- Métrica RNF-PER-01 medida em produção: p95 de pedido → KDS abaixo de 2 s
- Piloto: 4 horas de operação real com link desligado, sem perda de dado

## Revisitar quando

- A conectividade do público-alvo mudar substancialmente (5G fixo confiável e barato)
- O custo de manutenção do parque de hardware superar o valor da continuidade
- Surgir uma tecnologia de sincronização que torne o modelo 100% nuvem viável offline com coordenação entre dispositivos
