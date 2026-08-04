# US-012 · Grupos de modificadores

|  |  |
|---|---|
| **Épico** | [E-01 · Catalogo e Cardapio](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 8 pontos |
| **Sprint sugerida** | Sprint 1 |
| **Requisitos funcionais** | RF-CAT-03 |
| **Regras de negócio** | — |
| **ADRs** | ADR-016 |
| **Eventos** | EVT-050 |
| **Aplicações** | web-admin, web-menu, web-pos, api-cloud |
| **Autoridade do dado** | Nuvem |

---

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** definir adicionais, remoções e opções obrigatórias por produto,
> **para** que o cliente monte o pedido do jeito dele sem precisar escrever observação livre.

## 2. Contexto e motivação

Observação livre é o inimigo da cozinha: texto solto não é mensurável, não gera baixa de estoque e não precifica adicional. Modificador estruturado resolve os três problemas de uma vez.

O grupo de modificadores carrega a regra: mínimo, máximo, seleção única ou múltipla, obrigatório ou opcional. É isso que permite exigir a escolha de tamanho de massa antes de aceitar o item e cobrar a borda recheada corretamente.

## 3. Escopo

### 3.1 Dentro desta história

- CRUD de grupo de modificadores com regra de mínimo, máximo e obrigatoriedade
- CRUD de modificador com `price_delta` (positivo, zero ou negativo)
- Vínculo de grupo a produto ou a variação
- Validação da regra no momento de adicionar o item ao pedido
- `name_snapshot` gravado no item do pedido, preservando o nome da época
- Reuso do mesmo grupo em vários produtos

### 3.2 Fora desta história

- Modificador que consome insumo próprio na ficha técnica (US-101, Fase 2)
- Modificador condicional (aparece só se outro foi escolhido)
- Observação livre por item (US-030)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Grupos de modificadores

  Cenário: Modificador obrigatório
    Dado um produto com grupo "Tamanho" obrigatório e seleção única
    Quando o cliente tentar adicionar o item sem escolher
    Então o sistema deve impedir e destacar o grupo pendente

  Cenário: Limite máximo de seleção
    Dado um grupo "Adicionais" com máximo de 3 opções
    Quando o cliente tentar selecionar a quarta
    Então a seleção deve ser bloqueada com aviso claro
    E as três já escolhidas devem permanecer

  Cenário: Preço do adicional somado
    Dado uma pizza de R$ 45,00 e o adicional "Borda Catupiry" de R$ 8,00
    Quando o item for calculado
    Então o preço do item deve ser R$ 53,00
    E o adicional deve aparecer discriminado na comanda e no comprovante

  Cenário: Remoção sem custo
    Dado o modificador "sem cebola" com price_delta zero
    Quando for selecionado
    Então o preço não deve mudar
    E a instrução deve aparecer no cartão do KDS

  Cenário: Reuso de grupo entre produtos
    Dado o grupo "Ponto da massa" vinculado a 12 produtos
    Quando o gestor alterar uma opção do grupo
    Então a alteração deve valer para os 12 produtos

  Cenário: Preservação do nome histórico
    Dado um pedido antigo com o adicional "Borda Catupiry"
    Quando o gestor renomear o modificador para "Borda Recheada Catupiry"
    Então o comprovante do pedido antigo deve continuar exibindo o nome original
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Configuração, não código | Regras de mínimo/máximo são dados, nunca condicionais em código |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.updated` | Grupo ou modificador alterado | productId, modifierGroupId | ↓ |

## 7. Contrato de API

```http
POST /v1/catalog/modifier-groups
{ "name": "Adicionais", "minSelect": 0, "maxSelect": 3,
  "isRequired": false, "selectionType": "MULTIPLE" }

POST /v1/catalog/modifier-groups/{id}/modifiers
{ "name": "Borda Catupiry", "priceDelta": 800, "position": 1 }

POST /v1/catalog/variants/{id}/modifier-groups   { "groupId": "...", "position": 2 }

# Consumido no cardápio público:
GET /v1/public/menu?channel=DINE_IN
→ { "categories": [ { "products": [ { "modifierGroups": [
      { "id","name","minSelect","maxSelect","isRequired",
        "modifiers": [ { "id","name","priceDelta" } ] } ] } ] } ] }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `modifier_group` | Regra de escolha | `name`, `min_select`, `max_select`, `is_required`, `selection_type` |
| `modifier` | Opção com delta de preço | `group_id`, `name`, `price_delta`, `position`, `is_active` |
| `variant_modifier_group` | Vínculo variação↔grupo | `variant_id`, `group_id`, `position` |
| `order_item_modifier` | Escolha registrada no pedido | `modifier_id`, `name_snapshot`, `price_delta_snapshot` |

> `name_snapshot` e `price_delta_snapshot` garantem que comprovante antigo continue correto após renomeação ou reprecificação (decisão 7 do ERD).

## 9. Comportamento offline

Replicado para o edge e validado localmente. A regra de mínimo/máximo é aplicada no cliente (para feedback imediato) **e** no servidor (para integridade) — validação só no cliente é falha de segurança, validação só no servidor é experiência ruim em rede lenta.

## 10. Interface e experiência

- Grupo obrigatório destacado visualmente antes de o cliente tentar avançar, não depois
- Contador de seleção restante visível ("escolha até 3 · 1 selecionado")
- Adicional com preço sempre exibindo o valor, nunca só o nome
- No KDS, remoções aparecem em destaque — "SEM CEBOLA" é a informação que evita retrabalho

## 11. Métricas, alertas e observabilidade

- Modificadores mais escolhidos por produto — insumo de decisão de cardápio
- Taxa de itens com observação livre versus modificador estruturado; livre alto indica modificador faltando
- Receita de adicionais como percentual do faturamento

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Validação de mínimo, máximo, seleção única e obrigatoriedade |
| Unitário | Cálculo de preço do item com múltiplos adicionais e deltas negativos |
| Integração | Validação aplicada tanto no cardápio público quanto na API de pedido |
| Integração | `name_snapshot` preserva o nome após renomeação |
| E2E | Cliente não consegue enviar item com grupo obrigatório pendente |

## 13. Dependências

**Depende de:** US-010, US-011  
**Habilita:** US-030, US-101

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

- Excesso de grupos obrigatórios trava o fluxo de pedido do cliente do salão. Recomendação de produto: no máximo um grupo obrigatório por produto no MVP, validado no piloto.

---

*US-012 · Épico E-01 · Pacote 004_DonaBetinha · Replay Studio.*