# Nexora — Design System

**Nexora · Plataforma de Gestão Inteligente** é um produto multi-estabelecimento
(*multi-tenant*, white-label) para operação e gestão de negócios de alimentação:
o pedido nasce digital na mesa, atravessa cozinha e caixa cronometrado, baixa
insumo pela ficha técnica e chega ao dono como indicador — funcionando **com ou
sem internet**.

A primeira instância é a pizzaria **Dona Betinha** (projeto interno `004_DonaBetinha`,
Replay Studio). Nexora é a plataforma; Dona Betinha é um *tenant* dela. Essa distinção
governa todo o design system: **a plataforma tem uma marca (navy/verde), cada instância
tem a sua** — e a UI se veste da marca do tenant em runtime.

---

## Fontes deste design system

Nenhum código-fonte, repositório ou arquivo Figma foi fornecido. O sistema foi derivado de:

| Fonte | Onde | O que forneceu |
|---|---|---|
| `uploads/logo.jpeg` | copiado para `assets/logo-nexora-horizontal.jpeg` | Único ativo visual existente: wordmark + símbolo, gradiente navy → azul → ciano → verde |
| `uploads/Visao-Geral-Sistema-Dona-Betinha.md` (v1.1) | doc de visão | Ecossistema, módulos M1–M11, perfis, personalização, offline-first |
| `uploads/Otimizacao-Processos-Metricas-e-Experiencia-por-Usuario.md` | doc de operação | Desenho por usuário, KDS, timestamps T0–T5, métricas, anti-padrões de UX |
| `uploads/01-PRD-Especificacao-Funcional.md` | PRD | Requisitos RF-* citados nos componentes e kits |
| `uploads/02-Arquitetura-Tecnica.md` | arquitetura | **Tokens declarados:** `--brand-primary #C1121F`, `--brand-secondary #669BBC`, `--brand-surface #FDF0D5`, `--brand-font 'Inter'`, `--brand-radius 12px`; theming em runtime via CSS custom properties |
| `uploads/04-Catalogo-de-Eventos-e-Maquinas-de-Estado.md` | eventos | Nomes e cores canônicas dos estados (`QUEUED`, `FIRED`, `IN_OVEN`, `READY`, `SERVED`, `BILL_REQUESTED`, `PAID`, `DISPATCHED`…) |
| `uploads/05` a `10` | API, RNF, roadmap, testes, dados, ADRs | Perfis de autenticação (PIN × e-mail), densidades, limiares de alerta |

> **Nada foi recriado a partir de captura de tela** — não havia nenhuma. Onde a
> especificação não define aparência, o design system decide e **declara a decisão**
> nas seções abaixo; onde nem isso é possível (fotos de produto), usa placeholder explícito.

### Substituições que precisam da sua confirmação

1. **Fonte de interface — Inter.** Declarada na arquitetura; nenhum arquivo foi enviado. Carregada do Google Fonts.
2. **Fonte de display — Montserrat ExtraBold.** O wordmark do logo é um sans geométrico pesado; Montserrat é a aproximação mais próxima disponível. **Envie o arquivo original se existir.**
3. **Fonte numérica — JetBrains Mono.** Escolha nossa: tempo, dinheiro e peso precisam de mono tabular para colunas alinharem. Não estava especificada.
4. **Iconografia — Material Symbols Rounded.** Nenhum set foi fornecido; escolhido pelo traço arredondado coerente com as terminações do símbolo do logo.
5. **Logo — vetorizado por nós.** O cliente forneceu um único JPEG horizontal sobre branco. Nós **traçamos** esse bitmap: o contorno de cada forma (símbolo, letras de NEXORA, assinatura) virou path SVG e as paradas de gradiente foram amostradas da própria arte — não é redesenho no olho, é o mesmo desenho em vetor. Saída em `assets/`: `logo-nexora-{horizontal,lockup,symbol}[-white].svg` (com assinatura, sem assinatura, só símbolo, cada um colorido e todo branco). **Confirme o vetor** e envie o original se ele existir; ainda **faltam** favicon e ícones PWA em PNG.

   **Regra de fundo, sem exceção:** fundo branco/claro → marca colorida; fundo navy ou azul da marca → marca branca. Em cartão de **login** e de **primeiro acesso**, marca colorida centralizada no topo. Em React use `BrandMark`/`NexoraLogo`, que trazem o SVG inline e decidem a versão por `inverse`/`tone`.

---

## CONTENT FUNDAMENTALS — como a copy é escrita

**Idioma:** português do Brasil, sempre. Nenhum termo em inglês na interface, mesmo
quando o código usa (`IN_OVEN` no evento, "No forno" na tela).

**Pessoa e voz.** O sistema fala **do sistema para o usuário**, na terceira pessoa dos
fatos, e usa "você" só quando o assunto é do usuário ("Seu pedido", "Meu ticket médio").
Botões são **verbos no infinitivo** do que vai acontecer: "Enviar pedido", "Pedir a conta",
"Fechar caixa", "Provisionar". Nunca "Submeter", "Confirmar operação", "OK".

**Casing.** Frase capitalizada em tudo (`Enviar pedido`, não `Enviar Pedido`). CAPS só em
*overline* de 11px (rótulo de coluna e de KPI) e no wordmark.

**Números carregam contexto.** Número solto é proibido por regra de produto. Toda métrica
sai acompanhada de comparativo ou meta:

> "R$ 4.180 · +12,4% vs. mesma terça"
> "11:40 · meta ≤ 10 min"
> "82% · meta 85%"
> "Teórico 41,2 kg × real 36,8 kg"

**Alerta = fato + consequência + ação.** Nunca só o fato, nunca só o alarme:

> "3 pedidos acima da meta — Pico das 21h com 1 pizzaiolo na montagem." + botão "Ver fila"
> "Forno ocioso com fila há 4 min — 2 posições livres e 6 pedidos esperando: perda de capacidade." + "Ver KDS"

**Honestidade sobre o estado do sistema.** Nada de otimismo falso. "Modo local · 38 na fila"
em vez de esconder a queda; "prazo recalculado pela fila" em vez de um número de marketing;
"Sync atrasada · há 14 min" no painel do dono, porque dado defasado nunca pode passar por tempo real.

**Erros nomeiam a coisa e o caminho.** "Acima do limite — exige autorização", não "Ops! Algo
deu errado". "Divergência de R$ 12,00 em dinheiro" + "Justificar", não "Divergência detectada
no inventário".

**Sem emoji. Sem exclamação. Sem gamificação.** É um instrumento de gestão de um negócio
que perde dinheiro quando o pedido atrasa. Não celebra; informa. A única exceção de tom mais
quente é o PWA do cliente na mesa, que é a cara do estabelecimento — e ainda assim sem emoji.

**Rótulos do domínio, não de software.** "Comanda", "praça de produção", "ficha técnica",
"sangria", "CMV", "meio a meio", "fire time / montar". Vocabulário de restaurante, porque é
quem vai usar. Termos técnicos (tenant, idempotência) só na tela do admin da plataforma.

**Métrica individual é privada.** O garçom vê o próprio desempenho ("Meu ticket médio"); a
gestão vê o agregado. Nenhum ranking exposto — é regra escrita na especificação, e é regra de copy.

---

## VISUAL FOUNDATIONS

### Cor

A rampa vem do **gradiente do logo**: navy profundo → azul → ciano → verde-limão. Navy é a
cor institucional da plataforma (`--action-primary`, navegação, superfícies invertidas);
o **verde** é a cor de confirmação da operação (`--action-accent` — "Pronto", "Fechar conta");
ciano/teal é o acento analítico (barras, ocupação, all-day).

- **Neutros** têm viés frio de navy (`#F6F8FB` → `#101C2E`) — nunca cinza puro, nunca cinza quente.
- **Semânticos** são conservadores: `#28A55C` sucesso, `#E8A302` atenção, `#C42026` erro, azul da marca como info. Cada um tem par de fundo sutil (`*-100`) para banner e badge.
- **Escalonamento de tempo** é uma linguagem de cor própria e sagrada: verde → amarelo → vermelho, por limiar configurável por produto. Aparece no cronômetro, no cartão do KDS, no forno e em qualquer SLA. Nunca use essas três cores para outra coisa.
- **Estados** têm cor canônica fixa (`StatusPill`). Um estado = uma cor em todo o ecossistema; reatribuir cor entre telas quebra a leitura de operação.
- **Gradiente da marca** (`--nx-gradient-brand`) é permitido **só** em marca, capa e hero. Nunca atrás de conteúdo, dado ou formulário — a especificação é de um produto de leitura, e gradiente atrás de número é ruído. Fundos de tela são chapados: `#F6F8FB` na gestão, branco no card, navy no invertido.
- **Camada de tenant.** `--brand-primary`, `--brand-secondary`, `--brand-surface`, `--brand-radius` e `--brand-font` são sobrescritos por instância (`tokens/tenants.css`, escopo `[data-tenant="…"]`). Componentes de ação consomem `--brand-*`; estados e semânticos **não** — a leitura de "atrasado" não pode depender da marca do cliente.

### Tipografia

Três famílias, papéis não sobrepostos:

- **Montserrat ExtraBold** — marca, wordmark, número-herói, identificação de mesa. Só isso.
- **Inter** — toda a interface. 14px é o corpo de gestão; **16px em mesa e garçom** (leitura em movimento, em pé).
- **JetBrains Mono tabular** — todo dinheiro, tempo, peso, quantidade e código de pedido. Não é estética: é para colunas alinharem e o olho comparar valores.

Escala: 11 · 12 · 13 · 14 · 16 · 18 · 20 · 24 · 28 · 34 · 42 · 56 · 72. Entrelinha aperta
conforme o tamanho cresce (1,45 no corpo → 1,08 no display). *Tracking* negativo (−0,02em) só
acima de 28px; +0,08em no overline em caps.

**Mínimos de legibilidade do KDS** (lido a 1,5 m, com calor e pressão): item 24px, código do
pedido 28px, cronômetro 42px. Nunca reduza para caber mais — reduza a quantidade de informação.

### Espaço, densidade e layout

Base 4px, com 2px e 6px para ajuste fino de controle. **Três densidades declaradas**, e nunca
misturadas na mesma tela:

| Densidade | Alvo | Onde |
|---|---|---|
| Toque grande | 64px | Botão principal de operação (mesa, garçom, KDS, teclado) |
| Toque mínimo | 48px | Todo controle tocável em mesa/garçom/KDS |
| Gestão | 36px controle · 40px linha de tabela | Caixa, painel do dono, admin |

Layout: navegação lateral fixa de 248px (navy), barra superior fixa de 56px, conteúdo com
scroll próprio e padding de 24px. Só esses dois elementos são fixos — nada de cabeçalho
flutuante sobre dado. Grades: mesas `auto-fill minmax(190px,1fr)`, tickets do KDS
`auto-fill minmax(316px,1fr)`, KPIs em 4 ou 5 colunas iguais.

### Fundo, imagem e textura

Sem imagem de fundo, sem textura, sem padrão repetido, sem ilustração — não existe nenhum
ativo desse tipo nas fontes, e um produto operacional não os pede. O fundo é chapado.
**Não desenhamos ilustração nem ícone à mão** em nenhum lugar deste sistema.

Fotografia aparece em um único lugar: **foto de produto no cardápio**, fornecida pelo
estabelecimento. Como nenhuma foi entregue, os kits mostram um placeholder que diz isso
literalmente ("foto do produto — a fornecer pelo estabelecimento"). Quando existirem, a
direção pedida é: comida em luz natural quente, sem filtro, sem grão, enquadramento próximo.

### Borda, canto e sombra

- **A hierarquia vem da borda, não da sombra.** Todo card é `1px solid var(--border-subtle)` + sombra rasa. Sombras são navy-tintadas e discretas: `subtle` (1px) → `card` (3px) → `raised` (12px, só em hover e popover) → `overlay` (40px, só em modal).
- **Raio 12px** (`--radius-brand`) é o valor declarado na arquitetura e é token de tenant — o white-label pode mudá-lo. Controles usam 10px, chips e pílulas 999px, tags de código 6px.
- **Proibido:** card com borda colorida só à esquerda, card com sombra colorida, borda de 2px como decoração. Borda 2px existe só em foco, em linha de total e em contorno de atraso.
- Sem *glassmorphism*: transparência e blur aparecem apenas no véu de modal (`rgba(7,23,49,.55)`), nunca em card ou barra.

### Estados de interação

- **Hover:** superfície sobe um degrau (`sunken` no ghost, `raised` na sombra do card interativo) e a borda escurece um passo. Botão sólido escurece 1 tom — nunca clareia, nunca muda de matiz.
- **Press:** `translateY(1px)` + o tom mais escuro. Sem *scale*, sem *ripple*.
- **Foco:** anel de 3px `rgba(31,111,208,.35)` em `:focus-visible`, sempre — o KDS e o caixa são operados por teclado.
- **Disabled:** opacidade 0,45, cursor `not-allowed`. Nunca esconda a ação: o operador precisa ver que existe e está bloqueada.
- **Selecionado:** fundo `--surface-brand-subtle` + peso 600 + ícone `FILL 1`.

### Movimento

Discreto e curto: 120ms nos controles, 200ms padrão, 320ms em barra de progresso.
Easing único, `cubic-bezier(.2,.8,.2,1)`. Sem bounce, sem spring, sem entrada em cascata,
sem número animado por contador — o dado aparece pronto.

Duas animações contínuas, ambas com função: **pulso de 1,2s** no cronômetro atrasado e no
`StatusPill live` (exigem ação agora). Nada mais se move sozinho. Tudo respeita
`prefers-reduced-motion` (as durações vão a zero).

---

## ICONOGRAPHY

**Material Symbols Rounded**, do Google Fonts, carregado por `tokens/fonts.css` —
substituição sinalizada, já que nenhum conjunto foi fornecido. Motivo da escolha: traço
arredondado, coerente com as terminações redondas do símbolo do logo, e é fonte variável
(FILL, wght, opsz) — o que resolve estado ativo sem trocar de arquivo.

Regras:

- Sempre pelo componente `Icon` — **nunca SVG escrito à mão, nunca emoji, nunca caractere unicode como ícone.**
- Tamanhos: **20px** em gestão, **24px** em operação de toque, **32–40px** no KDS. `opsz` acompanha o tamanho automaticamente.
- Peso 400 padrão; 500 só quando o ícone é o único conteúdo de um alvo grande.
- `FILL 1` **apenas** em item de navegação selecionado e em ícone de alerta dentro de banner. Em qualquer outro lugar, outline.
- Ícone é decorativo por padrão (`aria-hidden`); passa `label` quando carrega significado sozinho (aí ganha `role="img"`).
- Vocabulário fixo do domínio: `table_restaurant` mesa · `outdoor_grill` cozinha · `local_fire_department` forno · `timer` tempo · `point_of_sale` caixa · `delivery_dining` delivery · `inventory_2` estoque · `insights` métrica · `receipt_long` comanda/conta · `wifi_off` offline · `sync_problem` sync atrasada · `storefront` instância.
- **Emoji: nunca**, em nenhum produto, incluindo o PWA do cliente.

Ativos em `assets/`: `logo-nexora-horizontal.jpeg` (único fornecido pelo cliente) e os
vetores traçados dele — `logo-nexora-{horizontal,lockup,symbol}[-white].svg`. Sem
biblioteca de ilustrações, sem imagens genéricas — nada foi inventado.

---

## Índice do repositório

### Raiz
| Arquivo | O que é |
|---|---|
| `styles.css` | Entrada única de CSS — só `@import`. É o arquivo que projetos consumidores linkam. |
| `readme.md` | Este guia. |
| `SKILL.md` | Empacotamento como Agent Skill. |
| `thumbnail.html` | Tile do design system. |
| `tokens/` | `fonts.css`, `colors.css`, `typography.css`, `spacing.css`, `shape.css`, `motion.css`, `surfaces.css` (aliases semânticos), `tenants.css` (white-label), `base.css` (reset). |
| `guidelines/` | 18 cards de especímen que populam a aba Design System (grupos Colors, Type, Spacing, Brand). |
| `assets/` | `logo-nexora-horizontal.jpeg` (original do cliente) + os vetores `logo-nexora-{horizontal,lockup,symbol}[-white].svg`. |
| `components/` | Primitivos React (abaixo). |
| `ui_kits/` | Recreações de tela por produto (abaixo). |

### Components

Cada diretório tem `<Name>.jsx` + `<Name>.d.ts` + `<Name>.prompt.md` e um card HTML de variantes.

**`components/core/`** — `Button`, `IconButton`, `Badge`, `Card`, `Icon`, `BrandMark`
**`components/forms/`** — `Field`, `Input`, `Select`, `Checkbox`, `Switch`, `QuantityStepper`, `NumericKeypad`
**`components/data/`** — `StatTile`, `ProgressMeter`, `DataTable`
**`components/feedback/`** — `StatusPill`, `OrderTimer`, `AlertBanner`, `SyncStatus`, `EmptyState`
**`components/navigation/`** — `SideNav`, `TopBar`, `SegmentedControl`
**`components/operacao/`** — `TableCard`, `OrderTicket`, `MenuItemCard`, `OrderLine`

Uso: `const { Button, StatusPill } = window.NexoraDesignSystem_aa692a`.
`components/nx-css.js` é um utilitário interno (injeção de folha de estilo), não um componente.

#### Adições intencionais
Não havia biblioteca de componentes nas fontes, então o conjunto foi autorado a partir dos
requisitos. Cinco componentes existem por exigência escrita do produto e não fariam parte de
um kit genérico:

- `NumericKeypad` — PIN de operador (RF-IAM-03) e comando do KDS sem mouse (RF-KDS-04).
- `OrderTimer` — cronômetro com escalonamento por limiar configurável (RF-KDS-03).
- `SyncStatus` — estado de conexão e atraso de sincronização (RF-OFF-05, RF-BI-14).
- `StatusPill` — cores canônicas das máquinas de estado do doc 04.
- `BrandMark` — decide colorido × branco pelo fundo e resolve o caso "tenant sem logo".
- `NexoraLogo` — a marca crua em SVG inline (`lockup`, `symbol`; `color`, `white`).
- `NexoraLoader` / `NexoraSplash` — espera padrão da plataforma: a moeda da marca quica e
  gira 360°; `NexoraSplash` é obrigatório antes de cartão de login e de primeiro acesso.

`Icon` é um invólucro do conjunto de glifos, e `Field` existe para não repetir rótulo/erro
em cada controle.

### UI kits

| Kit | Produto | Perfil | Tela típica |
|---|---|---|---|
| `ui_kits/mesa/` | PWA do cliente na mesa (marca do tenant) | Cliente do salão | Cardápio → meio a meio → pedido → acompanhar → consumo |
| `ui_kits/garcom/` | App do garçom | Garçom | PIN → mapa de mesas → comanda → lançamento |
| `ui_kits/kds/` | KDS de cozinha (superfície escura) | Cozinha | Fila de tickets + forno + all-day + comando numérico |
| `ui_kits/caixa/` | Terminal de caixa | Caixa | Mesas abertas + conta → recebimento → fechamento |
| `ui_kits/painel-dono/` | Painel do dono | Gestor | Pulso → desempenho → resultado e custo |
| `ui_kits/admin-nexora/` | Painel da plataforma | Admin Replay | Instâncias → provisionar → auditoria |

Cada kit tem `README.md` com o mapa tela → requisito e as decisões copiadas da especificação.
Todos são navegáveis: clique nos cards, botões e itens de navegação.

### Não coberto (e por quê)

- **Delivery próprio (M5) e app do entregador** — Fase 4 do roadmap; nenhum detalhe de tela na especificação além do fluxo. Os componentes já suportam (`StatusPill` tem `DISPATCHED`/`DELIVERED`, `Badge` tem canal).
- **Estoque/ficha técnica (M6) e financeiro (M7)** como telas próprias — Fases 2 e 3; aparecem como indicadores no painel do dono, não como CRUD.
- **App de frios** — explicitamente fora de escopo.
