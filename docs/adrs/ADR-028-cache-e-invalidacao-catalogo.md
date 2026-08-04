# ADR-028 · Cache e invalidação do catálogo

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-010, ADR-011, ADR-027, ADR-030 |
| **Requisitos afetados** | RF-CAT-07, RF-KDS-10, RNF-PER-03 |

---

## Contexto

O catálogo é o dado mais lido do sistema — toda abertura de cardápio, todo lançamento de pedido — e um dos que menos muda. Candidato natural a cache agressivo.

Mas há uma exceção crítica: **disponibilidade de produto**. Quando a cozinha marca "acabou a calabresa", isso precisa refletir **em até 2 segundos em todos os canais** (RF-CAT-07). Vender o que não existe é uma das principais causas de cancelamento e de nota baixa.

Ou seja: o mesmo recurso tem uma parte quase estática e uma parte que precisa ser quase instantânea. Tratá-lo como uma coisa só leva ou a cache inútil ou a disponibilidade errada.

## Decisão

**Separar catálogo (cache agressivo, invalidação por versão) de disponibilidade (tempo real, sem cache).**

## Detalhamento

### Dois recursos distintos

```http
GET /v1/public/menu?channel=DINE_IN
→ estrutura, nomes, fotos, preços, modificadores
   Cache-Control: public, max-age=300, stale-while-revalidate=3600
   ETag: "catalog-v88"

GET /v1/public/availability
→ { "unavailable": ["<variantId>", "<variantId>"] }
   Cache-Control: no-store
```

O cardápio pesado é cacheado. A lista de indisponíveis é pequena, muda com frequência e nunca é cacheada.

### Versão de catálogo

```
tenant_config.catalogVersion  (inteiro, incrementado a cada alteração estrutural)
```

O cliente guarda a versão que possui. A cada reconexão ou a cada 5 minutos:

```
GET /v1/public/catalog-version → { version: 89, availabilityHash: "a3f2..." }
   ├─ version diferente          → baixa o catálogo inteiro
   └─ availabilityHash diferente → baixa apenas a disponibilidade
```

Uma requisição minúscula resolve os dois casos.

### Propagação instantânea de indisponibilidade

```
Cozinha marca indisponível
   │
   ├─► WebSocket: evento product.unavailable → todos os clientes conectados (< 2 s)
   ├─► Incrementa availabilityHash
   └─► Nuvem propaga aos canais externos (delivery)
```

Cliente conectado recebe pelo WebSocket. Cliente que estava offline descobre no próximo `catalog-version`. Cliente que tenta pedir um item indisponível recebe `422 PRODUCT_UNAVAILABLE` (ADR-021) — a validação no servidor é a rede de segurança final.

### Camadas de cache

| Camada | O que | TTL |
|---|---|---|
| CDN | Fotos de produto e logos | 1 ano (URL versionada) |
| Service Worker | Catálogo, branding | Stale-while-revalidate |
| IndexedDB | Catálogo para uso offline | Até mudar a versão |
| Memória (TanStack Query) | Sessão atual | 5 min |
| Redis (edge) | Catálogo montado por canal | Até mudar a versão |

### Invalidação

| Alteração | Efeito |
|---|---|
| Preço, nome, foto, modificador | `catalogVersion++` → clientes rebaixam o catálogo |
| Disponibilidade | `availabilityHash` muda + evento em tempo real |
| Branding | `brandingVersion++` (ADR-010) |
| Configuração operacional | `configVersion++` |

Versões separadas evitam que uma mudança de cor force o rebaixamento do cardápio inteiro.

### Consistência aceita

O modelo é de **consistência eventual com validação forte no servidor**:

- O cliente pode, por segundos, exibir um produto que acabou
- Ao tentar pedir, recebe erro claro e o cardápio se corrige
- É preferível a bloquear o cardápio inteiro a cada mudança

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sem cache | Sempre atual | Carrega o cardápio inteiro a cada abertura; lento em 4G | Falha em RNF-PER-03 |
| Cache único para catálogo e disponibilidade | Simples | Ou cache inútil (TTL curto) ou disponibilidade errada (TTL longo) | Falha em RF-CAT-07 |
| Invalidação por TTL apenas | Simples | Janela de dado errado proporcional ao TTL | Insuficiente para disponibilidade |
| Push de catálogo completo a cada mudança | Sempre atual | Tráfego desnecessário para todos os clientes | Desproporcional |
| GraphQL com cache normalizado | Granularidade fina | Adotaria GraphQL só por isso | Complexidade desnecessária |

## Consequências

**Positivas**

- Cardápio carrega em menos de 2 s em 4G, com cache quente
- Indisponibilidade propaga em menos de 2 s
- Funciona offline com o último catálogo conhecido
- Tráfego baixo: verificação de versão custa poucos bytes

**Negativas**

- Duas versões a manter coerentes
- Janela pequena de inconsistência visual
- Cache em várias camadas dificulta depuração

**Mitigações**

- Validação no servidor é a autoridade final — inconsistência visual nunca vira pedido inválido
- Ferramenta de suporte permite forçar invalidação de um tenant
- `catalogVersion` exibida em tela de diagnóstico

## Como validar

- Teste: marcar indisponível reflete em mesa, garçom e delivery em menos de 2 s
- Teste: alterar preço incrementa a versão e o cliente rebaixa o catálogo
- Teste: cliente offline com catálogo antigo tenta pedir item esgotado → 422 ao reconectar
- RNF-PER-03: cardápio em menos de 2 s com cache quente

## Revisitar quando

- Cardápios ficarem grandes o suficiente para justificar carga incremental por categoria
