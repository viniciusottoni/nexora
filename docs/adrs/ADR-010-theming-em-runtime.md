# ADR-010 · Theming em runtime, build único

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, UX |
| **Relacionados** | ADR-009, ADR-013, ADR-028, ADR-030, ADR-032 |
| **Requisitos afetados** | RF-PLT-02, RF-PLT-03, RF-PLT-04 |

---

## Contexto

A diretriz de produto exige que **toda a camada web seja personalizada** por estabelecimento: marca, cores, tipografia, domínio, ícone do PWA, textos.

A tentação natural é gerar um build por cliente. Mas isso cresce linearmente: com 30 clientes, corrigir um bug de CSS significa 30 builds, 30 deploys e 30 verificações. É o caminho mais curto para um produto que não consegue mais ser atualizado.

## Decisão

**Um único artefato de build serve todos os tenants.** A identidade visual é carregada em runtime e aplicada por CSS custom properties.

## Detalhamento

### Fluxo

```
1. App carrega  → resolve o tenant pelo host ou pelo token
2. GET /v1/public/branding?host=cardapio.donabetinha.com.br
3. Aplica       → CSS custom properties em :root
4. Cacheia      → Service Worker (evita flash na próxima visita)
```

### Tokens de design

```css
:root {
  --brand-primary:      #C1121F;
  --brand-on-primary:   #FFFFFF;
  --brand-secondary:    #669BBC;
  --brand-surface:      #FDF0D5;
  --brand-font-family:  'Inter', system-ui, sans-serif;
  --brand-radius:       12px;
  --brand-density:      1;        /* escala de espaçamento */
}
```

O Tailwind consome os tokens, não valores literais:

```js
// packages/ui/tailwind.preset.js
colors: {
  primary: 'var(--brand-primary)',
  'on-primary': 'var(--brand-on-primary)',
  surface: 'var(--brand-surface)',
}
```

**Nenhum componente usa cor literal.** Regra verificada por lint.

### Manifest do PWA por tenant

```
GET /manifest.webmanifest?tenant=<slug>
→ gerado dinamicamente com nome, ícone, cor de tema e splash do estabelecimento
```

### O que é personalizável

| Dimensão | Itens |
|---|---|
| Marca | Logo claro/escuro, cores, tipografia, raio, densidade, favicon |
| PWA | Nome, nome curto, ícone, splash, cor de tema |
| Conteúdo | Nome, descrição, endereço, horários, redes sociais |
| Textos | Boas-vindas, confirmação, agradecimento, termos, política |
| Domínio | Domínio ou subdomínio próprio (Fase 5) |
| QR Code | Arte com a marca |

### O que **não** é personalizável

Estrutura de tela, fluxo de navegação, posição de elementos e comportamento. Personalização de layout por cliente é a porta de entrada da customização por código (ADR-013).

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Build por cliente | Otimização máxima por tenant | N builds, N deploys, N pipelines para corrigir um bug | Inviabiliza a operação em escala |
| Tema por variável de ambiente em build | Simples | Mesma limitação acima | Idem |
| CSS-in-JS com tema por provider | Flexível | Custo de runtime; pior para o cache; bundle maior | CSS custom properties resolvem com custo zero |
| Folha de estilo por tenant servida separadamente | Cache bom | Um arquivo a gerar e invalidar por cliente | Complexidade sem ganho frente às custom properties |

## Consequências

**Positivas**

- Um deploy corrige todos os clientes simultaneamente
- Cliente novo entra sem tocar em pipeline
- Personalização é **dado**, não código — pode ser alterada pelo próprio gestor
- Alteração de marca reflete em até 60 s, sem build

**Negativas**

- Possível flash de conteúdo não estilizado no primeiro carregamento
- Personalização limitada aos tokens previstos
- Uma requisição adicional no boot da aplicação

**Mitigações**

- Branding cacheado pelo Service Worker e injetado no HTML inicial no servidor quando possível
- Conjunto de tokens desenhado para cobrir os casos reais; ampliação vira configuração do produto (ADR-032), não exceção
- Requisição de branding é a primeira e é pequena (< 5 KB)

## Como validar

- Nenhuma cor literal em componente (regra de lint em `packages/ui`)
- Dois tenants carregados no mesmo navegador exibem marcas diferentes sem novo build
- Alteração de cor no painel reflete em menos de 60 s
- Lighthouse: sem regressão de CLS por causa do theming

## Revisitar quando

- Um cliente exigir estrutura de tela substancialmente diferente — nesse caso a resposta é ADR-013, não uma exceção de theming
