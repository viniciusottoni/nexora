# 07 — Backlog · Épicos e User Stories
## Ecossistema Nexora

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Backlog de produto |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `01-PRD-Especificacao-Funcional.md`, `04-Catalogo-de-Eventos-e-Maquinas-de-Estado.md` |

---

## 1. Como ler este backlog

**Formato da história:** `Como <persona>, quero <ação>, para <resultado>.`
**Critérios de aceite:** Gherkin (Dado / Quando / Então).
**Estimativa:** Fibonacci em pontos (1, 2, 3, 5, 8, 13). Acima de 13 → fatiar.
**Prioridade:** MoSCoW — **M**ust, **S**hould, **C**ould, **W**on't.

| Épico | Nome | Fase | Pontos |
|---|---|:-:|---:|
| E-00 | Fundação da plataforma | 0 | 55 |
| E-01 | Catálogo e cardápio | 1 | 42 |
| E-02 | Mesa e comanda | 1 | 47 |
| E-03 | Pedido e roteamento | 1 | 55 |
| E-04 | KDS — cozinha | 1 | 50 |
| E-05 | Caixa e pagamento | 1 | 47 |
| E-06 | Sincronização local ↔ nuvem | 1 | 55 |
| E-07 | Painel do dono v1 | 1 | 42 |
| E-08 | Alertas e notificações | 1 | 21 |
| E-09 | Auditoria | 1 | 13 |
| E-10 | Estoque e ficha técnica | 2 | 68 |
| E-11 | Inteligência de fluxo | 2 | 42 |
| E-12 | Financeiro de gestão | 3 | 55 |
| E-13 | Delivery próprio | 4 | 68 |
| E-14 | Plataforma em escala | 5 | 47 |
| E-15 | Gestão geral da plataforma | 5 | 58 |
| | **Total Fases 0–1 (MVP)** | | **427** |

---

# FASE 0 — FUNDAÇÃO

## E-00 · Fundação da plataforma

> Não é entregável ao cliente, mas é o que torna o produto replicável. Adiar significa reescrever o núcleo depois.

### US-001 · Estrutura multi-tenant com isolamento
**Como** administrador da plataforma, **quero** que os dados de cada estabelecimento fiquem isolados, **para** que nenhum cliente veja dados de outro.
**Prio:** M · **Pontos:** 8 · **RF:** RF-PLT-01

```gherkin
Cenário: Isolamento imposto pelo banco
  Dado um usuário autenticado no tenant A
  Quando ele consultar qualquer tabela de negócio
  Então apenas registros com tenant_id = A devem retornar

Cenário: Tentativa de acesso cruzado
  Dado um usuário do tenant A
  Quando tentar acessar um pedido do tenant B pelo ID
  Então deve receber 404 (idêntico a recurso inexistente)
  E a tentativa deve ser registrada em audit_log

Cenário: Query sem contexto de tenant
  Dado que a conexão não definiu app.tenant_id
  Quando executar uma query em tabela com RLS
  Então nenhum registro deve retornar
```

### US-002 · Provisionar novo estabelecimento
**Como** administrador da plataforma, **quero** criar um estabelecimento sem alterar código, **para** implantar em escala.
**Prio:** M · **Pontos:** 5 · **RF:** RF-PLT-05

```gherkin
Cenário: Criação de tenant
  Dado que informei nome, slug, plano e modelo de negócio
  Quando confirmar a criação
  Então o tenant deve ser criado com configuração padrão do modelo
  E deve ser gerado um token de instalação do servidor local
  E deve ser retornado o comando de instalação pronto para uso
```

### US-003 · Identidade visual por estabelecimento
**Prio:** M · **Pontos:** 8 · **RF:** RF-PLT-02

```gherkin
Cenário: Aplicação de marca em runtime
  Dado um tenant com cores e logo configurados
  Quando qualquer aplicação web for carregada para esse tenant
  Então as cores devem ser aplicadas via CSS custom properties
  E o logo e o ícone do PWA devem ser os do tenant
  E nenhum build específico deve ter sido gerado

Cenário: Alteração de marca sem deploy
  Dado que o gestor alterou a cor primária
  Quando recarregar a aplicação
  Então a nova cor deve estar aplicada em até 60 segundos
```

### US-004 · Autenticação e perfis
**Prio:** M · **Pontos:** 13 · **RF:** RF-IAM-01 a 07

```gherkin
Cenário: Login de gestor
  Dado um usuário com e-mail e senha válidos
  Quando efetuar login
  Então deve receber access token de 15 min e refresh de 30 dias
  E o token deve conter tenantId, papéis e permissões

Cenário: Login operacional por PIN
  Dado um dispositivo registrado e um operador com PIN válido
  Quando digitar o PIN
  Então deve receber sessão de 8 horas vinculada ao dispositivo

Cenário: Bloqueio por tentativas
  Dado 5 tentativas de PIN incorretas
  Quando tentar novamente
  Então o acesso deve ser bloqueado por 15 minutos
  E o gestor deve ser notificado

Cenário: Autorização de ação sensível
  Dado um operador sem permissão de cancelar item iniciado
  Quando solicitar o cancelamento
  Então deve ser pedido o PIN de um perfil superior no mesmo dispositivo
  E, autorizado, a ação deve registrar quem autorizou
```

### US-005 · Registro de dispositivos
**Prio:** M · **Pontos:** 5 · **RF:** RF-IAM-05

### US-006 · Servidor local instalável por script
**Prio:** M · **Pontos:** 8

```gherkin
Cenário: Instalação de nova loja
  Dado um mini-PC com Docker e o token de instalação
  Quando executar ./install.sh --tenant=X --token=Y
  Então os containers devem subir
  E a instalação deve se registrar na nuvem
  E cardápio e configuração devem ser baixados
  E o sistema deve estar operacional em menos de 30 minutos
```

### US-007 · Pipeline de CI/CD
**Prio:** M · **Pontos:** 8

```gherkin
Cenário: PR bloqueado por violação de isolamento
  Dado um PR cujo código compara literalmente um identificador de tenant
  Quando o CI executar
  Então o build deve falhar com mensagem apontando o ADR-013
```

---

# FASE 1 — MVP

## E-01 · Catálogo e cardápio

### US-010 · Cadastrar categorias e produtos — **M · 5** · RF-CAT-01
### US-011 · Variações com preço próprio — **M · 5** · RF-CAT-02
### US-012 · Grupos de modificadores — **M · 8** · RF-CAT-03

```gherkin
Cenário: Modificador obrigatório
  Dado um produto com grupo "Tamanho" obrigatório e seleção única
  Quando o cliente tentar adicionar sem escolher
  Então o sistema deve impedir e destacar o grupo pendente
```

### US-013 · Pizza meio a meio — **M · 8** · RF-CAT-04/05

```gherkin
Cenário: Montagem de meio a meio
  Dado um produto que permite frações e limite de 2 sabores
  Quando o cliente escolher dois sabores de mesmo tamanho
  Então o item deve conter duas frações com peso 0,5 cada

Cenário: Precificação por maior valor
  Dado a regra "HIGHEST" configurada
  E sabores de R$ 45,00 e R$ 52,00
  Quando o item for calculado
  Então o preço deve ser R$ 52,00

Cenário: Baixa proporcional de estoque
  Dado um meio a meio de Mussarela e Calabresa
  Quando o item for concluído
  Então deve ser baixada metade dos insumos de cada ficha técnica
```

### US-014 · Preço por canal — **M · 3** · RF-CAT-06
### US-015 · Marcar produto indisponível — **M · 5** · RF-CAT-07

```gherkin
Cenário: Propagação imediata
  Dado um produto disponível em todos os canais
  Quando a cozinha marcá-lo como indisponível
  Então ele deve sumir do cardápio da mesa, do garçom e do delivery em até 2 segundos
```

### US-016 · Tempo de preparo e praça por produto — **M · 3** · RF-CAT-08/09
### US-017 · Cadastro de praças de produção — **M · 5**

---

## E-02 · Mesa e comanda

### US-020 · Cadastrar ambientes, mesas e gerar QR Code — **M · 5** · RF-SAL-01
### US-021 · Cliente acessa cardápio pelo QR Code — **M · 8** · RF-SAL-02

```gherkin
Cenário: Acesso sem instalação
  Dado um cliente sentado na mesa 12
  Quando ler o QR Code com a câmera
  Então o cardápio deve abrir no navegador em até 2 segundos em 4G
  E não deve ser exigido cadastro nem instalação
  E a marca exibida deve ser a do estabelecimento
```

### US-022 · Abrir mesa (garçom ou cliente) — **M · 5** · RF-SAL-04
### US-023 · Mapa de mesas com status e tempo — **M · 8** · RF-SAL-05

```gherkin
Cenário: Visão do salão
  Dado mesas em estados diferentes
  Quando o garçom abrir o mapa
  Então cada mesa deve exibir status, tempo aberto e valor consumido
  E mesas acima do tempo médio devem ser destacadas
```

### US-024 · Consumo da mesa em tempo real — **M · 5** · RF-SAL-06
### US-025 · Chamar garçom — **M · 3** · RF-SAL-07
### US-026 · Solicitar a conta — **M · 3** · RF-SAL-08
### US-027 · Dividir a conta — **M · 8** · RF-SAL-10
### US-028 · Repetir item com um toque — **S · 2** · RF-SAL-11

---

## E-03 · Pedido e roteamento

> Épico que resolve a dor central declarada: *"o pedido é feito e não chega para cozinha"*.

### US-030 · Criar pedido com itens, modificadores e frações — **M · 13** · RF-PED-01/08

```gherkin
Cenário: Pedido do cliente na mesa
  Dado um cliente com itens no carrinho
  Quando confirmar o pedido
  Então o pedido deve ser criado com status PLACED
  E o evento order.placed deve ser emitido com occurredAt
  E o prazo estimado deve ser retornado

Cenário: Reenvio por instabilidade de rede
  Dado que o cliente tocou "enviar" duas vezes por falha de sinal
  Quando a segunda requisição chegar com a mesma Idempotency-Key
  Então deve retornar o mesmo pedido, sem duplicar
```

### US-031 · Roteamento simultâneo para cozinha e caixa — **M · 8** · RN-001

```gherkin
Cenário: Chegada ao KDS
  Dado um pedido confirmado com itens de praças diferentes
  Quando o pedido for criado
  Então cada item deve aparecer na fila da sua praça em até 2 segundos
  E o caixa deve ver o consumo atualizado da mesa
  E mesa, garçom, cozinha e caixa devem receber alerta
```

### US-032 · Carimbos de tempo T0 a T5 — **M · 8** · RF-PED-02

```gherkin
Cenário: Registro completo do ciclo
  Dado um item que percorreu todo o fluxo
  Quando consultar o item
  Então devem existir placedAt, firedAt, readyAt e servedAt
  E cada carimbo deve ter autor e dispositivo registrados
  E o horário gravado deve ser o de ocorrência, não o de sincronização
```

### US-033 · Cancelar item ou pedido com autorização — **M · 8** · RF-PED-04/05

```gherkin
Cenário: Cancelamento após início de produção
  Dado um item em estado FIRED
  Quando o garçom solicitar o cancelamento
  Então deve ser exigida autorização de perfil superior
  E, autorizado, o item deve ser cancelado com motivo obrigatório
  E o insumo consumido deve gerar registro de perda
```

### US-034 · Operar pedido integralmente offline — **M · 13** · RF-PED-09/RF-OFF-01

```gherkin
Cenário: Serviço com internet caída
  Dado que a internet da loja está indisponível
  Quando garçom, cozinha e caixa operarem normalmente
  Então todos os pedidos devem ser criados, produzidos e pagos
  E os eventos devem ficar enfileirados para sincronização
  E os dispositivos devem indicar o estado offline de forma discreta
  E nenhuma funcionalidade operacional deve ficar bloqueada
```

### US-035 · Bloquear fechamento com item pendente — **S · 3** · RF-PED-06

---

## E-04 · KDS — cozinha

### US-040 · Fila de pedidos com cartões e cronômetro — **M · 13** · RF-KDS-02/03

```gherkin
Cenário: Escalonamento de cor
  Dado limiares de 12 min (atenção) e 18 min (crítico)
  Quando um pedido ultrapassar 12 minutos
  Então o cartão deve ficar amarelo
  E ao ultrapassar 18 minutos deve ficar vermelho com alerta sonoro

Cenário: Legibilidade
  Dado o KDS em monitor de 21 polegadas
  Quando houver 12 pedidos na fila
  Então o texto do produto deve ser legível a 1,5 metro de distância
```

### US-041 · Avançar estado com um toque via teclado numérico — **M · 8** · RF-KDS-04

```gherkin
Cenário: Operação sem mouse
  Dado um pedido com código curto 47 em estado QUEUED
  Quando o operador digitar 47 e pressionar Enter
  Então o item deve avançar para FIRED
  E a resposta visual deve ocorrer em menos de 300 ms
  E nenhuma digitação de texto deve ser necessária

Cenário: Código inexistente
  Quando o operador digitar um código sem correspondência
  Então deve haver retorno visual de erro sem travar a tela
```

### US-042 · Filtro por praça de produção — **M · 5** · RF-KDS-06
### US-043 · Contagem consolidada "all-day" — **S · 5** · RF-KDS-07
### US-044 · Marcar item indisponível pelo KDS — **M · 5** · RF-KDS-10
### US-045 · Alerta sonoro de pedido novo e de atraso — **M · 3** · RF-KDS-13
### US-046 · Histórico do turno — **S · 3** · RF-KDS-14
### US-047 · Modo pico (simplificação automática) — **C · 5**
### US-048 · Fallback de polling se WebSocket cair — **M · 3** · ADR-011

```gherkin
Cenário: Falha do canal em tempo real
  Dado que a conexão WebSocket do KDS caiu
  Quando um novo pedido for confirmado
  Então o KDS deve exibi-lo em no máximo 5 segundos via polling
  E deve indicar visualmente o modo degradado
```

---

## E-05 · Caixa e pagamento

### US-050 · Painel de mesas e comandas abertas — **M · 5** · RF-CXA-01
### US-051 · Conta montada automaticamente — **M · 8** · RF-CXA-02
### US-052 · Múltiplas formas de pagamento — **M · 8** · RF-CXA-03
### US-053 · Taxa de serviço configurável com retirada registrada — **M · 5** · RF-CXA-04
### US-054 · Desconto com autorização — **M · 5** · RF-CXA-05

```gherkin
Cenário: Desconto acima do limite
  Dado limite de 5% sem autorização
  Quando o operador aplicar 15%
  Então deve ser exigido PIN de perfil superior
  E o registro deve conter valor, motivo e autorizador
```

### US-055 · Abertura e fechamento de caixa — **M · 8** · RF-CXA-06/08

```gherkin
Cenário: Divergência no fechamento
  Dado esperado R$ 1.850,00 e contado R$ 1.843,50
  Quando o caixa for fechado
  Então a divergência de R$ 6,50 deve ser registrada
  E, acima do limiar, deve ser exigida justificativa
  E o gestor deve ser alertado
```

### US-056 · Sangria e suprimento — **S · 3** · RF-CXA-07
### US-057 · Comprovante não fiscal — **M · 3** · RF-CXA-12
### US-058 · Registrar pagamento de maquininha externa — **M · 3** · RF-CXA-10

---

## E-06 · Sincronização local ↔ nuvem

> Componente mais arriscado do MVP. Construído depois que o fluxo operacional já gera eventos reais.

### US-060 · Outbox transacional — **M · 8** · ADR-007

```gherkin
Cenário: Evento nunca se perde
  Dado que um pedido foi criado
  Quando a transação for confirmada
  Então o estado e o evento no outbox devem ter sido gravados juntos
  E, se o processo cair logo após, o evento deve continuar pendente
```

### US-061 · Worker de envio com retry e cursor — **M · 8**
### US-062 · Recepção idempotente na nuvem — **M · 8** · RF-OFF-03

```gherkin
Cenário: Reenvio de lote
  Dado um lote já processado
  Quando o mesmo lote for reenviado
  Então nenhum registro deve ser duplicado
  E a resposta deve informar a quantidade de duplicados ignorados
```

### US-063 · Pull de cardápio e configuração — **M · 5**
### US-064 · Preservação de occurredAt — **M · 5** · RF-OFF-04/RN-020

```gherkin
Cenário: Métrica após sincronização atrasada
  Dado um pedido feito às 20h03 offline
  E sincronizado às 21h15
  Quando o relatório por faixa horária for gerado
  Então o pedido deve ser contabilizado às 20h
```

### US-065 · Indicador de conexão e atraso — **M · 5** · RF-OFF-05
### US-066 · Alerta de atraso de sincronização — **M · 3** · RF-OFF-06
### US-067 · Registro e revisão de conflitos — **M · 5** · RF-OFF-07
### US-068 · Recuperação após reconexão longa — **M · 8**

```gherkin
Cenário: Retomada após 6 horas offline
  Dado 4.000 eventos acumulados no outbox
  Quando a conexão for restabelecida
  Então a sincronização deve ocorrer em lotes ordenados
  E concluir em menos de 5 minutos
  E o painel deve refletir todos os dados no horário correto de ocorrência
```

---

## E-07 · Painel do dono v1

### US-070 · Pulso em tempo real no celular — **M · 8** · RF-BI-01

```gherkin
Cenário: Cinco números essenciais
  Dado o gestor fora da loja no celular
  Quando abrir o painel
  Então deve ver faturamento do dia com comparativo, pedidos atrasados,
       tempo médio da última hora, ocupação de mesas e alertas abertos
  E a tela deve carregar em menos de 3 segundos
  E deve indicar o atraso de sincronização dos dados
```

### US-071 · Tempos por etapa com média e p90 — **M · 8** · RF-BI-02/03
### US-072 · Aderência ao prazo (OTD) — **M · 5** · RF-BI-04
### US-073 · Faturamento com comparativo — **M · 5** · RF-BI-05
### US-074 · Venda por canal, produto e categoria — **M · 5** · RF-BI-06
### US-075 · Ticket médio, giro de mesa e ocupação — **M · 5** · RF-BI-07
### US-076 · Drill-down até o pedido — **M · 8** · RF-BI-11

```gherkin
Cenário: Do gráfico ao pedido
  Dado o gráfico de tempo médio por hora
  Quando o gestor tocar na barra das 20h
  Então deve ver a lista de pedidos daquela hora
  E ao tocar em um pedido deve ver todos os seus carimbos de tempo
  E o caminho deve ter no máximo 3 toques
```

### US-077 · Resumo diário automático — **S · 3** · RF-BI-12

---

## E-08 · Alertas e notificações

### US-080 · Motor de alertas com limiares configuráveis — **M · 8** · RF-ALT-01/02
### US-081 · Entrega in-app e push de navegador — **M · 5** · RF-ALT-03
### US-082 · Direcionamento por perfil e ação — **M · 5** · RF-ALT-01

```gherkin
Cenário: Alerta só para quem age
  Dado um item pronto na janela de expedição
  Quando o alerta for disparado
  Então apenas o garçom responsável pela mesa deve ser notificado
  E cozinha e caixa não devem receber esse alerta
```

### US-083 · Agrupamento de alertas repetidos — **S · 3** · RF-ALT-04

---

## E-09 · Auditoria

### US-090 · Trilha imutável de ações sensíveis — **M · 8** · RF-AUD-01/02/04

```gherkin
Cenário: Registro completo
  Dado um desconto aplicado com autorização
  Quando a ação for concluída
  Então o log deve conter autor, autorizador, horário, dispositivo,
       valores antes e depois

Cenário: Imutabilidade
  Dado um registro de auditoria existente
  Quando qualquer usuário da aplicação tentar alterá-lo ou apagá-lo
  Então o banco deve recusar a operação
```

### US-091 · Consulta e filtro da trilha — **M · 5** · RF-AUD-03

---

# FASE 2 — CUSTO E CONTROLE

## E-10 · Estoque e ficha técnica

> Épico de maior retorno financeiro do projeto.

### US-100 · Cadastro de insumos e fornecedores — **M · 5** · RF-EST-01
### US-101 · Ficha técnica por variação — **M · 8** · RF-EST-02
### US-102 · Sub-receitas (massa, molho) — **S · 8** · RF-EST-03
### US-103 · Baixa automática na conclusão do item — **M · 13** · RF-EST-04/RN-007

```gherkin
Cenário: Baixa por ficha técnica
  Dado uma pizza com 180 g de mussarela na ficha
  Quando o item for marcado como pronto
  Então deve ser criado movimento de saída de 0,180 kg
  E o custo do item deve ser registrado pelo custo médio vigente

Cenário: Baixa proporcional em meio a meio
  Dado um item com duas frações de peso 0,5
  Quando concluído
  Então cada ficha deve baixar metade das quantidades
```

### US-104 · Entradas de compra com custo e validade — **M · 8** · RF-EST-05
### US-105 · Registro de perda com motivo — **M · 5** · RF-EST-06
### US-106 · Contagem cíclica e divergência — **M · 8** · RF-EST-07
### US-107 · CMV teórico × real — **M · 8** · RF-EST-08

```gherkin
Cenário: Divergência acima do limiar
  Dado CMV teórico de R$ 18.420 e real de R$ 19.870
  Quando o período for apurado
  Então a divergência de 7,9% deve ser exibida
  E, acima do limiar de 5%, o gestor deve ser alertado
  E a composição por insumo deve estar disponível
```

### US-108 · Alerta de estoque mínimo e validade — **M · 5** · RF-EST-09
### US-109 · Custo e margem por produto — **M · 8** · RF-EST-13
### US-110 · Matriz de engenharia de cardápio — **M · 8** · RF-BI-09

```gherkin
Cenário: Classificação em quadrantes
  Dado produtos com volume e margem apurados no período
  Quando a matriz for gerada
  Então cada produto deve ser classificado como
       Estrela, Cavalo de batalha, Quebra-cabeça ou Abacaxi
  E deve haver recomendação de ação por quadrante
```

## E-11 · Inteligência de fluxo

### US-115 · Fire time (sequenciamento reverso) — **S · 13** · RF-KDS-09

```gherkin
Cenário: Saída sincronizada
  Dado um pedido com pizza de 12 min e fritas de 5 min
  Quando o pedido for confirmado
  Então a pizza deve ser liberada para produção imediatamente
  E as fritas devem ser liberadas 7 minutos depois
  E o KDS deve indicar o momento de iniciar cada item
```

### US-116 · Prioridade dinâmica explicável — **S · 8** · RF-KDS-12
### US-117 · Indicador de ocupação do gargalo — **S · 8** · RF-KDS-08
### US-118 · Prazo dinâmico — **S · 8** · RF-PED-07
### US-119 · Mapa de calor de demanda — **S · 5** · RF-BI-08

---

# FASE 3 — FINANCEIRO

## E-12 · Financeiro de gestão

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| US-120 | Receita automática a partir de pagamentos | M | 5 | RF-FIN-01 |
| US-121 | Categorias de despesa e lançamentos | M | 5 | RF-FIN-02 |
| US-122 | Custos fixos recorrentes | M | 5 | RF-FIN-03 |
| US-123 | Folha de pagamento | M | 8 | RF-FIN-04 |
| US-124 | CMV, custo de pessoal e prime cost | M | 8 | RF-FIN-05 |
| US-125 | Ponto de equilíbrio | M | 5 | RF-FIN-06 |
| US-126 | Fluxo de caixa | S | 8 | RF-FIN-07 |
| US-127 | Resultado do período com composição | M | 8 | RF-FIN-08 |
| US-128 | Exportação para o contador | S | 3 | RF-FIN-09 |

---

# FASE 4 — DELIVERY

## E-13 · Delivery próprio

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| US-130 | Canal público de pedido com marca própria | M | 13 | RF-DEL-01 |
| US-131 | Zonas de entrega e taxa | M | 8 | RF-DEL-02 |
| US-132 | Prazo dinâmico ao cliente | M | 5 | RF-DEL-03 |
| US-133 | Acompanhamento de status | M | 5 | RF-DEL-04 |
| US-134 | Pagamento online (Mercado Pago) | M | 13 | RF-CXA-09 |
| US-135 | Endereço salvo e repetir pedido | S | 5 | RF-DEL-05 |
| US-136 | Atribuição e app do entregador | M | 8 | RF-DEL-06/07 |
| US-137 | Aviso de pedido próximo de sair | S | 3 | RF-DEL-08 |
| US-138 | Agrupamento de entregas | C | 8 | RF-DEL-09 |

---

# FASE 5 — ESCALA

## E-14 · Plataforma em escala

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| US-140 | Painel de instalações com saúde | M | 8 | RF-PLT-07 |
| US-141 | Provisionamento autoatendido | M | 8 | RF-PLT-05 |
| US-142 | Modelos por tipo de negócio | S | 8 | RF-PLT-06 |
| US-143 | Domínio próprio por cliente | S | 8 | RF-PLT-03 |
| US-144 | Importação de cardápio por planilha | S | 5 | RF-CAT-12 |
| US-145 | Acesso de suporte auditado | M | 5 | RF-PLT-08 |
| US-146 | Atualização controlada do parque | M | 5 | — |

---

## E-15 · Gestão geral da plataforma

| ID | História | Prio | Pts | RF |
|---|---|:-:|--:|---|
| US-150 | Estrutura e navegação do painel de plataforma | M | 5 | RF-PLT-09 |
| US-151 | Diretório de estabelecimentos com busca e filtros | M | 8 | RF-PLT-10 |
| US-152 | Visão 360 e acesso aos módulos do estabelecimento | M | 8 | RF-PLT-11 |
| US-153 | Ciclo de vida do estabelecimento | M | 8 | RF-PLT-12 |
| US-154 | Gestão de planos e configuração comercial | S | 8 | RF-PLT-13 |
| US-155 | Proprietários, usuários iniciais e convites | M | 8 | RF-PLT-14 |
| US-156 | Recuperação do provisionamento e token de instalação | M | 8 | RF-PLT-15 |
| US-157 | Central operacional, auditoria e atalhos de suporte | M | 5 | RF-PLT-16 |

```gherkin
Cenário: Administrador reencontra e administra um estabelecimento
  Dado que existem múltiplos estabelecimentos em diferentes estados
  Quando o administrador acessar a raiz da plataforma
  Então deve conseguir localizar um estabelecimento
  E consultar seu detalhe administrativo
  E executar somente ações permitidas, confirmadas e auditadas
  E nunca acessar dados de negócio do cliente sem o fluxo de suporte autorizado
```

> O detalhamento completo da E-15 está em `User Stories/E-15-Gestao-Geral-da-Plataforma/`. A E-15 compõe as capacidades da E-14; não substitui as histórias de saúde, suporte e atualização do parque.

---

## 2. Definition of Ready (DoR)

Uma história só entra em sprint quando:

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

## 3. Definition of Done (DoD)

Uma história só é concluída quando:

- [ ] Código revisado e aprovado
- [ ] Testes unitários dos casos de negócio
- [ ] Teste de integração do fluxo principal
- [ ] Teste de isolamento multi-tenant (quando aplicável)
- [ ] Eventos emitidos conforme o catálogo (doc. 04)
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste
- [ ] Sem violação do ADR-013 (código por tenant)
- [ ] Documentação atualizada (API/eventos/modelo)
- [ ] Observabilidade instrumentada
- [ ] Aprovada pelo PO

---

## 4. Riscos de backlog

| Risco | Mitigação |
|---|---|
| E-06 (sincronização) estourar a estimativa | Construir por último no MVP, com fluxo já estável; fatiar por domínio |
| E-10 depender de carga de dados do cliente | Iniciar a carga de fichas técnicas em paralelo à Fase 1 |
| E-04 exigir hardware ainda indefinido | Validar teclado numérico e monitor na Sprint 0 |
| Cliente pedir função fora do produto | Aplicar governança do ADR-013 e registrar a recusa |

---

*Documento 07 do pacote 004_DonaBetinha. Replay Studio.*
