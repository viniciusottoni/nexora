# 01 — PRD / Especificação Funcional
## Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | PRD — Especificação Funcional |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Autor** | Sáskia — Replay Studio |
| **Depende de** | `Visao-Geral-Sistema-Dona-Betinha.md`, `Otimizacao-Processos-Metricas-e-Experiencia-por-Usuario.md` |

---

## 1. Objetivo do produto

Entregar a estabelecimentos de alimentação um ecossistema único que **elimina a perda de pedido, mede cada etapa do processo, apura custo e margem reais e entrega ao dono controle e métrica total** — operando com ou sem internet e replicável, com identidade própria, em qualquer estabelecimento com as mesmas dores.

### 1.1 Problema

Estabelecimentos de pequeno e médio porte operam sem sistema: o pedido circula em papel e se perde, nenhuma etapa é cronometrada, o custo de produção é desconhecido e a saúde financeira é invisível. A gestão decide por intuição.

### 1.2 Proposta de valor

| Para | Que sofre com | O produto | Diferente de |
|---|---|---|---|
| Donos de pizzaria/restaurante | Pedido perdido, tempo desconhecido, custo invisível | Ecossistema operação + gestão + métrica | PDVs que só registram venda |
| — | Internet instável derrubando a operação | Arquitetura local-first: a loja não para | Sistemas 100% nuvem |
| — | Sistemas genéricos que não servem ao seu negócio | Produto configurável e white-label por estabelecimento | Software sob medida (caro e não evolui) |

---

## 2. Escopo do produto

### 2.1 Dentro do escopo (visão completa, distribuída em fases)

Operação de salão (mesa, comanda, garçom), cozinha (KDS), caixa, delivery próprio, estoque com ficha técnica, financeiro de gestão, painel de indicadores, administração, auditoria, operação offline com sincronização, plataforma multi-estabelecimento com personalização web.

### 2.2 Fora do escopo desta primeira etapa

| Item | Motivo |
|---|---|
| App de venda de frios (B2B/condomínios) | Segundo produto — decisão pendente |
| Integração com iFood | Depende de terceiro; avaliar custo/benefício |
| Emissão fiscal (NFC-e/SAT) | **[PENDÊNCIA]** — precisa de definição do cliente e contador |
| Programa de fidelidade e CRM | Fase posterior |
| Notificações por WhatsApp/SMS | Fase posterior — v1 usa push in-app |
| Multi-loja (rede com várias unidades) | Modelo de dados preparado, funcionalidade posterior |
| Internacionalização (idioma/moeda) | Modelo preparado, sem implementação |
| Reservas e fila de espera | Fase posterior |

---

## 3. Personas

| ID | Persona | Perfil | Contexto de uso | Dor central |
|---|---|---|---|---|
| **P1** | Cliente do salão | Público geral, familiaridade digital variada | Mesa, celular próprio, QR Code | Espera sem informação |
| **P2** | Garçom | Baixa/média familiaridade digital | Celular, em pé, em movimento | Anda para lançar e conferir |
| **P3** | Pizzaiolo / Cozinha | Mãos ocupadas, sujas, pressão | KDS fixo, teclado numérico | Pedido não chega ou chega ilegível |
| **P4** | Caixa | Média familiaridade | Terminal fixo | Monta conta na mão |
| **P5** | Entregador | Celular, na rua | Celular, capacete, pressa | Espera na loja sem saber quanto falta |
| **P6** | Cliente delivery | Público geral | Celular/web, em casa | Não sabe o prazo real |
| **P7** | Estoquista / comprador | Média familiaridade | Depósito, celular/desktop | Compra por palpite |
| **P8** | Gestor / dono | Decisor, quer resposta rápida | Celular, de qualquer lugar | Decide sem informação |
| **P9** | Admin de plataforma (Replay) | Técnico | Desktop | Implantar e suportar em escala |

---

## 4. Requisitos funcionais

> Notação: **RF-[módulo]-[nº]**. Prioridade: **M** (must, MVP), **S** (should), **C** (could), **W** (won't nesta etapa).

### 4.1 Plataforma e administração — RF-PLT

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-PLT-01 | O sistema deve suportar múltiplos estabelecimentos (tenants) com isolamento total de dados | M | 0 |
| RF-PLT-02 | Cada estabelecimento deve ter identidade visual própria (logo, cores, tipografia, ícone PWA, favicon) | M | 0 |
| RF-PLT-03 | Cada estabelecimento deve poder usar domínio ou subdomínio próprio | S | 5 |
| RF-PLT-04 | O sistema deve permitir configurar textos públicos (boas-vindas, confirmação, termos) por estabelecimento | S | 1 |
| RF-PLT-05 | O sistema deve permitir provisionar um novo estabelecimento sem alteração de código | M | 0 |
| RF-PLT-06 | O sistema deve oferecer modelos pré-configurados por tipo de negócio (pizzaria, hamburgueria, restaurante) | C | 5 |
| RF-PLT-07 | O admin da plataforma deve monitorar remotamente a saúde de cada instalação | S | 1 |
| RF-PLT-08 | Todo acesso de suporte a dados de um estabelecimento deve ser registrado e autorizado | M | 1 |

### 4.2 Identidade, usuários e permissões — RF-IAM

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-IAM-01 | O sistema deve permitir cadastro de usuários vinculados a um estabelecimento | M | 1 |
| RF-IAM-02 | O sistema deve suportar perfis (papéis) com conjunto configurável de permissões | M | 1 |
| RF-IAM-03 | Operadores de salão e cozinha devem poder autenticar por **PIN numérico** em dispositivo confiável | M | 1 |
| RF-IAM-04 | Gestor e administrativo devem autenticar por e-mail e senha, com opção de segundo fator | M | 1 |
| RF-IAM-05 | O sistema deve registrar dispositivos (terminais) autorizados por estabelecimento | M | 1 |
| RF-IAM-06 | O sistema deve encerrar sessões inativas conforme parâmetro configurável | S | 1 |
| RF-IAM-07 | Ações sensíveis devem exigir autorização de perfil superior (aprovação no próprio dispositivo) | M | 1 |

### 4.3 Cardápio e produtos — RF-CAT

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-CAT-01 | O sistema deve permitir cadastrar categorias e produtos com foto, descrição e ingredientes | M | 1 |
| RF-CAT-02 | Um produto deve poder ter variações (tamanhos) com preço próprio | M | 1 |
| RF-CAT-03 | O sistema deve suportar grupos de modificadores (adicionais, remoções, ponto de massa) com regra de mínimo/máximo e preço | M | 1 |
| RF-CAT-04 | O sistema deve suportar **pizza meio a meio** (e frações configuráveis: 2, 3 ou 4 sabores) | M | 1 |
| RF-CAT-05 | A regra de precificação do meio a meio deve ser configurável (maior valor, média, soma proporcional) | M | 1 |
| RF-CAT-06 | O sistema deve permitir preço distinto por canal (salão, delivery, marketplace) | M | 1 |
| RF-CAT-07 | O sistema deve permitir marcar produto como indisponível, refletindo em todos os canais imediatamente | M | 1 |
| RF-CAT-08 | O sistema deve permitir definir tempo de preparo padrão por produto/variação | M | 1 |
| RF-CAT-09 | O sistema deve permitir roteamento do produto para uma praça de produção | M | 1 |
| RF-CAT-10 | O sistema deve suportar cardápios com disponibilidade por horário/dia | C | 2 |
| RF-CAT-11 | O sistema deve permitir combos e promoções | C | 4 |
| RF-CAT-12 | O sistema deve permitir importar cardápio por planilha | S | 5 |

### 4.4 Salão, mesas e comandas — RF-SAL

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-SAL-01 | O sistema deve permitir cadastrar ambientes e mesas, com QR Code por mesa | M | 1 |
| RF-SAL-02 | O cliente deve abrir o cardápio ao ler o QR Code, sem instalar aplicativo e sem cadastro obrigatório | M | 1 |
| RF-SAL-03 | O cliente deve poder montar e enviar pedido pela própria mesa | M | 1 |
| RF-SAL-04 | O garçom deve poder abrir mesa e lançar pedido pelo próprio celular | M | 1 |
| RF-SAL-05 | O sistema deve exibir mapa de mesas com status e tempo decorrido | M | 1 |
| RF-SAL-06 | O cliente deve visualizar o consumo acumulado da mesa em tempo real | M | 1 |
| RF-SAL-07 | O cliente deve poder chamar o garçom, gerando alerta direcionado | M | 1 |
| RF-SAL-08 | O cliente ou o garçom deve poder solicitar a conta | M | 1 |
| RF-SAL-09 | O sistema deve permitir transferir itens entre mesas e unir/separar mesas | S | 2 |
| RF-SAL-10 | O sistema deve permitir dividir a conta por pessoa, por item ou por valor | M | 1 |
| RF-SAL-11 | O sistema deve permitir repetir item lançado com um toque | S | 1 |
| RF-SAL-12 | O sistema deve coletar avaliação do cliente ao fechar a conta | S | 2 |
| RF-SAL-13 | Cada pedido deve poder ser identificado ao cliente que o fez dentro da mesa | C | 2 |

### 4.5 Cozinha e produção (KDS) — RF-KDS

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-KDS-01 | O pedido confirmado deve aparecer no KDS em até 2 segundos | M | 1 |
| RF-KDS-02 | O KDS deve exibir cartão por pedido com cronômetro desde a confirmação | M | 1 |
| RF-KDS-03 | O cronômetro deve escalonar cor (verde/amarelo/vermelho) por limiares configuráveis por produto | M | 1 |
| RF-KDS-04 | O KDS deve permitir avançar o estado do item com **um toque** via teclado numérico | M | 1 |
| RF-KDS-05 | O KDS deve registrar os estados: em fila, iniciado, no forno, fora do forno, pronto | M | 1 |
| RF-KDS-06 | O KDS deve permitir filtro por praça de produção | M | 1 |
| RF-KDS-07 | O KDS deve exibir contagem consolidada de itens iguais na fila ("all-day") | S | 1 |
| RF-KDS-08 | O KDS deve indicar ocupação atual do recurso-gargalo (forno) e posições livres | S | 2 |
| RF-KDS-09 | O sistema deve calcular e exibir o momento de iniciar cada item para saída sincronizada (fire time) | S | 2 |
| RF-KDS-10 | O KDS deve permitir marcar item como indisponível, refletindo em todos os canais | M | 1 |
| RF-KDS-11 | O KDS deve permitir registrar refazimento (re-fire) com motivo | S | 2 |
| RF-KDS-12 | O KDS deve ordenar a fila por prioridade calculada, com ordem explicável e sobreponível pelo operador | S | 2 |
| RF-KDS-13 | O KDS deve emitir sinal sonoro configurável em pedido novo e em atraso | M | 1 |
| RF-KDS-14 | O KDS deve permitir consultar histórico do turno | S | 1 |
| RF-KDS-15 | O sistema deve sugerir agrupamento de itens idênticos sem prejudicar o pedido mais antigo | C | 3 |

### 4.6 Pedido (núcleo) — RF-PED

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-PED-01 | Todo pedido deve pertencer a um canal (salão, delivery, balcão, marketplace) | M | 1 |
| RF-PED-02 | Todo pedido e item deve registrar os carimbos de tempo T0 a T5 (ver doc. 04) | M | 1 |
| RF-PED-03 | Todo evento de pedido deve registrar autor, dispositivo e horário de ocorrência | M | 1 |
| RF-PED-04 | O sistema deve permitir cancelar item ou pedido, com motivo obrigatório e registro de autoria | M | 1 |
| RF-PED-05 | O cancelamento de item já iniciado deve exigir autorização de perfil superior | M | 1 |
| RF-PED-06 | O sistema deve impedir o fechamento de conta com item pendente de entrega, salvo autorização | S | 1 |
| RF-PED-07 | O sistema deve calcular prazo estimado dinâmico com base na fila atual | S | 2 |
| RF-PED-08 | O sistema deve suportar observações livres por item | M | 1 |
| RF-PED-09 | O pedido deve ser criado e operado integralmente offline na rede local | M | 1 |

### 4.7 Caixa e pagamento — RF-CXA

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-CXA-01 | O sistema deve exibir todas as mesas e comandas abertas com valor e tempo | M | 1 |
| RF-CXA-02 | O sistema deve montar a conta automaticamente a partir dos itens lançados | M | 1 |
| RF-CXA-03 | O sistema deve suportar múltiplas formas de pagamento na mesma conta | M | 1 |
| RF-CXA-04 | O sistema deve aplicar taxa de serviço conforme regra configurável, permitindo retirada registrada | M | 1 |
| RF-CXA-05 | Desconto acima do limite configurado deve exigir autorização de perfil superior | M | 1 |
| RF-CXA-06 | O sistema deve controlar abertura e fechamento de caixa, com conferência de valores | M | 1 |
| RF-CXA-07 | O sistema deve registrar sangria e suprimento | S | 1 |
| RF-CXA-08 | O sistema deve registrar divergência de fechamento e alertar acima do limite | M | 1 |
| RF-CXA-09 | O sistema deve integrar pagamento online (Mercado Pago) no delivery | M | 4 |
| RF-CXA-10 | O sistema deve permitir registrar pagamento em maquininha externa (Cielo/Mercado Pago) como forma de pagamento | M | 1 |
| RF-CXA-11 | O sistema deve conciliar recebimentos eletrônicos com o registrado | C | 3 |
| RF-CXA-12 | O sistema deve gerar comprovante não fiscal de consumo | M | 1 |
| RF-CXA-13 | O sistema deve permitir estorno com motivo e autorização | S | 2 |

### 4.8 Delivery — RF-DEL

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-DEL-01 | O cliente deve montar e enviar pedido pelo canal próprio do estabelecimento | M | 4 |
| RF-DEL-02 | O sistema deve calcular taxa de entrega por região/distância | M | 4 |
| RF-DEL-03 | O sistema deve informar prazo estimado dinâmico | M | 4 |
| RF-DEL-04 | O cliente deve acompanhar o status do pedido | M | 4 |
| RF-DEL-05 | O sistema deve permitir salvar endereço e repetir pedido anterior | S | 4 |
| RF-DEL-06 | O sistema deve atribuir entregas a entregadores | M | 4 |
| RF-DEL-07 | O entregador deve registrar saída e entrega pelo celular | M | 4 |
| RF-DEL-08 | O sistema deve avisar o entregador quando o pedido estiver próximo de sair | S | 4 |
| RF-DEL-09 | O sistema deve sugerir agrupamento de entregas próximas | C | 4 |
| RF-DEL-10 | O sistema deve registrar ocorrências de entrega com motivo | S | 4 |
| RF-DEL-11 | O sistema deve pausar automaticamente o canal quando a fila exceder o limite configurado | C | 4 |

### 4.9 Estoque e ficha técnica — RF-EST

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-EST-01 | O sistema deve permitir cadastrar insumos com unidade de medida, custo e fornecedor | M | 2 |
| RF-EST-02 | O sistema deve permitir cadastrar ficha técnica por produto/variação | M | 2 |
| RF-EST-03 | A ficha técnica deve suportar sub-receitas (ex.: massa, molho) | S | 2 |
| RF-EST-04 | O sistema deve dar baixa automática de insumo ao concluir a produção do item | M | 2 |
| RF-EST-05 | O sistema deve registrar entradas de estoque (compra/recebimento) com custo e validade | M | 2 |
| RF-EST-06 | O sistema deve registrar perdas com motivo classificado | M | 2 |
| RF-EST-07 | O sistema deve suportar contagem cíclica de inventário e apurar divergência | M | 2 |
| RF-EST-08 | O sistema deve calcular CMV teórico e real e a divergência entre eles | M | 2 |
| RF-EST-09 | O sistema deve alertar estoque mínimo e validade próxima | M | 2 |
| RF-EST-10 | O sistema deve sugerir lista de compras com base em consumo e cobertura | S | 3 |
| RF-EST-11 | O sistema deve manter histórico de custo por insumo e fornecedor | S | 2 |
| RF-EST-12 | O sistema deve bloquear venda de produto sem insumo disponível, conforme configuração | S | 2 |
| RF-EST-13 | O sistema deve calcular custo de produção e margem por produto | M | 2 |

### 4.10 Financeiro — RF-FIN

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-FIN-01 | O sistema deve registrar receitas por canal automaticamente a partir das vendas | M | 3 |
| RF-FIN-02 | O sistema deve permitir cadastrar categorias de despesa | M | 3 |
| RF-FIN-03 | O sistema deve registrar custos fixos recorrentes (aluguel, impostos, CMO) | M | 3 |
| RF-FIN-04 | O sistema deve registrar folha de pagamento e encargos | M | 3 |
| RF-FIN-05 | O sistema deve apurar CMV, custo de pessoal e prime cost | M | 3 |
| RF-FIN-06 | O sistema deve calcular ponto de equilíbrio | M | 3 |
| RF-FIN-07 | O sistema deve apresentar fluxo de caixa realizado e projetado | S | 3 |
| RF-FIN-08 | O sistema deve apurar resultado do período com composição | M | 3 |
| RF-FIN-09 | O sistema deve exportar dados para o contador | S | 3 |
| RF-FIN-10 | O sistema deve registrar custo de taxa de cartão por transação | S | 3 |

### 4.11 Indicadores e painel do dono — RF-BI

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-BI-01 | O sistema deve exibir painel de tempo real (pulso) acessível por celular | M | 1 |
| RF-BI-02 | O sistema deve calcular tempos por etapa (fila, produção, cocção, expedição, total) | M | 1 |
| RF-BI-03 | O sistema deve apresentar média e percentil 90 dos tempos | M | 1 |
| RF-BI-04 | O sistema deve calcular aderência ao prazo (OTD) | M | 1 |
| RF-BI-05 | O sistema deve apresentar faturamento com comparativo do período anterior | M | 1 |
| RF-BI-06 | O sistema deve apresentar venda por canal, produto e categoria | M | 1 |
| RF-BI-07 | O sistema deve calcular ticket médio, giro de mesa e ocupação | M | 1 |
| RF-BI-08 | O sistema deve apresentar mapa de calor de demanda por dia e hora | S | 2 |
| RF-BI-09 | O sistema deve gerar a matriz de engenharia de cardápio (volume × margem) | M | 2 |
| RF-BI-10 | O sistema deve permitir definir metas e acompanhar realizado × meta | S | 2 |
| RF-BI-11 | Todo indicador deve permitir navegação até o pedido de origem em até 3 cliques | M | 1 |
| RF-BI-12 | O sistema deve enviar resumo diário automático ao gestor | S | 1 |
| RF-BI-13 | O sistema deve permitir exportar qualquer visão (planilha/PDF) | S | 2 |
| RF-BI-14 | O painel deve indicar explicitamente o atraso de sincronização dos dados | M | 1 |

### 4.12 Alertas e notificações — RF-ALT

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-ALT-01 | O sistema deve notificar cada perfil apenas sobre eventos que exigem ação dele | M | 1 |
| RF-ALT-02 | Os limiares de alerta devem ser configuráveis por estabelecimento | M | 1 |
| RF-ALT-03 | O sistema deve entregar notificações in-app e push de navegador | M | 1 |
| RF-ALT-04 | O sistema deve agrupar alertas repetidos para evitar ruído | S | 2 |
| RF-ALT-05 | O sistema deve permitir silenciar tipos de alerta por usuário | S | 2 |
| RF-ALT-06 | O sistema deve medir a taxa de alertas ignorados por tipo | C | 3 |

### 4.13 Offline e sincronização — RF-OFF

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-OFF-01 | Mesa, KDS e caixa devem operar integralmente sem internet, na rede local | M | 1 |
| RF-OFF-02 | O sistema deve enfileirar eventos localmente e sincronizar automaticamente ao restabelecer conexão | M | 1 |
| RF-OFF-03 | A sincronização deve ser idempotente — reenvio não pode duplicar registro | M | 1 |
| RF-OFF-04 | O sistema deve preservar o horário de ocorrência, distinto do horário de registro na nuvem | M | 1 |
| RF-OFF-05 | O sistema deve exibir indicador de estado da conexão e do atraso de sincronização | M | 1 |
| RF-OFF-06 | O sistema deve alertar gestor e plataforma quando o atraso exceder o limite | M | 1 |
| RF-OFF-07 | Conflitos de sincronização devem seguir regra explícita e ficar registrados para revisão | M | 1 |
| RF-OFF-08 | Dispositivos devem manter cache do cardápio para operação em contingência | S | 2 |

### 4.14 Auditoria — RF-AUD

| ID | Requisito | Prio | Fase |
|---|---|:-:|:-:|
| RF-AUD-01 | O sistema deve manter trilha imutável de ações sensíveis com autor, horário, dispositivo e valores antes/depois | M | 1 |
| RF-AUD-02 | A trilha deve cobrir: cancelamento, desconto, alteração de preço, movimentação de estoque, ajuste financeiro, caixa e permissões | M | 1 |
| RF-AUD-03 | A trilha deve ser consultável e filtrável pelo gestor | M | 1 |
| RF-AUD-04 | Nenhum usuário deve poder alterar ou apagar registros de auditoria | M | 1 |

---

## 5. Regras de negócio

> Notação **RN-xxx**. Regras marcadas **[PENDÊNCIA]** exigem definição do cliente.

| ID | Regra | Origem |
|---|---|---|
| RN-001 | Todo pedido confirmado é roteado simultaneamente para cozinha e caixa | [FATO] |
| RN-002 | A cozinha registra obrigatoriamente início e conclusão de cada item | [FATO] |
| RN-003 | Cada transição de estado gera alerta aos perfis envolvidos | [FATO] |
| RN-004 | Toda ação registra autor, horário e dispositivo | [FATO] |
| RN-005 | A operação local não depende de internet; a nuvem consolida | [FATO] |
| RN-006 | Cada produto possui ficha técnica que determina a baixa de insumo | [FATO] |
| RN-007 | A baixa de estoque ocorre na **conclusão da produção** do item, não no lançamento do pedido | [HIPÓTESE] |
| RN-008 | Item cancelado após início da produção **não** estorna insumo; gera registro de perda | [HIPÓTESE] |
| RN-009 | O preço do meio a meio segue regra configurável; padrão sugerido: **maior valor entre as frações** | [HIPÓTESE] |
| RN-010 | Taxa de serviço é opcional ao cliente; a retirada é registrada e auditada | [HIPÓTESE] |
| RN-011 | Desconto acima do limite configurado exige autorização de perfil superior | [HIPÓTESE] |
| RN-012 | Produto sem insumo disponível é bloqueado em todos os canais simultaneamente | [HIPÓTESE] |
| RN-013 | O prazo informado ao cliente é calculado pela fila atual, nunca fixo | [HIPÓTESE] |
| RN-014 | Itens do mesmo pedido devem sair sincronizados; o sistema calcula o início de cada um | [HIPÓTESE] |
| RN-015 | Nenhum dado de um estabelecimento é acessível a outro, em nenhuma circunstância | [FATO — diretriz] |
| RN-016 | Regra específica de negócio deve existir como configuração, nunca como código de cliente | [FATO — diretriz] |
| RN-017 | Conta não pode ser fechada com item pendente de entrega, salvo autorização registrada | [HIPÓTESE] |
| RN-018 | Caixa não pode ser fechado com mesa aberta, salvo autorização registrada | [HIPÓTESE] |
| RN-019 | Em conflito de sincronização, prevalece o evento com menor `ocorrido_em`; empate resolve por origem local | [HIPÓTESE] |
| RN-020 | Métrica de horário usa sempre `ocorrido_em`, nunca o horário de sincronização | [FATO — diretriz] |
| RN-021 | Regra de precificação diferenciada por canal/público | **[PENDÊNCIA]** |
| RN-022 | Política de estorno e cancelamento de pagamento | **[PENDÊNCIA]** |
| RN-023 | Emissão de documento fiscal | **[PENDÊNCIA]** |

---

## 6. Fluxos principais (resumo)

Detalhamento completo em `Otimizacao-Processos-Metricas-e-Experiencia-por-Usuario.md`, seções 9.1 a 9.3, e máquinas de estado no documento 04.

**Salão:** abertura de mesa → pedido (cliente ou garçom) → roteamento cozinha+caixa → produção cronometrada → pronto → entrega → conta → pagamento → fechamento.

**Delivery:** pedido online → pagamento → produção → despacho → entrega → conclusão.

**Retaguarda:** compra → entrada de estoque → venda → baixa por ficha técnica → custo → margem → resultado → indicador.

---

## 7. Métricas de sucesso do produto

| Métrica | Baseline | Meta | Prazo |
|---|---|---|---|
| Pedidos perdidos entre salão e cozinha | Recorrente | Zero | Fase 1 |
| Tempo total do pedido no salão (p90) | Desconhecido | ≤ 10 min | Fase 1 + 60 dias |
| Tempo total do delivery (p90) | Desconhecido | ≤ 25 min | Fase 4 + 60 dias |
| Aderência ao prazo (OTD) | Desconhecido | ≥ 85% | Fase 2 |
| Produtos com ficha técnica cadastrada | 0% | 100% | Fase 2 |
| Divergência CMV teórico × real | Desconhecido | ≤ 5% | Fase 2 + 90 dias |
| Adoção do KDS (pedidos processados no sistema) | 0% | ≥ 98% | Fase 1 + 30 dias |
| Tempo de implantação de novo estabelecimento | N/A | ≤ 5 dias úteis | Fase 5 |

---

## 8. Dependências e premissas

**Premissas**

1. O estabelecimento disponibiliza rede local cabeada ou Wi-Fi estável na área operacional
2. Haverá servidor local dedicado por loja, com nobreak
3. O cliente disponibiliza pessoa responsável pela carga de cardápio e fichas técnicas
4. O piloto ocorre com acompanhamento presencial nas duas primeiras semanas

**Dependências externas**

| Dependência | Impacto se falhar |
|---|---|
| Credenciais Mercado Pago | Bloqueia RF-CXA-09 (Fase 4) |
| Definição fiscal | Bloqueia lançamento em produção legal |
| Hardware local e rede | Bloqueia toda a operação |
| Identidade visual do cliente | Bloqueia personalização (RF-PLT-02) |

---

## 9. Rastreabilidade requisito → dor

| Dor da descoberta | Requisitos que a resolvem |
|---|---|
| "O pedido é feito e não chega para cozinha" | RF-PED-01/02, RF-KDS-01, RF-SAL-03/04 |
| "Quantos minutos minha pizza tá sendo feita" | RF-PED-02, RF-KDS-02/05, RF-BI-02/03 |
| "Não sei quais etapas são mais rápidas e lentas" | RF-BI-02, RF-KDS-08, RF-BI-08 |
| "Não sei quanto é necessário para fazer cada pizza" | RF-EST-02/04/13 |
| "Não sei quais foram as entradas" | RF-EST-05/07/08 |
| "Saber a saúde financeira" | RF-FIN-05/06/08, RF-BI-09 |
| "Não ter papel passando" | RF-SAL-03/04, RF-KDS-01, RF-CXA-02 |
| "Funcionar sem internet" | RF-OFF-01 a 08 |
| "Alerta para cada usuário" | RF-ALT-01 a 03 |
| "Registrar quem fez cada ação" | RF-AUD-01 a 04 |
| "Implantar em qualquer estabelecimento" | RF-PLT-01 a 08 |

---

*Documento 01 do pacote 004_DonaBetinha. Replay Studio.*
