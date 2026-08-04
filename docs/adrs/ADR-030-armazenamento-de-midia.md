# ADR-030 · Armazenamento e entrega de mídia

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-010, ADR-028 |
| **Requisitos afetados** | RF-CAT-01, RF-PLT-02, RNF-PER-03, RNF-PER-10 |

---

## Contexto

O sistema lida com dois tipos de mídia: **fotos de produto** (que vendem — cardápio com foto converte mais) e **assets de marca** (logo, ícone do PWA, splash).

Duas restrições se cruzam:

1. O cardápio precisa carregar em menos de 2 s em 4G (RNF-PER-03) — foto pesada mata esse requisito
2. O cardápio precisa funcionar **offline** na loja (ADR-027), o que significa que as fotos precisam estar acessíveis mesmo sem internet

## Decisão

**Object storage S3-compatível com CDN, URLs versionadas por hash de conteúdo, variantes geradas no upload, e réplica local das imagens do cardápio no edge.**

## Detalhamento

### Estrutura

```
s3://<bucket>/
  tenants/<tenantId>/
    branding/  logo-light.<hash>.svg · icon-512.<hash>.png
    products/  <variantId>/original.<hash>.jpg
                            large.<hash>.webp     800px
                            medium.<hash>.webp    400px
                            thumb.<hash>.webp     160px
                            blur.<hash>.webp       20px (placeholder)
```

### URL versionada por conteúdo

```
https://cdn.<plataforma>/tenants/018f.../products/018f.../medium.a3f2b8c1.webp
                                                          └── hash do conteúdo
Cache-Control: public, max-age=31536000, immutable
```

Conteúdo novo gera hash novo, logo URL nova. **Não existe invalidação de CDN** — o problema clássico de "troquei a foto e o cliente ainda vê a antiga" deixa de existir.

### Pipeline de upload

```
1. Cliente pede URL assinada  → POST /v1/media/upload-url
2. Upload direto ao storage   (não passa pela API — economiza banda e memória)
3. Worker processa            → gera variantes WebP + AVIF, calcula hash, extrai blur
4. Registra em media_asset    → vincula ao produto
5. Incrementa catalogVersion  (ADR-028)
```

### Limites e validação

| Item | Regra |
|---|---|
| Formatos aceitos | JPEG, PNG, WebP, HEIC |
| Tamanho máximo | 10 MB |
| Dimensão mínima | 800 × 600 |
| Validação | Tipo real verificado por magic bytes, não por extensão |
| Metadados | EXIF removido (pode conter geolocalização) |
| Nome do arquivo | Descartado; nome é gerado |

### Entrega otimizada

```html
<picture>
  <source srcset="...medium.hash.avif" type="image/avif">
  <source srcset="...medium.hash.webp" type="image/webp">
  <img src="...medium.hash.jpg" loading="lazy"
       style="background-image: url(data:image/webp;base64,<blur>)">
</picture>
```

O placeholder embutido (~200 bytes) elimina o salto de layout e dá sensação de carregamento imediato.

### Réplica local no edge

O edge mantém cópia das imagens `thumb` e `medium` do cardápio ativo:

```
/var/lib/dona-betinha/media/products/<variantId>/medium.<hash>.webp
```

Servidas por Nginx na LAN. Com isso:

- Cardápio funciona offline com fotos
- Carregamento na loja é instantâneo (rede local)
- Consumo de banda da loja cai

Sincronizadas junto com o catálogo (ADR-028); imagens de versão antiga são purgadas.

### Custo estimado

| Item | Estimativa |
|---|---|
| 200 produtos × 4 variantes × ~80 KB | ~64 MB por loja |
| 50 lojas | ~3,2 GB |
| Tráfego mensal (CDN) | ~50 GB |

Volume irrelevante em custo — a decisão é sobre desempenho e simplicidade operacional, não sobre economia de armazenamento.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Imagens no banco (BLOB) | Backup unificado; transacional | Infla o banco; sem CDN; consultas mais lentas | Antipadrão conhecido |
| Filesystem da aplicação | Simples | Não escala horizontalmente; sem CDN | Impede múltiplas instâncias na nuvem |
| Serviço externo de imagem (Cloudinary, imgix) | Transformação sob demanda | Custo por transformação; dependência externa | Variantes fixas resolvem; economia relevante |
| Só original, redimensionado no cliente | Sem processamento | Trafega arquivo pesado — mata o requisito de 4G | Falha em RNF-PER-03 |
| Sem réplica local | Menos complexidade | Cardápio offline ficaria sem foto | Degradação visível para o cliente na mesa |

## Consequências

**Positivas**

- Cardápio rápido em 4G e instantâneo na loja
- Sem problema de invalidação de CDN
- Upload direto ao storage não sobrecarrega a API
- Cardápio offline mantém as fotos
- Custo previsível e baixo

**Negativas**

- Worker de processamento a manter
- Réplica local ocupa disco do edge (~100 MB)
- Variantes fixas: novo tamanho exige reprocessar o acervo

**Mitigações**

- Variantes dimensionadas para os pontos de quebra reais do layout
- Purga automática de versões antigas no edge
- Reprocessamento em lote disponível como job administrativo

## Como validar

- RNF-PER-03: cardápio com 40 produtos carrega em menos de 2 s em 4G simulado
- Teste: trocar foto gera URL nova; a antiga permanece acessível por 24 h
- Teste: cardápio offline exibe fotos a partir da réplica local
- Teste: upload com extensão falsificada é rejeitado
- Verificação: EXIF removido em todas as imagens processadas

## Revisitar quando

- Surgir necessidade de transformação dinâmica (recorte por foco, marca d'água por campanha)
