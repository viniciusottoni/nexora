# Otimização de Processos, Métricas e Experiência por Usuário
## Documento técnico de operação — Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha — Ecossistema de Gestão e Operação |
| **Documento** | Otimização de processos, métricas e experiência por usuário |
| **Complementa** | `Visao-Geral-Sistema-Dona-Betinha.md` (v1.1) |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Responsável Replay** | Sáskia |
| **Status** | Proposta técnica de operação — pendente de validação |

> **Propósito**
> O documento de visão define **o que** o sistema é. Este define **como ele deve se comportar para que a operação fique de fato mais rápida, mais barata e mais previsível** — usuário por usuário, e no fluxo interno do próprio sistema. Cada recomendação parte de uma dor registrada na descoberta ou de um princípio consolidado de operação de restaurante.

---

## Sumário

**Parte I — Fundamentos**
1. [Diagnóstico: onde uma pizzaria realmente perde](#1-diagnóstico-onde-uma-pizzaria-realmente-perde)
2. [Os cinco princípios que orientam todas as decisões](#2-os-cinco-princípios-que-orientam-todas-as-decisões)
3. [O gargalo: por que tudo gira em torno do forno](#3-o-gargalo-por-que-tudo-gira-em-torno-do-forno)

**Parte II — Otimização por usuário**

4. [Cliente do salão](#4-cliente-do-salão)
5. [Garçom](#5-garçom)
6. [Cozinha — montagem, forno e expedição](#6-cozinha--montagem-forno-e-expedição)
7. [Caixa](#7-caixa)
8. [Entregador](#8-entregador)
9. [Cliente de delivery](#9-cliente-de-delivery)
10. [Estoquista / comprador](#10-estoquista--comprador)
11. [Gestor / dono](#11-gestor--dono)
12. [Administrador da plataforma (Replay)](#12-administrador-da-plataforma-replay)

**Parte III — Otimização do fluxo do próprio sistema**

13. [O modelo de eventos: a espinha dorsal](#13-o-modelo-de-eventos-a-espinha-dorsal)
14. [Inteligência de fluxo: sequenciamento, sincronização e previsão](#14-inteligência-de-fluxo-sequenciamento-sincronização-e-previsão)
15. [Automações que eliminam trabalho humano](#15-automações-que-eliminam-trabalho-humano)
16. [Comportamento em degradação (offline e falhas)](#16-comportamento-em-degradação-offline-e-falhas)
17. [Métricas de saúde do próprio sistema](#17-métricas-de-saúde-do-próprio-sistema)

**Parte IV — Consolidação**

18. [Dicionário de métricas](#18-dicionário-de-métricas)
19. [Matriz de alertas por usuário e limiar](#19-matriz-de-alertas-por-usuário-e-limiar)
20. [Anti-padrões: o que não fazer](#20-anti-padrões-o-que-não-fazer)
21. [Priorização: ganho rápido × esforço](#21-priorização-ganho-rápido--esforço)

---

# PARTE I — FUNDAMENTOS

## 1. Diagnóstico: onde uma pizzaria realmente perde

A dor declarada foi *"o pedido é feito e não chega para cozinha"*. Essa é a ponta visível. Abaixo dela existem seis perdas estruturais que uma pizzaria sem sistema acumula — todas presentes no cenário descrito na descoberta.

| # | Perda | Como se manifesta | O que o sistema precisa fazer |
|---|---|---|---|
| **P1** | **Perda de pedido** | Comanda de papel extraviada, pedido verbal esquecido, item não lançado | Pedido nasce digital e nunca depende de transporte físico |
| **P2** | **Perda de tempo (fila invisível)** | Pedido esperando na bancada sem ninguém saber há quanto tempo | Todo pedido tem cronômetro visível desde o segundo zero |
| **P3** | **Perda de sincronia** | Entrada sai 10 minutos antes da pizza; mesa recebe itens picados | Sequenciamento reverso: itens saem juntos (seção 14.2) |
| **P4** | **Perda de insumo** | Não se sabe quanto foi usado, quanto sobrou, quanto foi desperdiçado | Ficha técnica + baixa automática + contagem cíclica |
| **P5** | **Perda de margem** | Vende-se muito o produto que dá pouco lucro, sem que ninguém perceba | Engenharia de cardápio automática (seção 11.4) |
| **P6** | **Perda de receita** | Mesa ociosa, ticket baixo, cliente que não volta | Giro de mesa, sugestão de venda contextual, histórico de cliente |

> **Consequência de projeto:** resolver apenas P1 entrega talvez 25% do valor. O sistema só cumpre o pedido *"controle e métrica total"* se atacar as seis.

### 1.1 As perguntas que o dono não consegue responder hoje

Cada pergunta abaixo vira, neste documento, um requisito de instrumentação:

- Quanto tempo leva uma pizza, de verdade, no meu pior horário?
- Qual é a minha capacidade máxima de pizzas por hora?
- Quanto custa cada pizza que eu vendo?
- Quais produtos me dão lucro e quais me dão prejuízo disfarçado de volume?
- Quanto de queijo eu deveria ter usado no mês e quanto eu realmente usei?
- Quantas mesas eu perdi por demora?
- Meu garçom está vendendo ou só anotando?
- Que dia e que hora eu preciso de mais gente — e de menos?

---

## 2. Os cinco princípios que orientam todas as decisões

**P1 · O dado nasce do trabalho, nunca de um formulário.**
Ninguém "preenche indicador". Se um número exige digitação extra de alguém, ele será impreciso ou não existirá. Toda métrica deste documento é subproduto automático de uma ação que já precisa acontecer.

**P2 · Toque mínimo em ambiente de pressão.**
Na cozinha e no salão, cada toque a mais é tempo e erro. A meta é: **um toque para avançar o estado mais comum**, dois toques para exceção. Se um fluxo operacional exige mais de três toques, ele está mal desenhado.

**P3 · A informação vai até a pessoa; a pessoa não vai atrás.**
O garçom não deve "conferir se ficou pronto". O sistema avisa. O dono não deve "procurar problema no relatório". O sistema aponta.

**P4 · Otimize o gargalo, não a média.**
Acelerar o caixa quando o forno está saturado não aumenta uma pizza por hora. Toda decisão de fluxo pergunta antes: *isso alivia o gargalo?* (seção 3).

**P5 · Todo número leva ao evento que o originou.**
Indicador sem rastreabilidade vira desconfiança. Do gráfico de tempo médio até o pedido específico das 20h47 devem existir no máximo três cliques.

---

## 3. O gargalo: por que tudo gira em torno do forno

### 3.1 A restrição da pizzaria

Uma pizzaria tem uma restrição física clara: **o forno**. Ele define o teto de produção — nenhuma melhoria em outro ponto aumenta a capacidade se o forno já está saturado.

```
Capacidade teórica = (pizzas simultâneas no forno × 60) ÷ tempo de cocção (min)

Exemplo: 5 pizzas simultâneas, 7 min de cocção
         (5 × 60) ÷ 7 ≈ 42 pizzas/hora teóricas
         Na prática, com carga/descarga e ociosidade: ~30 a 34 pizzas/hora
```

> **Requisito.** Os parâmetros de capacidade de forno (posições simultâneas, tempo de cocção por tipo de massa) devem ser **configuráveis por estabelecimento** — em uma hamburgueria a restrição é a chapa; em um restaurante, a praça quente. O motor é o mesmo.

### 3.2 As três consequências para o sistema

**a) O forno nunca pode ficar ocioso com fila esperando.**
Se há pedido na fila e posição livre no forno, isso é perda irrecuperável de capacidade. O sistema deve tornar essa situação **visível e alarmante** no KDS.

**b) A montagem não pode formar pilha.**
Pizza montada esperando forno resseca a massa e degrada o produto. O ritmo da montagem deve ser puxado pelo forno, não empurrado pela fila de pedidos. O KDS deve indicar **quando montar**, não apenas o que montar.

**c) Aceitar pedido sem capacidade é prometer atraso.**
Quando a fila projetada ultrapassa o tempo prometido, o sistema deve **alongar automaticamente o prazo informado** ao cliente de delivery (seção 14.4) — em vez de aceitar e frustrar.

### 3.3 Métricas do gargalo

| Métrica | Fórmula / definição | Por que importa |
|---|---|---|
| **Taxa de ocupação do forno** | Tempo com forno em uso ÷ tempo de operação | Mostra se o teto foi atingido ou há folga |
| **Ociosidade com fila** | Minutos com posição livre E pedido esperando | Perda pura — meta: zero |
| **Pizzas por hora (real)** | Pizzas concluídas ÷ horas de operação | Capacidade real, não teórica |
| **Pico sustentado** | Maior produção mantida por 60 min | Base para escala de pessoal e promessa de prazo |
| **Tempo de cocção real × padrão** | Média realizada vs. ficha técnica | Detecta forno frio, sobrecarga ou desvio de processo |

---

# PARTE II — OTIMIZAÇÃO POR USUÁRIO

> Cada perfil é analisado em cinco blocos: **dores**, **facilidades** (o que o sistema entrega para tornar o trabalho mais fácil), **informações** (o que ele precisa ver), **métricas** (o que ele gera e o que é medido dele) e **automações e alertas**.

---

## 4. Cliente do salão

### 4.1 Dores
Espera sem saber quanto falta. Não consegue chamar o garçom. Não sabe quanto já consumiu. Perde tempo esperando a conta. Não entende o cardápio (o que leva cada pizza, o que é meio-a-meio).

### 4.2 Facilidades a entregar

| Facilidade | Detalhe |
|---|---|
| **Acesso em um toque** | QR Code na mesa abre o PWA no navegador — sem instalar, sem cadastro obrigatório |
| **Cardápio que vende** | Foto, descrição, ingredientes, alérgenos, tempo estimado por item |
| **Meio-a-meio bem resolvido** | Fluxo específico de pizzaria: escolher dois sabores, regra de preço clara (maior valor / média — configurável) |
| **Personalização** | Sem cebola, borda recheada, ponto da massa, observação livre |
| **Acompanhamento ao vivo** | Recebido → em produção → no forno → pronto → entregue, com tempo estimado |
| **Consumo da mesa em tempo real** | Quanto já consumiu, por item, sem precisar pedir |
| **Chamar garçom** | Botão que gera alerta direcionado, sem levantar a mão |
| **Pedir a conta** | Solicita fechamento e escolhe a forma de pagamento antecipadamente |
| **Dividir a conta** | Por pessoa ou por item, calculado pelo sistema |
| **Repetir item** | Um toque para pedir mais do mesmo |
| **Avaliação no fim** | Nota rápida ao fechar a conta — insumo de indicador de qualidade |

### 4.3 Informações que ele precisa ver
Status do pedido com tempo estimado; disponibilidade real (item em falta não aparece ou aparece esgotado); total acumulado com taxa de serviço destacada; tempo de espera estimado antes de pedir, nos horários de pico.

### 4.4 Métricas geradas

| Métrica | Uso gerencial |
|---|---|
| Tempo entre abertura da mesa e primeiro pedido | Mede clareza do cardápio e agilidade do atendimento |
| Taxa de abandono do carrinho | Cardápio confuso, preço, ou item indisponível |
| Itens visualizados × pedidos | Quais produtos atraem mas não convertem |
| Uso do botão "chamar garçom" | Frequência alta = falha de cobertura do salão |
| Tempo de permanência na mesa | Base do giro de mesa |
| Nota de satisfação | Correlacionar com tempo de espera |
| Taxa de adesão ao autoatendimento | Quantos pedem pelo QR × pelo garçom |

### 4.5 Automações e alertas
Sugestão contextual de complemento (bebida, borda, sobremesa) baseada no que está no carrinho; aviso automático quando a pizza entra no forno; alerta ao garçom se a mesa está aberta há X minutos sem nenhum pedido; oferta de sobremesa disparada quando o prato principal é marcado como entregue.

---

## 5. Garçom

### 5.1 Dores
Anota no papel e reescreve. Anda até a cozinha para lançar e para conferir se ficou pronto. Esquece item. Não sabe o que está em falta. Perde tempo montando conta. Não sabe qual mesa precisa de atenção.

### 5.2 Facilidades a entregar

| Facilidade | Detalhe |
|---|---|
| **Lançamento no próprio celular** | Zero deslocamento até a cozinha ou o caixa |
| **Mapa de mesas em uma tela** | Livre / ocupada / aguardando / pronta para entregar / conta pedida — com cor e tempo |
| **Cardápio com busca rápida e favoritos** | Os 20 itens mais vendidos em atalho |
| **Indisponibilidade em tempo real** | Item sem estoque some da lista — evita vender o que não existe |
| **Modificadores em dois toques** | Sem cebola, bem assada, borda — pré-configurados |
| **Aviso de "pronto"** | Notificação direcionada só ao garçom da mesa |
| **Transferência de mesa e de item** | Cliente mudou de lugar, item foi para a mesa errada |
| **Fechamento assistido** | Conta pronta, divisão calculada, taxa aplicada conforme regra |
| **Modo offline transparente** | Continua lançando na rede local mesmo sem internet |
| **Sugestão de venda no momento certo** | O sistema sugere o complemento ao lançar o item |

### 5.3 Informações que ele precisa ver
Tempo de cada mesa desde a abertura e desde o último pedido; quais pratos já saíram e quais faltam por mesa; itens prontos aguardando retirada (com tempo na janela de expedição — pizza esfriando é falha grave); estimativa atual da cozinha para informar o cliente com honestidade; seu próprio desempenho do turno.

### 5.4 Métricas geradas e de desempenho

| Métrica | O que revela |
|---|---|
| **Mesas atendidas no turno** | Carga de trabalho e dimensionamento |
| **Ticket médio por garçom** | Capacidade de venda — não só de anotar |
| **Taxa de upsell aceito** | Efetividade da sugestão de venda |
| **Tempo pronto → entregue** | Comida esperando na janela; qualidade percebida |
| **Itens cancelados após lançamento** | Erro de lançamento ou falha de comunicação |
| **Tempo de resposta ao chamado da mesa** | Qualidade de atendimento |
| **Nota média das mesas atendidas** | Satisfação atribuída |
| **Mesas simultâneas** | Ponto de saturação individual |

> **Cuidado de gestão.** Métricas individuais devem ser usadas para **treinar e dimensionar**, não para punir. Ranking exposto sem contexto (mesa de 2 pessoas × mesa de 8) gera comportamento defensivo e sabotagem de dado. Recomenda-se visibilidade individual para o próprio garçom e agregada para a gestão.

### 5.5 Automações e alertas
Alerta de item pronto há mais de 2 minutos na janela; alerta de mesa aberta há mais de X minutos sem pedido; alerta de mesa que pediu a conta e ainda não foi atendida; sugestão automática de sobremesa/café quando o prato principal é entregue; aviso de mesa que ultrapassou o tempo médio de permanência (útil em fila de espera).

---

## 6. Cozinha — montagem, forno e expedição

Este é o coração do sistema. É onde a métrica de tempo nasce e onde a maior parte do ganho está.

### 6.1 Dores
Pedido não chega. Chega ilegível. Não sabe a ordem de prioridade. Faz itens que saem em momentos errados. Não sabe quanto tempo já passou. Precisa parar de trabalhar para se comunicar com o salão. Descobre falta de insumo no meio do preparo.

### 6.2 Facilidades a entregar — desenho do KDS

**Regra de ouro: mãos sujas, sem mouse, sem digitação.** Operação por **teclado numérico / bump bar**, cartões grandes, alto contraste, legível a 1,5 metro.

| Facilidade | Detalhe |
|---|---|
| **Fila ordenada por urgência, não por chegada** | O sistema calcula prioridade considerando prazo prometido, sincronização de mesa e tipo de item |
| **Cartão por pedido com cronômetro** | Verde → amarelo → vermelho conforme limiar configurado por produto |
| **Código numérico por cartão** | Operador digita o número e confirma — um toque para avançar |
| **Separação por praça** | Montagem, forno, fritura, bebidas, sobremesas — cada praça vê só o que é seu |
| **Contagem consolidada ("all-day")** | "12 mussarelas, 7 calabresas" — permite montar em lote e ganhar ritmo |
| **Indicador de ocupação do forno** | Quantas posições ocupadas, quanto falta para cada uma sair |
| **Momento de montar (fire time)** | O sistema diz **quando** iniciar cada item para que saiam sincronizados (seção 14.2) |
| **Marcação de falta de insumo** | Um toque marca item indisponível — some do cardápio de todos os canais imediatamente |
| **Refazer (re-fire)** | Registro explícito de retrabalho, com motivo — vira métrica de qualidade |
| **Modo pico** | Interface simplifica automaticamente quando a fila passa de X pedidos |
| **Histórico do turno** | Consulta rápida dos últimos pedidos concluídos, para conferência |

### 6.3 Os carimbos de tempo obrigatórios

Sem esses seis timestamps, nenhuma métrica de tempo existe:

```
T0  Pedido confirmado          → entra na fila
T1  Produção iniciada          → montagem começou
T2  Entrada no forno           → ocupou o gargalo
T3  Saída do forno             → cocção concluída
T4  Pedido pronto / expedição  → disponível para retirada
T5  Entregue (mesa) ou Despachado (delivery)
```

| Intervalo | Métrica | O que diagnostica |
|---|---|---|
| T0 → T1 | **Tempo de fila** | Falta de mão de obra ou de capacidade |
| T1 → T2 | **Tempo de montagem** | Eficiência da bancada e do mise en place |
| T2 → T3 | **Tempo de cocção** | Temperatura do forno, sobrecarga, padrão de produto |
| T3 → T4 | **Tempo de finalização** | Corte, embalagem, montagem final |
| T4 → T5 | **Tempo de expedição** | Comida esperando — perda de qualidade |
| **T0 → T5** | **Tempo total do pedido** | Meta declarada: **10 min (salão)** |

> Este é o dado que responde diretamente ao *"eu vou saber quantos minutos a minha pizza tá sendo feita"* — e é ele que transforma a promessa de 10 e 25 minutos em algo gerenciável em vez de aspiracional.

### 6.4 Métricas da cozinha

| Métrica | Definição | Meta / uso |
|---|---|---|
| **Tempo médio de produção por produto** | T1→T4 por item | Base do prazo prometido |
| **Percentil 90 do tempo** | 90% dos pedidos ficam abaixo de X | Mais honesto que a média — é o cliente insatisfeito |
| **Aderência ao prazo (OTD)** | % de pedidos dentro da meta | Indicador-mestre da cozinha |
| **Pedidos em atraso agora** | Contagem em tempo real | Ação imediata |
| **Taxa de retrabalho (re-fire)** | Itens refeitos ÷ produzidos | Qualidade e treinamento |
| **Taxa de falta de insumo** | Itens marcados indisponíveis | Falha de compra/previsão |
| **Produção por hora e por operador** | Throughput | Escala de pessoal |
| **Ociosidade do forno com fila** | Minutos perdidos | Perda direta de faturamento |
| **Sincronização de mesa** | Diferença entre primeiro e último item da mesma mesa | Qualidade de experiência |

### 6.5 Automações e alertas
Alerta sonoro e visual de pedido novo; escalonamento de cor por tempo; alerta de pedido acima do limiar (também para o gestor); aviso de posição livre no forno com fila esperando; agrupamento automático de itens idênticos entre pedidos próximos; sugestão de ordem de entrada no forno; baixa automática de insumo ao concluir o item; bloqueio automático do produto em todos os canais quando o insumo acaba.

---

## 7. Caixa

### 7.1 Dores
Conta montada na mão. Não sabe o que já foi entregue. Divergência no fechamento sem explicação. Fila no momento do pagamento. Conferência de maquininha separada do sistema.

### 7.2 Facilidades a entregar

| Facilidade | Detalhe |
|---|---|
| **Todas as mesas em uma tela** | Valor, tempo, status, garçom responsável |
| **Conta pronta a qualquer momento** | Sem digitação — o consumo já está lançado |
| **Divisão flexível** | Por pessoa, por item, valor fixo, percentual |
| **Múltiplas formas na mesma conta** | Parte dinheiro, parte cartão, parte PIX |
| **Taxa de serviço conforme regra** | Aplicada automaticamente, com opção de retirada registrada e auditada |
| **Desconto com autorização** | Acima do limite configurado, exige aprovação de perfil superior |
| **Abertura e fechamento de caixa** | Conferência guiada, sangria e suprimento registrados |
| **Conciliação assistida** | Comparação entre o registrado no sistema e o informado por Cielo/Mercado Pago |
| **Reimpressão e reenvio de comprovante** | Sem retrabalho |
| **Operação offline** | Recebe e registra normalmente; sincroniza depois |

### 7.3 Métricas geradas

| Métrica | Uso |
|---|---|
| **Tempo de fechamento de conta** | Gargalo de saída; impacta giro de mesa |
| **Divergência de caixa** | Valor e frequência — controle e confiança |
| **Composição por forma de pagamento** | Negociação de taxa com adquirente |
| **Custo de taxa de cartão** | Despesa frequentemente invisível — entra no resultado |
| **Descontos concedidos** | Valor, quem autorizou, motivo |
| **Cancelamentos pós-lançamento** | Indício de erro operacional ou desvio |
| **Ticket médio por canal e por período** | Base de faturamento |
| **Tempo mesa fechada → mesa liberada** | Giro de mesa |

### 7.4 Automações e alertas
Alerta de conta solicitada; alerta de divergência no fechamento acima do limite; alerta de desconto acima do padrão; bloqueio de fechamento com item pendente de entrega; conciliação automática de recebimento eletrônico; alerta de mesa fechada mas não liberada.

---

## 8. Entregador

### 8.1 Dores
Espera na loja sem saber quanto falta. Endereço incompleto. Não sabe o valor a receber. Faz viagens com uma entrega só. Sem registro de comprovação.

### 8.2 Facilidades a entregar
Fila de entregas atribuídas com endereço, mapa e valor a receber; aviso de "pizza sai do forno em 3 minutos" para chegar na hora certa e não esperar; **agrupamento de entregas próximas** com ordem sugerida de rota; um toque para "saí" e "entreguei"; registro de ocorrência (cliente ausente, endereço errado); acúmulo do dia (entregas e valores).

### 8.3 Métricas geradas

| Métrica | O que revela |
|---|---|
| **Tempo de espera do entregador na loja** | Descoordenação entre produção e despacho — custo puro |
| **Tempo despacho → entrega** | Eficiência de rota |
| **Entregas por hora** | Produtividade e dimensionamento da frota |
| **Entregas por rota (agrupamento)** | Ganho de eficiência logística |
| **Taxa de entrega no prazo** | Componente da meta de 25 minutos |
| **Ocorrências por motivo** | Qualidade de cadastro de endereço |
| **Custo por entrega** | Entra na margem real do delivery |

### 8.4 Automações e alertas
Atribuição automática por proximidade e disponibilidade; aviso antecipado de pedido saindo do forno; agrupamento sugerido; alerta de entrega em risco de estourar o prazo; notificação automática ao cliente a cada mudança de status.

---

## 9. Cliente de delivery

### 9.1 Dores
Não sabe quanto tempo vai levar. Não sabe se o pedido foi aceito. Refaz o endereço toda vez. Descobre no fim que o pagamento não passou.

### 9.2 Facilidades a entregar
Prazo estimado **realista e dinâmico**, calculado com a fila atual — não um número fixo de marketing; confirmação imediata de aceite; acompanhamento por etapa; endereço salvo e pedido anterior repetível em um toque; pagamento online (Mercado Pago) com confirmação antes da produção; cupom e programa de fidelidade *(fase posterior)*; canal próprio com a marca do estabelecimento, reduzindo dependência e comissão do marketplace.

### 9.3 Métricas geradas

| Métrica | Uso |
|---|---|
| **Taxa de conversão do funil** | Visita → carrinho → checkout → pago |
| **Abandono por etapa** | Onde o pedido morre (frete, prazo, pagamento) |
| **Ticket médio do canal próprio × iFood** | Justifica o investimento no canal próprio |
| **Taxa de recompra e frequência** | Base de fidelização |
| **Prazo prometido × realizado** | Confiabilidade — principal driver de recompra |
| **Comissão economizada** | Pedidos migrados do marketplace × taxa |
| **Raio de entrega × rentabilidade** | Onde entregar deixa de compensar |

### 9.4 Automações e alertas
Prazo recalculado conforme a fila; notificação a cada etapa; recuperação de carrinho abandonado *(fase posterior)*; sugestão baseada no histórico do cliente; bloqueio automático de item indisponível.

---

## 10. Estoquista / comprador

Perfil não citado na descoberta, mas **necessário** — a dor de estoque foi uma das mais explícitas: *"não se sabe quanto é necessário... não se sabe quais foram as entradas e precisa controlar."*

### 10.1 Dores
Não sabe quanto comprar. Descobre falta no meio do serviço. Não sabe quanto foi perdido. Compra por hábito, não por consumo.

### 10.2 Facilidades a entregar

| Facilidade | Detalhe |
|---|---|
| **Ficha técnica por produto** | Quanto de cada insumo cada item consome |
| **Baixa automática na venda** | Estoque teórico sempre atualizado, sem digitação |
| **Sugestão de compra** | Baseada em consumo histórico, previsão de demanda e prazo do fornecedor |
| **Contagem cíclica** | Contar poucos itens de alto valor com frequência, em vez de inventário total raro |
| **Registro de entrada simples** | Nota, quantidade, custo, validade, fornecedor |
| **Registro de perda com motivo** | Quebra, vencimento, erro de produção, cortesia |
| **Comparação de preço entre fornecedores** | Histórico de custo por insumo |
| **Alerta de estoque mínimo e de validade** | Antes de faltar, não depois |

### 10.3 Métricas geradas

| Métrica | Definição | Por que é decisiva |
|---|---|---|
| **CMV teórico × real** | O que deveria ter sido consumido × o que realmente saiu | **A métrica mais reveladora do negócio** — a diferença é perda, desvio ou erro de ficha |
| **Rendimento por insumo** | Ex.: pizzas produzidas por kg de queijo | Detecta porcionamento fora do padrão |
| **Taxa de perda por motivo** | % sobre compras | Onde o dinheiro evapora |
| **Giro de estoque** | Consumo ÷ estoque médio | Capital parado |
| **Cobertura em dias** | Estoque atual ÷ consumo diário | Risco de ruptura |
| **Ruptura (stockout)** | Ocorrências de item indisponível | Venda perdida |
| **Variação de custo de insumo** | Preço médio ao longo do tempo | Gatilho para reprecificar o cardápio |
| **Capital imobilizado** | Valor total em estoque | Fluxo de caixa |

> **Onde está o dinheiro escondido.** Em pizzaria, o queijo costuma responder por 30–40% do CMV. Um desvio de 15 gramas por pizza, em 3.000 pizzas/mês, são 45 kg — normalmente invisível sem ficha técnica. É aqui que o sistema costuma se pagar sozinho.

### 10.4 Automações e alertas
Baixa automática por ficha técnica; lista de compras gerada automaticamente; alerta de mínimo, de validade próxima e de divergência relevante entre teórico e real; alerta de aumento de custo de insumo que compromete a margem do produto.

---

## 11. Gestor / dono

> É o usuário da diretriz de **controle e métrica total**. Precisa de duas coisas distintas: **pilotar agora** e **decidir depois**.

### 11.1 A hierarquia da informação

```
NÍVEL 1 — PULSO (segundos)        "está tudo correndo?"
          Celular, 5 números, sem clique

NÍVEL 2 — DESEMPENHO (minutos)    "como foi o dia/semana/mês?"
          Comparativo com período anterior e com a meta

NÍVEL 3 — DIAGNÓSTICO (análise)   "por que aconteceu?"
          Abertura até o pedido individual

NÍVEL 4 — DECISÃO (periódica)     "o que eu mudo?"
          Engenharia de cardápio, escala, precificação, investimento
```

### 11.2 Nível 1 — Pulso (tela de celular, cinco números)

1. **Faturamento de hoje** — com % contra a média do mesmo dia da semana
2. **Pedidos em atraso agora** — o número que exige ação imediata
3. **Tempo médio da última hora** — contra a meta
4. **Mesas ocupadas / total** — ocupação
5. **Alertas abertos** — o que está fora do lugar

### 11.3 Nível 2 — Desempenho

**Bloco tempo:** tempo total médio e percentil 90 por canal; aderência ao prazo; tempo por etapa (fila, montagem, forno, expedição); tempo por faixa horária — o mapa do gargalo.

**Bloco venda:** faturamento por dia/semana/mês com comparativo; venda por canal (salão, delivery próprio, iFood); ticket médio; pedidos por hora e por dia da semana (mapa de calor); giro de mesa; ranking de produtos.

**Bloco pessoas:** produtividade por operador; custo de pessoal sobre faturamento; escala sugerida × realizada.

**Bloco cliente:** nota de satisfação correlacionada ao tempo; taxa de recompra no delivery; reclamações por motivo.

### 11.4 Nível 3–4 — Engenharia de cardápio (a análise de maior retorno)

Cruzamento de **popularidade × margem de contribuição**, gerado automaticamente pelo sistema a partir da ficha técnica e das vendas:

```
                    ALTA MARGEM          BAIXA MARGEM
                ┌────────────────────┬────────────────────┐
   ALTO         │     ESTRELA        │  CAVALO DE BATALHA │
   VOLUME       │  Proteger, destacar│  Reduzir custo ou  │
                │  Não mexer no preço│  ajustar porção    │
                ├────────────────────┼────────────────────┤
   BAIXO        │    QUEBRA-CABEÇA   │      ABACAXI       │
   VOLUME       │  Promover, treinar │  Reformular ou     │
                │  o garçom a vender │  tirar do cardápio │
                └────────────────────┴────────────────────┘
```

Sem ficha técnica, essa matriz é impossível — e é exatamente ela que responde *"quais produtos me dão lucro"*. Deve ser um relatório nativo, não um exercício manual.

**Outras decisões de nível 4 que o sistema deve sustentar:**
- **Precificação:** custo real por produto + variação de insumo → quando e quanto reajustar
- **Escala:** curva de demanda por dia e hora → quantas pessoas em cada turno
- **Cardápio:** itens que travam a cozinha nos horários de pico (alto tempo, alta demanda simultânea)
- **Canal:** margem real do delivery próprio × iFood, já descontadas comissão, embalagem e custo de entrega
- **Investimento:** ociosidade do forno com fila responde se vale comprar um segundo forno
- **Ponto de equilíbrio:** quanto precisa vender por dia para não ter prejuízo

### 11.5 Métricas-mestre do dono

| Métrica | Por que é mestre |
|---|---|
| **CMV %** | Insumo sobre faturamento — saúde da operação de compra e porcionamento |
| **Custo de pessoal %** | Segunda maior despesa |
| **Prime cost (CMV + pessoal)** | Referência de mercado: abaixo de ~65% do faturamento |
| **Margem de contribuição por produto** | Base de toda decisão de cardápio |
| **Aderência ao prazo** | Proxy direto de satisfação e recompra |
| **Ponto de equilíbrio diário** | Quanto precisa vender para não perder dinheiro |
| **Resultado do período** | A resposta final |

### 11.6 Facilidades e automações
Acesso pelo celular de qualquer lugar; resumo diário automático ao fechar o dia; resumo semanal comparativo; alertas por exceção (não por rotina); exportação para o contador; definição de metas com acompanhamento; navegação de qualquer número até o pedido de origem.

---

## 12. Administrador da plataforma (Replay)

### 12.1 Necessidades
Implantar um novo estabelecimento sem desenvolvimento; saber quais instalações estão saudáveis; suportar sem depender de acesso presencial; entender uso real para priorizar evolução do produto.

### 12.2 Facilidades a entregar
Provisionamento guiado de nova instância; aplicação de identidade visual por formulário; importação de cardápio e ficha técnica por planilha; biblioteca de modelos por tipo de negócio (pizzaria, hamburgueria, restaurante) com cardápio e configuração pré-montados; monitoramento remoto de todas as instalações; acesso de suporte com registro e autorização.

### 12.3 Métricas de produto

| Métrica | Uso |
|---|---|
| **Tempo de implantação de nova instância** | Indicador-chave da escalabilidade do produto |
| **Instalações com sincronização atrasada** | Risco operacional no cliente |
| **Uso por módulo e por instância** | O que é usado de verdade × o que foi construído à toa |
| **Chamados de suporte por causa** | Onde o produto confunde o usuário |
| **Adoção de novas versões** | Saúde do parque instalado |
| **Retenção e churn** | Saúde comercial do produto |

---

# PARTE III — OTIMIZAÇÃO DO FLUXO DO PRÓPRIO SISTEMA

## 13. O modelo de eventos: a espinha dorsal

**Decisão arquitetural central: o sistema não armazena apenas estados, armazena eventos.**

Cada transição gera um registro imutável:

```
evento {
  id, estabelecimento, tipo, ocorrido_em, registrado_em,
  autor, dispositivo, origem (local|nuvem),
  entidade (pedido/item/mesa/insumo/caixa),
  dados_anteriores, dados_novos
}
```

**Por que isso é inegociável:**

| Consequência | Explicação |
|---|---|
| **Toda métrica passa a ser derivada, não digitada** | Cumpre o princípio P1 |
| **Auditoria vem de graça** | Atende à exigência confirmada do cliente |
| **Sincronização offline funciona** | Fila de eventos é naturalmente reconciliável; estado sobrescrito não é |
| **Indicadores novos não exigem nova coleta** | Basta reprocessar o histórico existente |
| **Rastreabilidade do número até a origem** | Cumpre o princípio P5 |

> `ocorrido_em` × `registrado_em` são campos distintos e ambos obrigatórios. Um pedido feito às 20h03 offline e sincronizado às 21h15 precisa contar como 20h03 na métrica — senão todo indicador de horário fica corrompido pela instabilidade da internet.

---

## 14. Inteligência de fluxo: sequenciamento, sincronização e previsão

Aqui está a diferença entre um sistema que **registra** a operação e um que a **otimiza**.

### 14.1 Priorização dinâmica da fila

A fila da cozinha **não deve ser ordem de chegada pura**. O sistema calcula prioridade combinando:

- Prazo prometido menos tempo já decorrido (urgência real)
- Tempo de preparo do item (item longo precisa entrar antes)
- Sincronização de mesa (itens do mesmo pedido saem juntos)
- Canal (delivery esfria em rota; tem tolerância menor)
- Ocupação do gargalo (aproveitar posição livre no forno)

> Regra de transparência: a ordem sugerida deve ser **visível e explicável**, e o operador pode sempre sobrepor. Sistema que reordena sem explicar perde a confiança da cozinha na primeira semana.

### 14.2 Sequenciamento reverso — o "fire time"

A dor P3 (itens saindo em momentos diferentes) se resolve calculando **para trás** a partir do momento de saída desejado:

```
Pedido: 1 pizza grande (12 min) + 1 porção de fritas (5 min) + 1 refrigerante (1 min)

Saída sincronizada em T+12:
  T+0   → inicia pizza
  T+7   → inicia fritas
  T+11  → prepara bebida
  T+12  → tudo pronto, sai junto
```

Sem isso, a fritura sai às 5 minutos e chega fria à mesa, ou a bebida chega no fim. **O KDS deve dizer quando começar cada item, não apenas o que fazer.**

### 14.3 Agrupamento inteligente (batching)

Itens idênticos em pedidos próximos no tempo podem ser montados em lote, ganhando ritmo de bancada — desde que isso **não atrase o pedido mais antigo**. O sistema sugere o agrupamento e mostra o ganho estimado; a decisão fica com o operador.

O mesmo princípio no delivery: entregas próximas agrupadas em uma rota, respeitando o prazo do pedido mais antigo.

### 14.4 Promessa de prazo dinâmica

O prazo informado ao cliente deve ser **calculado**, não fixo:

```
Prazo = tempo de preparo do item
      + fila projetada (pedidos à frente ÷ capacidade atual)
      + tempo médio de expedição
      + tempo de rota (delivery, por região)
      + margem de segurança configurável
```

Quando a fila cresce, o prazo aumenta automaticamente. Em situação extrema, o sistema pode **pausar o canal de delivery** ou restringir itens de preparo longo — decisão configurável.

> Prometer 25 minutos com 40 pedidos na fila não é meta, é geração programada de cliente insatisfeito. Um sistema honesto no prazo retém mais que um sistema otimista.

### 14.5 Previsão de demanda e pré-preparo

Com histórico de 60–90 dias, o sistema projeta a demanda por dia e faixa horária, considerando dia da semana, sazonalidade e eventos. Isso alimenta:

- **Lista de pré-preparo (mise en place)** — quantas massas abrir, quanto molho produzir
- **Sugestão de compra** — com prazo de fornecedor e cobertura desejada
- **Sugestão de escala** — quantas pessoas por turno

> Em pizzaria, o pré-preparo mal dimensionado é perda dos dois lados: massa sobrando vira desperdício, massa faltando vira venda perdida no pico. É um dos ganhos mais rápidos da previsão.

### 14.6 Propagação instantânea de indisponibilidade

Quando um insumo acaba ou a cozinha marca um item indisponível, o efeito deve ser **imediato e em todos os canais**: cardápio da mesa, app do garçom, delivery próprio e (quando integrado) marketplace. Nenhum cliente deve conseguir pedir o que não existe — é uma das principais causas de cancelamento e de nota baixa.

---

## 15. Automações que eliminam trabalho humano

O melhor processo é o que ninguém precisa executar. Ordem de prioridade por retorno:

| # | Automação | Trabalho eliminado |
|---|---|---|
| 1 | **Roteamento do pedido** para cozinha e caixa | Deslocamento e comanda de papel |
| 2 | **Baixa de estoque por ficha técnica** | Controle manual inexistente hoje |
| 3 | **Cálculo de custo e margem por venda** | Planilha que ninguém mantém |
| 4 | **Montagem da conta** | Digitação e erro de fechamento |
| 5 | **Notificação por etapa** | Ida e volta para "conferir se saiu" |
| 6 | **Lista de compras sugerida** | Compra por palpite |
| 7 | **Fechamento diário automático** | Consolidação manual |
| 8 | **Conciliação de pagamento eletrônico** | Conferência de maquininha |
| 9 | **Bloqueio de item sem estoque** | Venda do que não existe |
| 10 | **Prazo dinâmico** | Promessa irreal |
| 11 | **Pré-preparo sugerido** | Palpite de produção |
| 12 | **Exportação contábil** | Repasse manual ao contador |
| 13 | **Resumo diário ao dono** | Perguntar "como foi hoje?" |

---

## 16. Comportamento em degradação (offline e falhas)

O requisito offline-first exige que o comportamento em falha seja **projetado**, não improvisado.

| Cenário | O que continua | O que degrada | Comportamento exigido |
|---|---|---|---|
| **Internet cai** | Mesa, KDS, caixa, comanda, fechamento | Delivery, pagamento online, painel remoto | Aviso discreto ao operador; fila de sincronização acumula; nada trava |
| **Servidor local cai** | Nada localmente | Toda a operação local | **Modo contingência:** dispositivos mantêm cache do cardápio e permitem lançamento local para sincronizar depois. Requisito crítico — precisa de decisão explícita |
| **Um dispositivo cai** | Todo o resto | Aquele posto | Estado está no servidor local; basta abrir em outro aparelho e continuar |
| **KDS cai no pico** | Pedidos continuam entrando | Visão da cozinha | Fallback de impressão ou espelhamento em tablet |
| **Adquirente fora do ar** | Operação inteira | Cartão | Registra a forma manualmente para conciliar depois |
| **Sincronização em conflito** | — | — | Regra explícita de resolução, com log e revisão do gestor |

**Regras de sincronização:**

1. Fila persistente, ordenada, com retomada automática — nada se perde
2. Operações idempotentes — reenvio não duplica pedido
3. `ocorrido_em` preservado (seção 13)
4. Indicador visível de "última sincronização há X" no painel do dono
5. Alerta ao gestor e à Replay quando o atraso ultrapassa o limite

> **Pendência a decidir.** Se o servidor local falhar fisicamente no meio do serviço, qual é o plano? Equipamento reserva, contingência em nuvem ou operação manual assistida? Esta decisão precisa estar na proposta — é o pior cenário possível e o cliente vai perguntar.

---

## 17. Métricas de saúde do próprio sistema

O sistema também precisa ser medido — especialmente em um produto que rodará em várias lojas.

| Métrica | Limiar sugerido | Por quê |
|---|---|---|
| **Latência pedido → KDS** | < 2 segundos | É a promessa central do produto |
| **Atraso de sincronização** | < 60 segundos em operação normal | Confiabilidade do painel remoto |
| **Eventos na fila não sincronizados** | Alerta acima de 500 | Indício de falha silenciosa |
| **Disponibilidade do servidor local** | > 99,9% em horário de operação | Continuidade da operação |
| **Tempo de resposta do KDS ao toque** | < 300 ms | Usabilidade sob pressão |
| **Conflitos de sincronização** | Próximo de zero | Qualidade do modelo de dados |
| **Erros por instalação** | Monitorado por instância | Manutenção preventiva |
| **Cobertura de instrumentação** | 100% das transições com timestamp e autor | Se cair, as métricas de negócio se corrompem |

> A última linha é a mais importante. **Métrica de negócio errada é pior do que métrica nenhuma** — o dono decide com base em número falso. Recomenda-se validação de integridade dos eventos como rotina automática diária.

---

# PARTE IV — CONSOLIDAÇÃO

## 18. Dicionário de métricas

| Métrica | Fórmula / definição | Usuário-alvo | Fase |
|---|---|---|---|
| Tempo de fila | T1 − T0 | Cozinha, Gestor | 1 |
| Tempo de produção | T4 − T1 | Cozinha, Gestor | 1 |
| Tempo de cocção | T3 − T2 | Cozinha | 1 |
| Tempo de expedição | T5 − T4 | Garçom, Gestor | 1 |
| **Tempo total do pedido** | T5 − T0 | Todos | 1 |
| Percentil 90 do tempo | 90º percentil de (T5 − T0) | Gestor | 1 |
| Aderência ao prazo (OTD) | Pedidos no prazo ÷ total | Gestor | 1 |
| Ocupação do forno | Tempo em uso ÷ tempo de operação | Cozinha, Gestor | 2 |
| Ociosidade com fila | Min. com posição livre e fila > 0 | Gestor | 2 |
| Pizzas por hora | Concluídas ÷ horas | Gestor | 1 |
| Ticket médio | Faturamento ÷ nº de pedidos | Gestor, Caixa | 1 |
| Giro de mesa | Nº de atendimentos ÷ nº de mesas | Gestor | 1 |
| Taxa de ocupação do salão | Mesas ocupadas ÷ total, por faixa horária | Gestor | 1 |
| Taxa de upsell | Sugestões aceitas ÷ oferecidas | Garçom, Gestor | 2 |
| Taxa de retrabalho | Itens refeitos ÷ produzidos | Cozinha, Gestor | 2 |
| Taxa de ruptura | Ocorrências de item indisponível | Estoque, Gestor | 2 |
| **CMV %** | Custo de insumo ÷ faturamento | Gestor | 2 |
| **CMV teórico × real** | Consumo por ficha × consumo apurado | Estoque, Gestor | 2 |
| Rendimento por insumo | Produtos ÷ unidade de insumo | Estoque | 2 |
| Margem de contribuição | Preço − custo variável | Gestor | 2 |
| Curva de rentabilidade | Volume × margem (matriz) | Gestor | 2 |
| Giro de estoque | Consumo ÷ estoque médio | Estoque | 2 |
| Cobertura em dias | Estoque ÷ consumo diário | Estoque | 2 |
| Custo de pessoal % | Folha ÷ faturamento | Gestor | 3 |
| **Prime cost** | (CMV + pessoal) ÷ faturamento | Gestor | 3 |
| Ponto de equilíbrio | Custo fixo ÷ margem de contribuição % | Gestor | 3 |
| Resultado do período | Receita − custos totais | Gestor | 3 |
| Tempo de espera do entregador | Chegada → despacho | Gestor | 4 |
| Entregas por rota | Entregas ÷ saídas | Gestor | 4 |
| Custo por entrega | Custo total de entrega ÷ entregas | Gestor | 4 |
| Conversão do funil (delivery) | Pago ÷ visitas | Gestor | 4 |
| Comissão economizada | Pedidos próprios × taxa de marketplace | Gestor | 4 |
| Taxa de recompra | Clientes com 2+ pedidos ÷ total | Gestor | 4 |
| Tempo de implantação | Contrato → operação | Replay | 5 |

---

## 19. Matriz de alertas por usuário e limiar

> Todos os limiares devem ser **configuráveis por estabelecimento** — princípio da seção 8 do documento de visão.

| Alerta | Limiar sugerido | Cliente | Garçom | Cozinha | Caixa | Entregador | Gestor |
|---|---|:-:|:-:|:-:|:-:|:-:|:-:|
| Pedido recebido | imediato | ✓ | ✓ | ✓ | ✓ | | |
| Produção iniciada | imediato | ✓ | ✓ | | | | |
| Item pronto | imediato | ✓ | ✓ | | | | |
| **Item parado na janela** | > 2 min | | ✓ | ✓ | | | |
| **Pedido acima do tempo-alvo** | > meta do produto | | ✓ | ✓ | | | ✓ |
| **Forno ocioso com fila** | > 1 min | | | ✓ | | | ✓ |
| Mesa sem pedido | > 10 min da abertura | | ✓ | | | | |
| Chamado de garçom | imediato | | ✓ | | | | |
| Conta solicitada | imediato | | ✓ | | ✓ | | |
| Mesa fechada não liberada | > 5 min | | ✓ | | ✓ | | |
| Insumo indisponível | imediato | ✓ | ✓ | ✓ | | | ✓ |
| Estoque mínimo | configurável | | | | | | ✓ |
| Validade próxima | 3 dias | | | | | | ✓ |
| **Divergência CMV teórico × real** | > 5% | | | | | | ✓ |
| Margem negativa em produto | qualquer | | | | | | ✓ |
| Desconto acima do padrão | configurável | | | | ✓ | | ✓ |
| Divergência de caixa | > R$ configurável | | | | ✓ | | ✓ |
| Entrega em risco de atraso | 5 min antes | | | | | ✓ | ✓ |
| Entregador esperando | > 3 min | | | ✓ | | ✓ | ✓ |
| Faturamento abaixo da média | fim do turno | | | | | | ✓ |
| Fila acima da capacidade | configurável | | | ✓ | | | ✓ |
| **Falha de sincronização** | > 5 min | | | | | | ✓ |
| Servidor local indisponível | imediato | | | | | | ✓ |

> **Regra anti-ruído.** Alerta em excesso é ignorado — e alerta ignorado é o mesmo que alerta inexistente. Cada perfil deve receber apenas o que exige **ação dele**, com agrupamento de alertas repetidos e silenciamento configurável. Recomenda-se revisar trimestralmente a taxa de alertas ignorados por tipo.

---

## 20. Anti-padrões: o que não fazer

| # | Anti-padrão | Por que falha | O que fazer |
|---|---|---|---|
| 1 | **Exigir digitação na cozinha** | Mãos ocupadas, pressão, sujeira | Teclado numérico, um toque, cartões grandes |
| 2 | **KDS com muita informação** | Ilegível à distância, gera erro | Máximo de informação por cartão limitado; hierarquia visual forte |
| 3 | **Pedir ao usuário que "registre" métrica** | Ninguém faz; o dado apodrece | Métrica derivada de evento operacional |
| 4 | **Usar só a média de tempo** | Esconde o cliente insatisfeito | Percentil 90 + % de aderência ao prazo |
| 5 | **Prazo fixo de entrega** | Vira mentira no pico | Prazo dinâmico calculado pela fila |
| 6 | **Alertar tudo para todos** | Ruído → todos ignoram | Alerta por ação, não por evento |
| 7 | **Relatório sem comparativo** | Número solto não gera decisão | Sempre contra período anterior e meta |
| 8 | **Bloquear a operação por regra de gestão** | A operação contorna o sistema | Permitir com registro e auditoria, não impedir |
| 9 | **Ranking individual exposto sem contexto** | Sabotagem de dado, clima ruim | Individual privado, agregado para gestão |
| 10 | **Adiar a ficha técnica** | Sem ela não há custo, margem nem CMV | Fase 2 é indispensável, não opcional |
| 11 | **Inventário total mensal** | Ninguém sustenta; some em 3 meses | Contagem cíclica de poucos itens de alto valor |
| 12 | **Customizar por código para um cliente** | Destrói o produto replicável | Configuração, nunca código |
| 13 | **Tratar dado offline com horário de sincronização** | Corrompe toda métrica de horário | `ocorrido_em` separado de `registrado_em` |
| 14 | **Painel que não abre até o pedido** | Gera desconfiança no número | Rastreabilidade em até 3 cliques |
| 15 | **Treinar uma vez e abandonar** | Uso degrada, dado degrada | Piloto acompanhado + revisão nas semanas 2 e 4 |

---

## 21. Priorização: ganho rápido × esforço

### 21.1 Ganho alto, esforço baixo — fazer primeiro

| Item | Ganho |
|---|---|
| Pedido digital roteado para cozinha e caixa | Elimina P1 (perda de pedido) — a dor declarada |
| Seis timestamps do pedido | Habilita **toda** a métrica de tempo com custo marginal quase zero |
| Cronômetro e escalonamento de cor no KDS | Torna o atraso visível e acionável |
| Notificação de item pronto ao garçom | Elimina deslocamento e comida esfriando |
| Mapa de mesas com tempo | Gestão visual do salão |
| Conta montada automaticamente | Reduz o gargalo de saída, aumenta giro |
| Resumo diário automático ao dono | Primeira experiência de "controle total" |

### 21.2 Ganho alto, esforço médio — segunda onda

| Item | Ganho |
|---|---|
| Ficha técnica e baixa automática | Destrava CMV, custo e margem — **a maior descoberta financeira do projeto** |
| Sequenciamento reverso (fire time) | Elimina P3 (itens dessincronizados) |
| Prazo dinâmico | Confiabilidade e recompra no delivery |
| Propagação de indisponibilidade | Elimina cancelamento por item inexistente |
| Engenharia de cardápio | Decisão de maior retorno financeiro do dono |
| Contagem cíclica e alerta de mínimo | Elimina ruptura e reduz perda |

### 21.3 Ganho alto, esforço alto — planejar bem

| Item | Observação |
|---|---|
| Offline-first completo com contingência | Já reconhecido como requisito estruturante e de custo relevante |
| Previsão de demanda e pré-preparo | Depende de 60–90 dias de histórico — só faz sentido após a Fase 2 |
| Agrupamento e roteirização de entregas | Ganho proporcional ao volume de delivery |
| Plataforma multi-estabelecimento madura | Fundação na Fase 0, maturidade na Fase 5 |

### 21.4 Cuidado — pode ser armadilha

| Item | Risco |
|---|---|
| Programa de fidelidade | Alto esforço, retorno incerto antes de ter base de clientes |
| Integração com iFood | Depende de terceiros; avaliar custo × benefício real |
| Relatórios muito granulares na v1 | Ninguém usa; melhor poucos indicadores bem escolhidos |
| Personalização visual ilimitada | Custo alto de manutenção; oferecer conjunto controlado de temas |

---

## Encerramento

Três conclusões que atravessam todo o documento:

**1. O ganho de tempo vem da eliminação de deslocamento e espera, não de digitação mais rápida.** Pedido que nasce digital, notificação que vai até a pessoa e conta já montada devolvem mais minutos por dia do que qualquer otimização de tela.

**2. O ganho financeiro vem da ficha técnica.** Tempo e fluxo resolvem a dor visível; custo, margem e CMV resolvem a dor cara. Um desvio silencioso de porcionamento costuma valer mais, ao mês, do que várias horas economizadas de garçom. A Fase 2 não é opcional.

**3. A métrica só existe se nascer do trabalho.** Todo indicador deste documento é subproduto automático de uma ação que já precisa acontecer. No instante em que alguém precisar "alimentar o sistema" para gerar um número, esse número está condenado.

> **Recomendação de método.** Antes do desenvolvimento, realizar uma sessão de observação na operação real da Dona Betinha em um dia de pico. Como o cliente declarou não ter processo, o desenho precisa nascer da realidade observada — não da descrição verbal. Duas horas de observação valem mais que duas reuniões.

---

*Documento complementar a `Visao-Geral-Sistema-Dona-Betinha.md` (v1.1). Replay Studio — Projeto 004_DonaBetinha.*
