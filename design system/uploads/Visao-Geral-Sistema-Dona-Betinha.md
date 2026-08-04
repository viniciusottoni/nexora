# Sistema de Controle Total — Ecossistema Dona Betinha
## Documento de Visão Geral do Produto

| | |
|---|---|
| **Cliente / Projeto** | Dona Betinha — Ecossistema de Gestão e Operação de Pizzaria |
| **Código interno** | 004_DonaBetinha |
| **Documento** | Visão Geral do Produto (consolidação da descoberta inicial) |
| **Origem** | `Assets/Briefing-Pedido-Inicial.docx` — RS-BRF-MTG-001 |
| **Versão** | 1.1 |
| **Data** | 30/07/2026 |
| **Responsável Replay** | Sáskia |
| **Status** | Descoberta consolidada — pendente de validação com o cliente |

> **Aviso de natureza do documento**
> Este material consolida e organiza o que foi registrado na reunião de descoberta inicial, acrescido das duas diretrizes estratégicas definidas na sequência: **foco no ecossistema completo com controle e métrica total para o dono**, e **arquitetura de produto customizável, replicável em qualquer estabelecimento com as mesmas dores**. Trechos marcados como *Hipótese* ou *Pendência* representam interpretação da Replay ou lacuna de informação e **precisam ser confirmados** antes de virarem compromisso de escopo.

---

## Sumário

1. [Resumo executivo](#1-resumo-executivo)
2. [Contexto e problema central](#2-contexto-e-problema-central)
3. [Visão do produto e posicionamento](#3-visão-do-produto-e-posicionamento)
4. [O ecossistema completo](#4-o-ecossistema-completo)
5. [Usuários, perfis e permissões](#5-usuários-perfis-e-permissões)
6. [Arquitetura funcional — os módulos do sistema](#6-arquitetura-funcional--os-módulos-do-sistema)
7. [Controle e métrica total — o painel do dono](#7-controle-e-métrica-total--o-painel-do-dono)
8. [Produto customizável — multi-estabelecimento e white-label](#8-produto-customizável--multi-estabelecimento-e-white-label)
9. [Fluxos operacionais principais](#9-fluxos-operacionais-principais)
10. [Regras de negócio e lógica do sistema](#10-regras-de-negócio-e-lógica-do-sistema)
11. [Dados, cadastros e informação](#11-dados-cadastros-e-informação)
12. [Integrações e pagamentos](#12-integrações-e-pagamentos)
13. [Canais, dispositivos e experiência](#13-canais-dispositivos-e-experiência)
14. [Requisito estruturante: operação offline-first](#14-requisito-estruturante-operação-offline-first)
15. [Notificações e alertas](#15-notificações-e-alertas)
16. [Auditoria, rastreabilidade e administração](#16-auditoria-rastreabilidade-e-administração)
17. [Priorização — MVP e evolução por fases](#17-priorização--mvp-e-evolução-por-fases)
18. [Critérios de sucesso](#18-critérios-de-sucesso)
19. [Riscos e pontos de atenção](#19-riscos-e-pontos-de-atenção)
20. [Pendências e próximos passos](#20-pendências-e-próximos-passos)
21. [Anexo — Separação entre fato, hipótese e pendência](#21-anexo--separação-entre-fato-hipótese-e-pendência)

---

## 1. Resumo executivo

A Dona Betinha opera hoje **sem nenhum sistema de gestão**. O único apoio digital existente é um cardápio web para delivery e a presença no iFood. Salão, cozinha, caixa, estoque e finanças funcionam sem registro estruturado, sem rastreabilidade e sem indicadores. O dono não consegue responder perguntas básicas sobre o próprio negócio: quanto tempo leva uma pizza, quanto custa produzi-la, quanto sobrou de insumo, qual produto dá lucro.

O pedido é a criação de um **ecossistema único de controle total da pizzaria** — do toque do cliente na mesa até o resultado financeiro do mês. Duas diretrizes orientam o produto:

**Diretriz 1 — Controle e métrica total para o dono.**
O sistema não é um conjunto de telas operacionais. É um **instrumento de gestão**: cada ação da operação existe também como dado, e todo dado converge para uma camada de indicadores que responde, em tempo real e em histórico, como o negócio está indo. Operação e métrica são a mesma coisa vista de dois ângulos.

**Diretriz 2 — Produto customizável e replicável.**
O sistema não é feito só para a Dona Betinha. É desenhado desde a origem como **produto multi-estabelecimento**: qualquer negócio de alimentação com as mesmas dores (pizzaria, hamburgueria, restaurante, lanchonete) pode ser implantado sobre a mesma base, com **toda a camada web personalizada** — marca, cores, domínio, cardápio, identidade e configuração operacional próprias.

### Frentes do ecossistema

| Frente | O que resolve |
|---|---|
| **Salão / Mesa** | Pedido na mesa (QR Code ou celular do garçom), consumo por mesa/comanda |
| **Cozinha (KDS)** | Pedido chega direto, com controle de início/conclusão e tempo de produção |
| **Caixa** | Mesas e comandas consolidadas, recebimento e fechamento |
| **Delivery próprio** | Pedido online com marca própria e foco em entrega rápida (meta: 25 min) |
| **Estoque e ficha técnica** | Quanto cada produto consome, quanto entrou, quanto saiu, quanto perdeu |
| **Financeiro** | Salários, insumos, custos fixos, margem, resultado |
| **Painel do dono** | Métrica total: tempo, venda, custo, pessoas, saúde do negócio |
| **Plataforma** | Multi-estabelecimento, white-label, configuração por cliente |

### Requisitos estruturantes

- **A operação precisa continuar funcionando sem internet.** Mesas, caixa e KDS rodam em rede local; a nuvem consolida, administra e publica. Sincronização automática ao retornar a conexão.
- **Todo evento gera alerta** para o usuário responsável — mesa, cozinha, caixa e gestão.
- **Tudo é medido.** Nenhuma etapa acontece sem carimbo de tempo, autor e registro.
- **Tudo que é web é personalizável** por estabelecimento.

---

## 2. Contexto e problema central

### 2.1 Situação atual (registrada na descoberta)

> *"Hoje eu não tenho processo."*
> *"Eu dou um tiro no escuro pois não sei quais etapas hoje são mais rápidas e mais lentas."*
> *"O pedido é feito e não chega para cozinha."*

**O que existe hoje**

- Cardápio web usado como canal de delivery
- iFood como marketplace
- Maquininha Cielo (conta Banco do Brasil) e maquininha Mercado Pago
- Contador externo, responsável apenas pela contabilidade formal

**O que não existe hoje**

- Qualquer sistema para caixa, mesa ou cozinha
- Registro de tempo de produção ou de etapas do pedido
- Controle de estoque e de consumo de insumos por produto
- Visão financeira da operação (custos, margem, saúde do negócio)
- Histórico, auditoria ou rastreabilidade de qualquer ação
- Qualquer indicador de desempenho

### 2.2 Problema central

> **A pizzaria opera sem informação.** Pedidos se perdem entre salão e cozinha, não há medição de tempo em nenhuma etapa, não se sabe quanto insumo cada produto consome nem quanto custa produzi-lo, e a saúde financeira do negócio não é observável. A gestão decide por intuição.

O problema não é apenas operacional — é **de cegueira gerencial**. Resolver o fluxo do pedido sem entregar a camada de medição resolveria metade da dor.

### 2.3 Resultado desejado

Declarações registradas do cliente:

- *"Não ter papel passando para cima e para baixo, vender pizza rápido"*
- *"Eu vou saber quantos minutos a minha pizza tá sendo feita, saber como está o consumo das mesas"*
- *"Entregar pizza na casa das pessoas em 25 min"*
- *"Pedido está na mesa em 10 minutos"*
- *"Gerenciar as finanças para saber a saúde financeira, controlar todo o processo de pedidos de mesas até o pagamento e fechamento"*

### 2.4 O que não pode ser perdido

Não se aplica. O cliente declarou não possuir processo estruturado atual — o sistema **cria** o processo em vez de digitalizar um existente. Isso é uma oportunidade (liberdade de desenho, e base limpa para um produto replicável) e um risco (o processo precisa ser desenhado pela Replay e validado, não extraído do cliente).

---

## 3. Visão do produto e posicionamento

### 3.1 Declaração de visão

> Um ecossistema único que controla o estabelecimento de ponta a ponta — do toque do cliente na mesa até o resultado financeiro do mês — funcionando com ou sem internet, com cada etapa cronometrada, rastreada e notificada, entregando ao dono controle e métrica total do negócio; e replicável, com identidade própria, para qualquer estabelecimento com as mesmas dores.

### 3.2 Os dois níveis do produto

| Nível | O que é | Quem usa |
|---|---|---|
| **Plataforma** | Base comum: operação, medição, financeiro, sincronização, motor de configuração e personalização | Replay (mantém e evolui) |
| **Instância do estabelecimento** | Configuração + identidade visual + cardápio + regras próprias de um cliente específico | Dona Betinha (primeira instância), e cada novo cliente |

A Dona Betinha é a **primeira implantação e o caso de validação**, não uma customização única. Toda decisão de desenho deve responder à pergunta: *isto é regra do negócio de pizzaria ou parâmetro configurável do produto?*

### 3.3 Referências mencionadas pelo cliente

| Referência | O que representa no pedido |
|---|---|
| **Yon San** | Modelo de pedido/delivery desejado |
| **McDonald's (KDS)** | Painel de cozinha: pedido chega direto, com controle de produção |
| **Vila Frios** | Modelo de aplicativo de vendas/catálogo |

> **Pendência crítica.** É necessário confirmar **o que exatamente** o cliente valoriza em cada referência (fluxo, velocidade, aparência, organização). Referências sem essa qualificação geram divergência de expectativa no desenho de UX.

### 3.4 Escopo adjacente — "App de frios"

O briefing registra a intenção de um **aplicativo de venda de frios**, com público declarado: *"Pizzaria, Hamburgueria e Condomínios"*, e uma regra de preço diferenciada (*"preço de Ceasa, fora da Ceasa"*).

> **Hipótese / Alerta de escopo.** Isso caracteriza um **canal de venda B2B/B2C de insumos**, com catálogo, logística, tabelas de preço por público e ciclo comercial distintos da operação da pizzaria. Recomenda-se tratá-lo como **módulo de canal adicional da plataforma** (aproveitando catálogo, estoque, pagamento e personalização já existentes), mas **fora da primeira entrega**. Requer decisão explícita do cliente.

---

## 4. O ecossistema completo

O termo "sistema" subestima o que foi pedido. O que está em desenho é um **ecossistema** com quatro camadas que se alimentam mutuamente:

```
┌──────────────────────────────────────────────────────────────────────┐
│  CAMADA 4 · INTELIGÊNCIA DE GESTÃO                                   │
│  Painel do dono · KPIs · Metas · Alertas gerenciais · Comparativos    │
│  Responde: "como está o meu negócio, agora e ao longo do tempo?"     │
└──────────────────────────────────────────────────────────────────────┘
                                  ▲ consome
┌──────────────────────────────────────────────────────────────────────┐
│  CAMADA 3 · RETAGUARDA                                               │
│  Estoque · Ficha técnica · Custo · Financeiro · Pessoas · Compras     │
│  Responde: "quanto custa, quanto sobra, quanto sai, quanto rende?"   │
└──────────────────────────────────────────────────────────────────────┘
                                  ▲ alimenta
┌──────────────────────────────────────────────────────────────────────┐
│  CAMADA 2 · OPERAÇÃO                                                 │
│  Mesa · Cozinha (KDS) · Caixa · Delivery · Entrega                    │
│  Responde: "o pedido chegou, foi feito, foi entregue, foi pago?"     │
└──────────────────────────────────────────────────────────────────────┘
                                  ▲ sustenta
┌──────────────────────────────────────────────────────────────────────┐
│  CAMADA 1 · PLATAFORMA                                               │
│  Multi-estabelecimento · Personalização · Sincronização local/nuvem   │
│  Usuários · Permissões · Auditoria · Configuração · Integrações       │
└──────────────────────────────────────────────────────────────────────┘
```

**Princípio de ligação entre camadas:** nenhum dado é digitado duas vezes. O pedido lançado na mesa vira produção no KDS, vira baixa de insumo no estoque, vira custo no financeiro e vira indicador no painel do dono — a partir do mesmo evento original.

---

## 5. Usuários, perfis e permissões

### 5.1 Perfis identificados

| Perfil | Onde atua | Precisa conseguir | Não deve poder |
|---|---|---|---|
| **Cliente do salão** | Mesa (QR Code) | Ver cardápio, montar e enviar pedido, acompanhar status, chamar garçom, ver consumo da mesa | Alterar preços, cancelar itens em produção, ver outras mesas |
| **Garçom** | Celular / tablet | Abrir mesa, lançar e ajustar pedido, acompanhar status, solicitar fechamento | Aplicar desconto, cancelar pagamento, acessar financeiro |
| **Cozinha / Produção** | KDS com teclado numérico | Ver fila, marcar início e conclusão, sinalizar falta de item | Alterar valores, cancelar pedido, acessar caixa |
| **Caixa / Atendente** | Terminal / desktop | Ver mesas e comandas, receber, fechar conta, abrir/fechar caixa | Alterar ficha técnica, custos fixos, apagar histórico |
| **Gestor / Proprietário** | Web (qualquer dispositivo) | **Painel de métrica total**, financeiro, relatórios, cadastros, permissões, auditoria | — (acesso total à sua instância) |
| **Cliente delivery** | App / web pública | Pedir, pagar online, acompanhar entrega | Qualquer área interna |
| **Entregador** | Celular | Ver entregas atribuídas, marcar saída e conclusão | Financeiro ou cadastros |
| **Administrador da plataforma (Replay)** | Painel de plataforma | Criar e configurar instâncias, personalizar marca, monitorar saúde técnica, suporte | Operar o negócio do cliente sem autorização |

> **Nota.** Os perfis de **entregador**, **cliente delivery** e **administrador da plataforma** não foram explicitamente detalhados na descoberta — decorrem das metas de entrega e da diretriz de produto replicável. **Requerem confirmação.**

### 5.2 Contexto de uso

- **Ambiente:** operacional de pizzaria — pressão de tempo, mãos ocupadas, ruído, calor e umidade na cozinha
- **Frequência:** uso contínuo durante todo o expediente
- **Implicação de design:** cozinha e mesa exigem alvos grandes, alto contraste, poucos passos, confirmação visual imediata. O briefing menciona **teclado numérico para a cozinha** — operação por código, sem digitação livre.

> **Pendência.** Não foi registrada necessidade específica de acessibilidade ou baixa familiaridade digital. Verificar com a equipe real antes do desenho de interface.

---

## 6. Arquitetura funcional — os módulos do sistema

### 6.1 Mapa de módulos

```
┌─────────────────────────────────────────────────────────────────┐
│               ECOSSISTEMA — INSTÂNCIA DO ESTABELECIMENTO         │
├─────────────────────────────────────────────────────────────────┤
│  OPERAÇÃO LOCAL (rede interna — funciona sem internet)          │
├──────────────────┬──────────────────┬───────────────────────────┤
│  M1 · MESA/PWA   │  M2 · KDS COZINHA│  M3 · CAIXA / COMANDAS    │
│  QR Code         │  Fila de pedidos │  Mesas abertas            │
│  Cardápio        │  Início/Conclusão│  Consumo por mesa         │
│  Pedido          │  Cronômetro      │  Recebimento              │
│  Status          │  Teclado numérico│  Abertura/Fechamento      │
├──────────────────┴──────────────────┴───────────────────────────┤
│  M4 · SINCRONIZAÇÃO (fila local → servidor remoto)              │
├─────────────────────────────────────────────────────────────────┤
│  NUVEM (gestão, consolidação e canais externos)                 │
├──────────────────┬──────────────────┬───────────────────────────┤
│  M5 · DELIVERY   │  M6 · ESTOQUE E  │  M7 · FINANCEIRO          │
│  Pedido online   │       FICHA TÉC. │  Salários                 │
│  Pagamento       │  Insumos         │  Insumos                  │
│  Rastreio        │  Entradas/Saídas │  Custos fixos (aluguel,   │
│  Meta: 25 min    │  Consumo por item│   impostos, CMO)          │
│                  │  Perdas/sobras   │  Resultado e margem       │
├──────────────────┼──────────────────┴───────────────────────────┤
│  M8 · ADMIN      │  M9 · RELATÓRIOS                             │
│  Usuários        │  Operacionais, de venda, de custo             │
│  Permissões      │  Exportação                                   │
│  Cadastros       │                                               │
│  Parâmetros      │                                               │
│  Auditoria       │                                               │
├──────────────────┴───────────────────────────────────────────────┤
│  M10 · PAINEL DO DONO — CONTROLE E MÉTRICA TOTAL                │
│  Tempo real · Indicadores · Metas · Alertas · Histórico          │
└──────────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────────┐
│  M11 · PLATAFORMA (transversal, mantida pela Replay)             │
│  Multi-estabelecimento · White-label · Motor de configuração     │
│  Provisionamento de nova instância · Suporte · Observabilidade    │
└──────────────────────────────────────────────────────────────────┘
```

### 6.2 Detalhamento por módulo

#### M1 — Mesa / Pedido no salão (PWA)
- Acesso por **QR Code na mesa** ou pelo **celular do garçom**
- Cardápio digital com categorias, adicionais, observações e meio-a-meio
- Envio do pedido direto para cozinha e caixa, **sem papel**
- Acompanhamento do status pelo cliente e pelo garçom
- Visualização do **consumo acumulado da mesa**
- Chamada de garçom / solicitação de conta
- **Camada web personalizável** (ver seção 8): marca, cores, imagens do cardápio

#### M2 — KDS Cozinha
- Fila de pedidos por ordem de chegada, com destaque por tempo de espera
- **Marcação de início e conclusão** por pedido/item
- **Cronômetro visível** — atende ao *"saber quantos minutos minha pizza está sendo feita"*
- Operação por **teclado numérico**, sem mouse
- Sinalização de item indisponível / falta de insumo
- Separação por praça de produção (forno, montagem, bebidas) — *hipótese, a validar*

#### M3 — Caixa e Comandas
- Painel com todas as mesas e comandas abertas, com valor e tempo
- Fechamento de mesa, divisão de conta, taxa de serviço
- Recebimento por múltiplas formas (dinheiro, cartão, PIX, app)
- Abertura e fechamento de caixa com conferência
- Sangria e suprimento — *hipótese, a validar*

#### M4 — Sincronização local ↔ nuvem
Ver seção 14 (requisito estruturante).

#### M5 — Delivery próprio
- Canal de pedido online com **marca do estabelecimento** (reduz dependência do iFood)
- Pagamento online integrado
- Fluxo: pedido → produção → despacho → entrega
- Meta operacional declarada: **25 minutos**
- Acompanhamento pelo cliente

> **Pendência.** Não foi definido se haverá integração com iFood, roteirização, gestão de entregadores próprios ou terceirizados, nem taxa por região.

#### M6 — Estoque e Ficha Técnica
Responde a uma dor registrada com clareza:

> *"Cada pizza precisa ser cadastrada o quanto é preciso para fazê-la. Hoje há um relatório de quanto sobrou após comprar em quantidade, mas não se sabe quanto é necessário. Não se sabe quais foram as entradas e precisa controlar."*

- **Ficha técnica por produto** — consumo de cada insumo
- **Cadastro de insumos** com unidade, custo e fornecedor
- **Registro de entradas** (compras/notas) — hoje inexistente
- **Baixa automática** a cada venda, via ficha técnica
- Comparação **consumo teórico × sobra real** → perda e desvio
- Alerta de estoque mínimo
- **Custo de produção por produto**, alimentando a margem

#### M7 — Financeiro
> *"Quero uma gestão financeira (salários de funcionários, custos com insumos e custo com CMO — aluguel, imposto)"*

- Receitas por canal (salão, delivery próprio, iFood)
- **Custo com insumos** (alimentado pelo M6)
- **Folha de pagamento**
- **Custos fixos**: aluguel, impostos, energia, demais despesas
- Resultado, margem por produto, **saúde financeira**
- Fluxo de caixa

#### M8 — Administração
> *"Usuários, permissões, cadastros, solicitações, relatórios, auditoria e afins."*

- Usuários e perfis de acesso
- Cadastros: produtos, insumos, fichas técnicas, mesas, formas de pagamento
- Parâmetros operacionais (horários, taxas, metas de tempo)
- **Trilha de auditoria**

#### M9 — Relatórios
Relatórios detalhados e exportáveis, complementares ao painel: fechamento de caixa, vendas por período, movimentação de estoque, folha, extrato financeiro.

#### M10 — Painel do dono
Ver seção 7 — é o núcleo da diretriz de **controle e métrica total**.

#### M11 — Plataforma
Ver seção 8 — é o núcleo da diretriz de **produto customizável**.

---

## 7. Controle e métrica total — o painel do dono

> **Diretriz.** O foco é o ecossistema da pizzaria e **tudo que o dono precisa para ter controle e métrica total**. A camada de medição não é um relatório no fim do mês: é o produto.

### 7.1 Princípio de instrumentação

Todo evento operacional grava, obrigatoriamente: **o que aconteceu, quando, quem fez, em qual dispositivo, e a qual pedido/mesa/produto se refere.** Sem essa disciplina, nenhum indicador é confiável. A medição é consequência automática da operação — o usuário nunca "preenche" um indicador.

### 7.2 As quatro perguntas do dono

O painel se organiza para responder quatro perguntas, nesta ordem:

| # | Pergunta | Camada |
|---|---|---|
| 1 | **O que está acontecendo agora?** | Tempo real |
| 2 | **Como foi o dia / a semana / o mês?** | Desempenho |
| 3 | **Estou ganhando dinheiro? Em quê?** | Resultado |
| 4 | **O que está fora do lugar?** | Alerta gerencial |

### 7.3 Painel 1 — Tempo real (o pulso da operação)

Visão de "sala de controle", acessível de qualquer lugar, inclusive fora da loja.

- Mesas abertas, valor e tempo de cada uma
- Pedidos em produção, com **cronômetro por pedido** e destaque de atraso
- Fila da cozinha e tempo médio da última hora
- Entregas em rota e tempo decorrido
- Vendas do dia acumuladas, por canal
- Caixa: valor em aberto e status
- Pessoas em turno

### 7.4 Painel 2 — Desempenho operacional

**Indicadores de tempo** (a dor mais explícita do cliente):

| Indicador | Definição |
|---|---|
| Tempo pedido → início de produção | Quanto o pedido espera na fila |
| Tempo de produção | Início → conclusão na cozinha |
| Tempo conclusão → entrega na mesa | Eficiência do salão |
| **Tempo total do pedido (salão)** | Meta declarada: **10 minutos** |
| Tempo despacho → entrega | Eficiência da rota |
| **Tempo total do delivery** | Meta declarada: **25 minutos** |
| Tempo médio por produto | Quais itens travam a cozinha |
| Tempo por faixa de horário | Onde está o gargalo do pico |

**Indicadores de venda:**

- Faturamento por dia, semana, mês, com comparativo do período anterior
- Venda por canal (salão, delivery próprio, iFood)
- Venda por produto e por categoria — curva ABC
- Ticket médio geral, por canal e por mesa
- Pedidos por hora e por dia da semana (mapa de calor)
- Giro de mesa e taxa de ocupação
- Itens mais e menos vendidos
- Cancelamentos e motivos

**Indicadores de pessoas:**

- Pedidos atendidos por garçom
- Ticket médio por garçom
- Produção por operador de cozinha
- Entregas por entregador e tempo médio
- Custo de pessoal sobre faturamento

### 7.5 Painel 3 — Resultado e custo

Esta é a camada que hoje **não existe de nenhuma forma** e que responde ao *"saber como está a saúde financeira"*.

| Indicador | O que revela |
|---|---|
| **CMV — custo da mercadoria vendida** | Quanto do faturamento vira insumo |
| **Custo por produto** (via ficha técnica) | Quanto custa cada pizza |
| **Margem de contribuição por produto** | Quais produtos realmente dão lucro |
| **Curva de rentabilidade** | Cruzamento volume × margem: o que vende muito e rende pouco |
| **Perda e desvio** | Consumo teórico × sobra real |
| Custo fixo mensal | Aluguel, impostos, CMO |
| Custo de pessoal | Folha, encargos |
| **Ponto de equilíbrio** | Quanto precisa vender para não ter prejuízo |
| **Resultado do período** | Lucro ou prejuízo, com composição |
| Fluxo de caixa | Entradas e saídas projetadas |
| Faturamento por m² / por mesa | Eficiência do espaço |

### 7.6 Painel 4 — Alertas gerenciais

O dono não precisa procurar problema: o sistema aponta.

- Pedido parado além do tempo-limite
- Tempo médio de produção acima da meta na última hora
- Insumo abaixo do estoque mínimo
- Divergência relevante entre consumo teórico e real (indício de perda ou desvio)
- Produto vendendo com margem negativa
- Faturamento do dia abaixo da média do mesmo dia da semana
- Cancelamentos ou descontos acima do padrão
- Caixa com diferença no fechamento
- Queda de sincronização com a nuvem

### 7.7 Metas e acompanhamento

O dono define metas (tempo de produção, faturamento diário, CMV alvo, ticket médio) e o sistema acompanha o realizado contra a meta, com sinalização visual e histórico de evolução.

### 7.8 Diretrizes de desenho do painel

- **Acessível do celular** — o dono precisa acompanhar de fora da loja
- **Do resumo ao detalhe** — todo número permite abrir e chegar ao pedido individual
- **Comparativo sempre presente** — número solto não gera decisão; número contra período anterior ou meta, sim
- **Exportação** de qualquer visão (planilha/PDF), inclusive para o contador
- **Configurável por estabelecimento** — cada negócio escolhe seus indicadores prioritários

---

## 8. Produto customizável — multi-estabelecimento e white-label

> **Diretriz.** O sistema deve ser customizável: implantável em **qualquer estabelecimento com os mesmos problemas e necessidades**, com **toda a camada web personalizada**.

Isso muda a natureza do projeto: não é um software sob medida, é um **produto com implantações**. A decisão precisa valer desde a primeira linha de código — transformar um sistema single-tenant em multi-tenant depois é caro e arriscado.

### 8.1 Modelo de arquitetura

```
                    ┌────────────────────────────┐
                    │   PLATAFORMA (Replay)      │
                    │   Código único · Evolução  │
                    │   Motor de configuração    │
                    │   Motor de personalização  │
                    └────────────┬───────────────┘
                                 │ provisiona
        ┌────────────────┬───────┴────────┬────────────────┐
        ▼                ▼                ▼                ▼
 ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
 │ Instância   │  │ Instância   │  │ Instância   │  │ Instância   │
 │ Dona        │  │ Hamburgueria│  │ Restaurante │  │    ...      │
 │ Betinha     │  │             │  │             │  │             │
 │ marca,      │  │ marca,      │  │ marca,      │  │             │
 │ domínio,    │  │ domínio,    │  │ domínio,    │  │             │
 │ cardápio,   │  │ cardápio,   │  │ cardápio,   │  │             │
 │ regras,     │  │ regras,     │  │ regras,     │  │             │
 │ dados       │  │ dados       │  │ dados       │  │             │
 └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘
        │                 │                │
        └── cada uma com seu servidor local próprio na loja ──┘
```

**Isolamento de dados é inegociável.** Cada estabelecimento enxerga exclusivamente os próprios dados. Nenhuma consulta, relatório ou erro pode cruzar instâncias.

### 8.2 O que é personalizável (camada web)

| Dimensão | Itens |
|---|---|
| **Identidade visual** | Logo, cores primária/secundária, tipografia, favicon, imagem de fundo, ícone do PWA |
| **Domínio** | Domínio ou subdomínio próprio do estabelecimento |
| **Conteúdo público** | Nome, descrição, endereço, horário, redes sociais, formas de pagamento aceitas |
| **Cardápio** | Categorias, produtos, fotos, descrições, adicionais, preços |
| **Textos** | Mensagens de boas-vindas, confirmação, agradecimento, termos, política |
| **PWA** | Nome do app, ícone, splash screen, cor de tema |
| **Comunicação** | Modelos de notificação e de mensagem ao cliente |
| **QR Code da mesa** | Arte personalizada com a marca |

### 8.3 O que é configurável (operação)

| Dimensão | Exemplos |
|---|---|
| **Estrutura física** | Quantidade e nomes de mesas, ambientes, praças de produção |
| **Fluxo** | Pedido pelo cliente e/ou só pelo garçom; com ou sem KDS; com ou sem delivery |
| **Regras** | Taxa de serviço, desconto máximo, quem autoriza cancelamento, tempo-limite de alerta |
| **Perfis** | Papéis e permissões por estabelecimento |
| **Pagamento** | Quais meios e quais integrações estão ativos |
| **Metas e indicadores** | Metas de tempo, CMV alvo, indicadores prioritários no painel |
| **Fiscal e tributário** | Regime, alíquotas, emissão de documento |
| **Idioma e moeda** | *Preparado, mas fora do escopo inicial* |

### 8.4 O que é comum e imutável (o núcleo do produto)

- Modelo de pedido, comanda e mesa
- Ciclo de vida do pedido e seus carimbos de tempo
- Motor de ficha técnica e baixa de estoque
- Motor de custo e margem
- Motor de sincronização local ↔ nuvem
- Trilha de auditoria
- Motor de indicadores

> **Regra de governança de produto.** Toda solicitação de cliente é avaliada em três respostas: *(a)* já é configurável, *(b)* vira configuração nova do produto (beneficia todos), ou *(c)* é exceção que não entra. **Customização por código para um cliente específico deve ser evitada** — é o que mata a escalabilidade de produtos deste tipo.

### 8.5 Implantação de um novo estabelecimento

Fluxo padronizado que a Replay deve conseguir executar sem desenvolvimento:

1. Criação da instância e do domínio
2. Aplicação da identidade visual (marca, cores, ícones)
3. Carga do cardápio e das fichas técnicas
4. Configuração de mesas, perfis e regras
5. Instalação e configuração do servidor local + rede
6. Configuração de meios de pagamento
7. Treinamento da equipe
8. Piloto acompanhado
9. Ativação

> Quanto mais desta lista for autoatendimento assistido, mais barata e replicável fica a operação da Replay.

### 8.6 Implicações comerciais

- Abre modelo de **receita recorrente (assinatura)** em vez de projeto único
- Permite **planos por porte ou por módulo** (só operação / operação + gestão / completo)
- Exige **suporte, atualização e observabilidade** como parte do produto — custo contínuo
- Cria ativo de propriedade intelectual da Replay

> **Pendência crítica.** É necessário definir com clareza a **propriedade do produto** e o **modelo comercial**: a Dona Betinha é cliente de um produto da Replay, sócia do produto, ou proprietária do sistema? Essa definição precede a proposta e tem impacto contratual direto.

---

## 9. Fluxos operacionais principais

### 9.1 Fluxo do salão

| # | Etapa | Ator | Sistema | Métrica gerada |
|---|---|---|---|---|
| 1 | Cliente senta e lê QR Code (ou chama garçom) | Cliente / Garçom | Abre mesa/comanda | Horário de abertura |
| 2 | Escolha dos itens no cardápio digital | Cliente / Garçom | Monta pedido | Tempo de decisão |
| 3 | Confirmação do pedido | Cliente / Garçom | **Envia à Cozinha e ao Caixa** + alerta | Timestamp do pedido |
| 4 | Cozinha marca **início** | Cozinha (KDS) | Inicia cronômetro, alerta mesa | Tempo de espera na fila |
| 5 | Cozinha marca **conclusão** | Cozinha (KDS) | Registra tempo, alerta garçom e mesa | **Tempo de produção** |
| 6 | Entrega na mesa | Garçom | Item marcado como entregue | Tempo de salão |
| 7 | Cliente solicita a conta | Cliente / Garçom | Alerta ao caixa | Tempo de permanência |
| 8 | Caixa fecha e recebe | Caixa | Baixa de estoque, registro financeiro, fecha comanda | Ticket, margem, giro de mesa |

**Meta declarada:** *"Pedido está na mesa em 10 minutos."*

### 9.2 Fluxo do delivery

| # | Etapa | Ator | Sistema | Métrica gerada |
|---|---|---|---|---|
| 1 | Cliente monta pedido no app/site | Cliente | Carrinho | Taxa de conversão |
| 2 | Pagamento online (ou na entrega) | Cliente | Confirmação | Tempo de checkout |
| 3 | Pedido entra na produção | Sistema | Envia ao KDS + alerta | Timestamp |
| 4 | Cozinha inicia e conclui | Cozinha | Cronômetro | Tempo de produção |
| 5 | Despacho ao entregador | Operação | Registra saída | Tempo de espera para despacho |
| 6 | Entrega concluída | Entregador | Fecha pedido, alerta cliente | **Tempo total (meta: 25 min)** |

### 9.3 Fluxo de reposição e custo (retaguarda)

Compra de insumo → registro de entrada → estoque atualizado → venda do produto → baixa via ficha técnica → custo apurado → margem calculada → resultado financeiro → **indicador no painel do dono**.

> Este fluxo **hoje não existe de forma alguma** e é o que sustenta toda a resposta à pergunta *"como está a saúde financeira?"*.

---

## 10. Regras de negócio e lógica do sistema

### 10.1 Regras confirmadas na descoberta

| # | Regra | Origem |
|---|---|---|
| R1 | O pedido pode ser originado por **PWA na mesa (QR)** ou **celular do garçom** | Item 7.2 |
| R2 | Todo pedido confirmado chega **simultaneamente ao caixa e à cozinha** | Item 7.2 |
| R3 | A cozinha **registra início e conclusão** de cada pedido | Item 7.2 |
| R4 | **Cada etapa gera alerta** para o usuário envolvido | Itens 5.3 e 7.3 |
| R5 | O sistema **registra quem fez cada ação** e mantém histórico | Item 7.4 |
| R6 | A operação local **funciona sem internet** e sincroniza depois | Item 8.4 |
| R7 | Cada produto tem **ficha técnica** com consumo de insumos | Item 6.1 |

### 10.2 Regras derivadas das diretrizes

| # | Regra | Origem |
|---|---|---|
| R8 | Nenhuma etapa ocorre sem carimbo de tempo, autor e origem | Diretriz de métrica total |
| R9 | Todo indicador do painel deve permitir navegação até o evento de origem | Diretriz de métrica total |
| R10 | Nenhum dado de um estabelecimento é visível a outro | Diretriz de produto customizável |
| R11 | Toda regra específica de negócio deve existir como configuração, não como código | Diretriz de produto customizável |

### 10.3 Regras a definir (pendentes)

- Quem pode **cancelar** item já em produção e o que acontece com o insumo consumido
- Regras de **desconto, cortesia e taxa de serviço** — quem autoriza
- Comportamento quando um **insumo acaba** no expediente (bloqueia? avisa?)
- **Divisão de conta** e transferência de itens entre mesas
- Política de **estorno / cancelamento de pagamento**
- Tratamento de **pedido duplicado** ou reenviado
- Tempo-limite que dispara alerta de atraso por tipo de produto
- Regra de **precificação diferenciada** ("preço de Ceasa / fora da Ceasa")

---

## 11. Dados, cadastros e informação

### 11.1 Entidades principais

| Entidade | Descrição | Observação |
|---|---|---|
| **Estabelecimento (tenant)** | Instância do produto | Base do isolamento de dados |
| **Configuração / Marca** | Identidade visual, domínio, parâmetros, textos | Personalização web |
| **Produto** | Pizzas, bebidas, adicionais | Preço por canal |
| **Ficha técnica** | Insumos e quantidades por produto | Base do custo e da baixa |
| **Insumo** | Matéria-prima, unidade, custo, fornecedor | |
| **Entrada de estoque** | Compras e recebimentos | **Hoje sem registro** |
| **Mesa / Comanda** | Sessão de consumo | Aberta → em consumo → fechada |
| **Pedido** | Itens, status e timestamps | Núcleo da medição |
| **Item do pedido** | Produto, quantidade, observações, status | |
| **Evento operacional** | Cada transição de status com autor e horário | Base de todos os indicadores |
| **Pagamento** | Forma, valor, canal, comprovante | |
| **Usuário** | Perfil, permissões, credenciais | Vinculado ao estabelecimento |
| **Log de auditoria** | Ação, autor, timestamp, valor anterior/novo | Exigência confirmada |
| **Lançamento financeiro** | Receita ou despesa, categoria, competência | Salário, insumo, aluguel, imposto |
| **Meta** | Indicador, valor alvo, período | Painel do dono |

### 11.2 Situação dos dados hoje

> *"Não existe um sistema que gerencia os dados da pizzaria, somente a parte da contabilidade que fica com um contador."*

**Consequência:** não há base legada para migrar. A carga inicial será **cadastro manual assistido** — sobretudo produtos, fichas técnicas e insumos, a tarefa mais trabalhosa do onboarding e um item que deve ser padronizado e otimizado, porque **se repetirá em cada novo estabelecimento**.

> **Pendência.** Confirmar se existe planilha, caderno ou lista de produtos/preços que acelere a carga inicial, e se há histórico de vendas do cardápio web/iFood que valha importar.

---

## 12. Integrações e pagamentos

### 12.1 Situação atual

> *"Atualmente não há sistema algum sendo utilizado, a não ser o da maquineta da Cielo com a conta do Banco do Brasil e a maquineta do Mercado Pago."*

### 12.2 Integrações pretendidas

| Integração | Finalidade | Status |
|---|---|---|
| **Mercado Pago** | Pagamento direto pelo aplicativo | Citado — validar credenciais e modalidade |
| **Cielo / Banco do Brasil** | Cartão via maquininha | Citado — definir se será integração real (TEF/Pinpad) ou registro manual da forma |
| **PIX** | Recebimento instantâneo | *Hipótese* — natural ao cenário, não citado |
| **iFood** | Canal já em uso | *Pendência* — integração não definida |
| **Emissão fiscal (NFC-e / SAT)** | Obrigação legal de venda | **Pendência crítica** — não mencionado |
| **Contador** | Repasse contábil | Hoje manual; avaliar exportação |

> **Alerta.** A **emissão de documento fiscal** não foi abordada e é exigência legal no varejo alimentar. Precisa ser esclarecida antes da proposta — altera escopo, custo e prazo de forma significativa. Em um produto multi-estabelecimento, também precisa ser **configurável por regime tributário**.

> **Alerta.** Integração de maquininha (TEF) e integração de gateway online são coisas técnicas distintas, com custos diferentes. O cliente citou ambas — confirmar o comportamento esperado em cada canal.

---

## 13. Canais, dispositivos e experiência

> *"Celular, tablet, computador, KDS, PWA, tudo responsivo."*

| Canal | Uso | Perfil |
|---|---|---|
| **PWA no celular do cliente** | Pedido na mesa via QR Code | Cliente do salão |
| **PWA no celular do garçom** | Lançamento de pedido | Garçom |
| **Tablet dedicado** | KDS ou apoio de salão | Cozinha |
| **Terminal / desktop** | Caixa | Caixa |
| **Tela KDS** | Painel de produção com teclado numérico | Cozinha |
| **Web responsivo** | Painel do dono, gestão e financeiro | Gestor |
| **Web / app público** | Delivery com marca do estabelecimento | Cliente final |
| **Painel de plataforma** | Gestão de instâncias | Replay |

### Diretrizes de experiência

- **Sem instalação obrigatória para o cliente do salão** — o QR Code abre o PWA no navegador
- **Cozinha:** alto contraste, alvos grandes, navegação por teclado numérico, zero digitação livre
- **Caixa:** alta densidade de informação, todas as mesas em uma tela
- **Gestor:** mobilidade — acompanhar a operação de fora da loja
- **Público:** cada estabelecimento com sua identidade, sem marca da Replay em primeiro plano

### Comunicação com o usuário

> *"Inicialmente tudo dentro de push-up do sistema, no celular ou navegador."*

Primeira versão: **notificações no próprio sistema** (in-app / push do navegador). E-mail, SMS e WhatsApp em fases posteriores.

---

## 14. Requisito estruturante: operação offline-first

Requisito que mais impacta arquitetura e custo. Registro literal:

> *"Sim, é necessário funcionar sem internet rodando em rede local as mesas, caixa e KDS | cozinha. Ou seja, tudo roda local, com administrativo e acompanhamento do que está acontecendo via internet. Se internet cair, produção local continua funcionando. Sistema sempre deve publicar no servidor remoto, mas se localmente ficar sem internet, mantém tudo local, e depois sobe para o servidor web."*

### 14.1 Modelo arquitetural derivado

```
      [ Servidor remoto / nuvem — multi-estabelecimento ]
      Painel do dono · Financeiro · Delivery · Configuração · Plataforma
                        ▲
                        │  sincronização contínua
                        │  (fila resiliente, retomada automática)
                        ▼
      [ Servidor local no estabelecimento ]  ← fonte da verdade operacional
                        │
      ┌─────────────────┼─────────────────┐
      ▼                 ▼                 ▼
   Mesas/PWA          KDS Cozinha        Caixa
```

### 14.2 Princípios

1. **A operação nunca para.** A rede local é autossuficiente para pedido, produção e recebimento.
2. **O local é a fonte da verdade operacional** durante a queda de conexão.
3. **A sincronização é contínua e automática**, com fila persistente — nada se perde.
4. **A nuvem é a fonte de verdade consolidada** para gestão, financeiro e indicadores.
5. **Delivery e pagamento online dependem de internet** — degradação parcial esperada e aceitável.
6. **O painel do dono reflete o atraso de sincronização de forma explícita** — nunca apresentar dado defasado como se fosse tempo real.

### 14.3 Implicações a decidir

- Qual **hardware local** ficará na loja (mini-PC, servidor dedicado, roteador isolado) — e como isso se padroniza para replicação
- Política de **resolução de conflito** na sincronização
- **Backup local** e recuperação em caso de falha de equipamento
- Responsável pela **manutenção física** na loja
- Comportamento se o servidor local cair (não só a internet)
- **Monitoramento remoto** da saúde de cada instalação (essencial no modelo multi-estabelecimento)

> Este requisito, isoladamente, adiciona complexidade relevante. Deve ter destaque próprio na proposta comercial.

---

## 15. Notificações e alertas

> *"Alerta para cada usuário envolvido no processo, desde a mesa, caixa, cozinha."*
> *"Deve ter alertas em cada etapa para cada usuário."*

### Matriz de alertas (proposta inicial — a validar)

| Evento | Mesa/Cliente | Garçom | Cozinha | Caixa | Gestor |
|---|:---:|:---:|:---:|:---:|:---:|
| Pedido enviado | ✓ | ✓ | ✓ | ✓ | |
| Produção iniciada | ✓ | ✓ | | | |
| Pedido pronto | ✓ | ✓ | | | |
| Item entregue | ✓ | ✓ | | ✓ | |
| Conta solicitada | | ✓ | | ✓ | |
| Pagamento confirmado | ✓ | ✓ | | ✓ | |
| Pedido atrasado | | ✓ | ✓ | | ✓ |
| Insumo em falta | | ✓ | ✓ | | ✓ |
| Estoque mínimo atingido | | | | | ✓ |
| Caixa aberto/fechado | | | | ✓ | ✓ |
| Meta do dia em risco | | | | | ✓ |
| Margem negativa em produto | | | | | ✓ |
| Divergência de estoque | | | | | ✓ |
| Falha de sincronização | | | | | ✓ |

> Os limiares de cada alerta devem ser **configuráveis por estabelecimento**.

---

## 16. Auditoria, rastreabilidade e administração

Exigência confirmada (item 7.4: **"Sim."**) e reforçada em 5.4.

O sistema deve registrar, de forma imutável:

- **Quem** executou cada ação
- **Quando** (timestamp)
- **O que mudou** (valor anterior e novo)
- **De onde** (dispositivo/terminal)
- **Em qual estabelecimento**

Aplicável, no mínimo, a: cancelamentos, descontos, alterações de preço, movimentações de estoque, ajustes financeiros, abertura/fechamento de caixa e alterações de permissão.

> A trilha de auditoria também é o insumo de vários indicadores de gestão (cancelamentos, descontos, desvios) — é infraestrutura de confiança, não burocracia.

---

## 17. Priorização — MVP e evolução por fases

> **Atenção.** O bloco de priorização do briefing (seção 09) **não foi preenchido na reunião**. A proposta abaixo é **interpretação da Replay** e **precisa ser validada** antes de virar escopo.

### Fase 0 — Fundação de plataforma *(transversal, feita junto com a Fase 1)*
- Estrutura multi-estabelecimento e isolamento de dados
- Motor de configuração e de personalização visual
- Autenticação, perfis e auditoria
- Motor de eventos e instrumentação (base de todos os indicadores)

> Não é uma fase entregável ao cliente final, mas **não pode ser adiada** — é o que torna o produto replicável.

### Fase 1 — MVP: "o pedido chega na cozinha e o dono enxerga"
Resolve a dor mais explícita (*"o pedido é feito e não chega para cozinha"*) já entregando a primeira camada de métrica.

- Cadastro de produtos e cardápio
- Pedido na mesa via QR Code e via celular do garçom (PWA)
- KDS de cozinha com início/conclusão e cronômetro
- Caixa com mesas/comandas, fechamento e recebimento
- Alertas in-app entre mesa, cozinha e caixa
- Usuários, perfis e permissões
- **Operação em rede local com sincronização para a nuvem**
- **Painel do dono v1:** tempo real, tempos por etapa, vendas do dia, ticket médio

### Fase 2 — Custo e controle
- Cadastro de insumos e **ficha técnica**
- Entradas de estoque e baixa automática por venda
- Custo de produção e margem por produto
- Consumo teórico × sobra real (perda e desvio)
- **Painel do dono v2:** CMV, margem por produto, curva de rentabilidade, alertas de estoque

### Fase 3 — Financeiro de gestão
- Folha de pagamento
- Custos fixos (aluguel, impostos, CMO)
- Fluxo de caixa e resultado
- **Painel do dono v3:** ponto de equilíbrio, resultado do período, saúde financeira

### Fase 4 — Delivery próprio
- Canal de pedido online com marca do estabelecimento
- Pagamento online (Mercado Pago)
- Despacho, entregador e rastreio
- Medição da meta de 25 minutos

### Fase 5 — Produto replicável em escala
- Painel de plataforma e provisionamento assistido de novas instâncias
- Personalização por autoatendimento (marca, cores, domínio, textos)
- Monitoramento remoto das instalações
- Planos, cobrança e onboarding padronizado

### Fase 6 — Expansão
- **App de frios** (Pizzaria, Hamburgueria, Condomínios) e tabelas de preço diferenciadas
- Integração com iFood
- Notificações por WhatsApp/SMS
- Programa de fidelidade e CRM de clientes

---

## 18. Critérios de sucesso

| Indicador | Situação hoje | Meta declarada |
|---|---|---|
| Pedido chega à cozinha | Falha recorrente | 100% dos pedidos, instantaneamente |
| Tempo pedido → mesa | Desconhecido | **10 minutos** |
| Tempo pedido → entrega delivery | Desconhecido | **25 minutos** |
| Uso de papel na operação | Presente | Zero |
| Visibilidade do tempo de produção | Inexistente | Cronômetro por pedido |
| Consumo de insumo por produto | Desconhecido | Ficha técnica em 100% dos produtos |
| Custo e margem por produto | Desconhecido | Apurados automaticamente |
| Visão da saúde financeira | Inexistente | Painel com receita, custo, margem e resultado |
| Decisão do dono | Por intuição | Por indicador |
| Continuidade sem internet | N/A | Operação local ininterrupta |
| Tempo de implantação de um novo estabelecimento | N/A | *Definir meta — indicador-chave do produto* |

> **Pendência.** Confirmar se as metas de 10 e 25 minutos são **objetivo de negócio** (o sistema mede e apoia) ou **requisito do sistema** (o sistema garante). São compromissos contratuais muito diferentes.

---

## 19. Riscos e pontos de atenção

| # | Risco | Impacto | Mitigação proposta |
|---|---|---|---|
| 1 | **Escopo muito amplo** — POS, KDS, delivery, estoque, financeiro, painel gerencial, plataforma multi-tenant e app de frios | Alto | Faseamento explícito e assinado; MVP restrito ao fluxo salão→cozinha→caixa + painel v1 |
| 2 | **Cliente não possui processo definido** | Alto | A Replay desenha o processo; validar em workshop antes do desenvolvimento |
| 3 | **Offline-first** eleva custo e complexidade de forma relevante | Alto | Precificar separadamente; padronizar hardware; definir responsável por manutenção |
| 4 | **Emissão fiscal não abordada** | Alto | Esclarecer com cliente e contador antes da proposta; prever configuração por regime |
| 5 | **Multi-tenant decidido depois seria refação completa** | Alto | Fundação de plataforma na Fase 0, junto com o MVP |
| 6 | **Customização por código para cada cliente** destrói a escalabilidade | Alto | Governança de produto: configuração, não código (seção 8.4) |
| 7 | **Propriedade do produto e modelo comercial indefinidos** | Alto | Definir antes da proposta — impacto contratual direto |
| 8 | **Prazo, orçamento e priorização não informados** | Alto | Bloqueio para proposta |
| 9 | **App de frios como segundo produto disfarçado de funcionalidade** | Alto | Separar formalmente desta etapa |
| 10 | **Métrica sem qualidade de dado** — indicador errado é pior que indicador nenhum | Médio-Alto | Instrumentação obrigatória por evento; validação em piloto |
| 11 | **Carga inicial de fichas técnicas** é trabalhosa e depende do cliente | Médio | Definir responsável e prazo; criar processo padronizado de carga |
| 12 | **Integrações de pagamento** (TEF × gateway) indefinidas | Médio | Reunião técnica específica |
| 13 | **Adoção pela equipe** — nunca usaram sistema | Médio | Treinamento, interface simplificada, piloto acompanhado |
| 14 | **Suporte contínuo a várias instalações locais** | Médio | Monitoramento remoto desde a Fase 1; padronização de hardware |
| 15 | **Referências não qualificadas** (Yon San, Vila Frios, McDonald's) | Médio | Sessão de referências antes do desenho de UX |

---

## 20. Pendências e próximos passos

### 20.1 Pendências bloqueantes para a proposta

| # | Pendência | Responsável | Status |
|---|---|---|---|
| 1 | Priorização: os três resultados indispensáveis da primeira entrega | Cliente | Aberto |
| 2 | Prazo desejado e prazo obrigatório (com a razão) | Cliente | Aberto |
| 3 | Faixa de investimento e modelo de contratação | Cliente | Aberto |
| 4 | O que **não** entra nesta primeira etapa | Cliente + Replay | Aberto |
| 5 | Necessidade de emissão fiscal (NFC-e/SAT) | Cliente + Contador | Aberto |
| 6 | **Propriedade do produto e modelo comercial** (cliente, sócio ou proprietário) | Cliente + Replay | Aberto |
| 7 | App de frios: módulo posterior ou produto separado? | Cliente | Aberto |
| 8 | Modalidade de integração de pagamento por canal | Cliente + Replay | Aberto |
| 9 | Quais indicadores são prioritários para o dono na v1 do painel | Cliente | Aberto |

### 20.2 Materiais a solicitar ao cliente

- [ ] Cardápio completo com preços (salão e delivery)
- [ ] Lista de insumos e fornecedores
- [ ] Relatório de sobras/compras mencionado na reunião
- [ ] Prints do cardápio web atual e do painel iFood
- [ ] Planta ou quantidade de mesas
- [ ] Lista de funcionários e funções
- [ ] Identidade visual da marca (logo, cores, fontes)
- [ ] Contato do contador
- [ ] Faturas/extratos Cielo e Mercado Pago (volume e modalidade)
- [ ] Informação sobre infraestrutura de rede e internet da loja

### 20.3 Próximos passos recomendados

1. **Validar este documento com o cliente** — leitura conjunta e correção de interpretações
2. **Reunião de priorização** — fechar as pendências bloqueantes (20.1)
3. **Definição de produto** — propriedade, modelo comercial e limites da customização
4. **Workshop de desenho de processo** — como não existe processo atual, ele precisa ser construído e aprovado
5. **Workshop de indicadores** — definir com o dono quais métricas ele realmente usará para decidir
6. **Descoberta técnica** — infraestrutura local, rede, hardware, integrações e arquitetura multi-tenant
7. **Protótipo navegável** — mesa, KDS, caixa e painel do dono
8. **Proposta comercial e escopo formal** por fases

---

## 21. Anexo — Separação entre fato, hipótese e pendência

Conforme a metodologia do briefing (seção 12): *"Uma necessidade mencionada não é automaticamente requisito aprovado. Uma preferência não é regra de negócio. Uma hipótese não deve ser apresentada como fato."*

### Fatos confirmados (podem sustentar a proposta)

- A pizzaria não possui sistema algum além do cardápio web e do iFood
- Pedidos se perdem entre salão e cozinha
- Não há controle de estoque, consumo de insumos nem custo por produto
- Não há processo estruturado definido
- Usuários: cozinha, mesa, garçom, caixa
- Pagamentos atuais: maquininha Cielo (BB) e maquininha Mercado Pago
- É exigido registro de autoria e histórico de alterações
- A operação precisa funcionar sem internet, em rede local, sincronizando depois
- É exigido alerta em cada etapa para cada usuário envolvido
- A gestão financeira deve cobrir salários, insumos e custos fixos (aluguel, imposto, CMO)
- **O foco é o ecossistema completo com controle e métrica total para o dono** *(diretriz)*
- **O sistema deve ser customizável e implantável em qualquer estabelecimento com as mesmas dores, com toda a camada web personalizada** *(diretriz)*

### Hipóteses (precisam ser validadas)

- Faseamento proposto (Fundação → MVP → custo → financeiro → delivery → plataforma → expansão)
- Conjunto de indicadores proposto para o painel do dono
- App de frios como módulo posterior da plataforma
- Perfis de entregador, cliente delivery e administrador de plataforma
- Separação da cozinha por praças de produção
- Necessidade de PIX, sangria/suprimento e divisão de conta
- Metas de 10 e 25 minutos como objetivo de negócio (e não garantia do sistema)
- Precificação diferenciada ligada ao app de frios
- Modelo de receita recorrente por assinatura

### Pendências (exigem informação ou decisão)

- Prioridades, prazo e orçamento
- Propriedade do produto e modelo comercial
- Emissão fiscal e configuração por regime tributário
- Integração com iFood
- Modalidade técnica das integrações de pagamento
- Regras de cancelamento, desconto, estorno e exceções operacionais
- Indicadores prioritários da primeira versão do painel
- Referências visuais e o que agrada em cada uma
- Dados históricos a importar
- Infraestrutura, hardware local e padrão de instalação replicável

---

*Documento gerado a partir de `Assets/Briefing-Pedido-Inicial.docx` (RS-BRF-MTG-001), acrescido das diretrizes de ecossistema/métrica total e de produto customizável. Replay Studio — Projeto 004_DonaBetinha.*
