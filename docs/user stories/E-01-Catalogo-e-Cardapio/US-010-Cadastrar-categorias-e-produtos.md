# US-010 · Cadastrar categorias e produtos

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-CAT-01 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-005, ADR-030 |
| **Eventos** | EVT-050 |
| **Aplicações** | web-admin, api-cloud |
| **Autoridade do dado** | Nuvem — editado pela gestão, lido pela operação |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** cadastrar as categorias e os produtos que vendo, com foto e descrição,
> **para** que o cardápio digital substitua o cardápio de papel em todos os canais.

## 2. Contexto e motivação

É a porta de entrada do produto: sem catálogo não há pedido, não há preço, não há ficha técnica e não há métrica de venda por produto.

A regra de autoridade do dado (doc. 02, seção 2.1) vale integralmente aqui: cardápio é **editado na nuvem e apenas lido no local**. Isso elimina qualquer conflito de sincronização nesse domínio.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de categoria com ordenação e visibilidade por canal
- CRUD de produto com nome, descrição, ingredientes visíveis ao cliente e foto
- Upload de imagem com otimização automática e entrega por CDN
- Ordenação manual de produtos dentro da categoria
- Ativação e desativação de produto (distinto de indisponibilidade operacional)
- Propagação do catálogo para o edge pelo pull de configuração

### 3.2 Fora desta história

- Variações e preços (US-011 e US-014)
- Modificadores (US-012)
- Importação por planilha (US-144, Fase 5)
- Cardápio por horário e dia (RF-CAT-10, Fase 2)
- Combos e promoções (RF-CAT-11, Fase 4)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Cadastro de categorias e produtos

  Cenário: Criação de produto completo
    Dado uma categoria "Pizzas Salgadas" já existente
    Quando o gestor cadastrar o produto "Pizza Mussarela" com descrição, ingredientes e foto
    Então o produto deve ser criado como ativo
    E o evento product.created deve ser emitido
    E a imagem deve estar disponível pelo CDN em formato otimizado

  Cenário: Ordenação do cardápio
    Dado uma categoria com cinco produtos
    Quando o gestor reordenar os produtos por arrastar
    Então a nova ordem deve ser refletida em todos os canais
    E a ordem deve ser respeitada no cardápio da mesa e do delivery

  Cenário: Desativação de produto
    Dado um produto ativo com pedidos históricos
    Quando o gestor desativá-lo
    Então ele deve sumir dos canais de venda
    E os pedidos históricos devem continuar exibindo o produto corretamente

  Cenário: Propagação ao servidor local
    Dado um produto criado na nuvem
    Quando o próximo pull de configuração ocorrer
    Então o produto deve estar disponível no cardápio do edge
    E o atraso não deve exceder 30 segundos com conexão normal

  Cenário: Produto sem foto
    Quando o gestor salvar um produto sem imagem
    Então o cadastro deve ser aceito
    E o cardápio deve exibir um marcador visual neutro no lugar da foto
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica é configuração, nunca código | Categorias e produtos são dados; nenhum produto é codificado |
| RN-015 | Isolamento entre estabelecimentos | Catálogo carrega `tenant_id` com RLS ativo |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.created` / `product.updated` | Cadastro criado ou alterado | productId, changedKeys[] | ↓ |

## 7. Contrato de API

```http
POST  /v1/catalog/categories     { "name": "Pizzas Salgadas", "position": 1 }
GET   /v1/catalog/categories
PATCH /v1/catalog/categories/{id}

POST  /v1/catalog/products
{ "categoryId": "...", "name": "Pizza Mussarela",
  "description": "...", "ingredients": "molho, mussarela, orégano",
  "isActive": true }
→ 201 { "product": {...} }

POST  /v1/catalog/products/{id}/image     (multipart)
PATCH /v1/catalog/products/{id}
PATCH /v1/catalog/products/reorder        { "categoryId": "...", "order": ["...","..."] }

# Leitura pública, por canal:
GET /v1/public/menu?channel=DINE_IN
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `category` | Agrupamento do cardápio | `name`, `position`, `is_active`, `channels[]` |
| `product` | Produto vendável | `category_id`, `name`, `description`, `ingredients`, `station_id`, `is_active` |
| `media_asset` | Foto do produto no object storage | `kind=PRODUCT_IMAGE`, `url`, `bytes` |

## 9. Comportamento offline

Somente leitura no edge. O catálogo é replicado da nuvem pelo pull de sincronização (US-063) e fica disponível integralmente offline — a operação nunca depende de internet para saber o que vender.

Cadastro e edição são operações de nuvem: com internet caída, o gestor não consegue alterar o cardápio, degradação aceitável porque não é operação crítica de tempo real.

## 10. Interface e experiência

- Cadastro em tela única, sem assistente de múltiplas etapas — o gestor cadastra dezenas de produtos em sequência
- Duplicação de produto com um clique, para acelerar a carga inicial
- Pré-visualização de como o produto aparece no cardápio da mesa, ao lado do formulário
- Recorte assistido de imagem no upload, com proporção fixa

## 11. Métricas, alertas e observabilidade

- Contagem de produtos ativos por categoria e por canal
- Percentual de produtos com foto — indicador de qualidade do cardápio
- Tempo entre `product.created` e disponibilidade no edge

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de campos obrigatórios e de ordenação |
| Integração | Produto criado na nuvem chega ao edge pelo pull |
| Integração | Produto desativado some dos canais mas permanece legível em pedido histórico |
| Isolamento | Catálogo de um tenant não é visível a outro |
| Desempenho | Cardápio com 200 produtos carrega em menos de 2 s em 4G |

## 13. Dependências

**Depende de:** US-001, US-017  
**Habilita:** US-011, US-012, US-021, US-101

## 14. Definition of Ready e Definition of Done

**DoR — a história só entra em sprint quando:**

- [ ] Persona, ação e resultado estão claros
- [ ] Critérios de aceite escritos em Gherkin
- [ ] Requisito funcional (RF) e evento (EVT) referenciados
- [ ] Dependências identificadas e resolvidas
- [ ] Desenho de tela existe (quando há interface)
- [ ] Estimada pelo time
- [ ] Comportamento offline definido
- [ ] Impacto em métrica e alerta identificado

**DoD — a história só é concluída quando:**

- [ ] Código revisado e aprovado por outro desenvolvedor
- [ ] Testes unitários dos casos de negócio passando
- [ ] Teste de integração do fluxo principal passando
- [ ] Teste de isolamento multi-tenant (quando a história toca tabela com `tenant_id`)
- [ ] Eventos emitidos conforme o catálogo do documento 04
- [ ] Comportamento offline verificado (quando aplicável)
- [ ] Critérios de aceite validados em ambiente de teste pelo PO
- [ ] Sem violação do ADR-013 (proibição de código por cliente)
- [ ] Documentação atualizada (OpenAPI, catálogo de eventos, modelo de dados)
- [ ] Observabilidade instrumentada (log estruturado + traço OpenTelemetry)
- [ ] Aprovada pelo PO

## 15. Riscos, premissas e pendências

- A carga inicial de cardápio é a tarefa mais trabalhosa do onboarding e se repete em cada novo cliente (Visão Geral, 11.2). Padronizar e otimizar desde já reduz o custo marginal de implantação.
- Materiais pendentes do cliente: cardápio completo com preços de salão e delivery, e fotos dos produtos.

---

*US-010 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*