# US-063 · Pull de cardapio e configuracao

|  |  |
|---|---|
| **Épico** | [E-06 · Sincronizacao Local-Nuvem](./README.md) — ❌ **CANCELADA** |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 5 pontos |
| **Sprint sugerida** | Sprint 6 |
| **Requisitos funcionais** | RF-OFF-02 |
| **Regras de negócio** | RN-016 |
| **ADRs** | ADR-028, ADR-032 |
| **Eventos** | EVT-050, EVT-052, EVT-054, EVT-055 |
| **Aplicações** | api-edge, api-cloud, packages/sync |
| **Autoridade do dado** | Nuvem → loja |

---

> ❌ **Cancelada em 06/08/2026.** Mudança de foco de negócio: o produto passa a operar 100% online, sem edge nem sincronização (ver [ADR-040](../../adrs/ADR-040-arquitetura-100-online-api-unica.md) e [E-16](../E-16-iMenu-Online/README.md)). Conteúdo mantido como registro histórico.

## 1. História

> **Como** gestor do estabelecimento (P8),
> **quero** que as alterações que faço no painel cheguem à loja automaticamente,
> **para** que eu não precise de ninguém na loja para atualizar cardápio ou preço.

## 2. Contexto e motivação

É a direção inversa do sync: nuvem para loja. Cardápio, preços, configuração, limiares, usuários e permissões nascem na nuvem e descem para o edge.

Como a autoridade é única (doc. 02, seção 6.3), não há conflito possível nesses domínios — o edge apenas aplica o que recebe. O intervalo de pull é de 30 segundos, o que é adequado para dado de configuração.

## 3. Escopo

### 3.1 Dentro desta história

- Pull a cada 30 s com cursor persistido
- Aplicação de eventos de catálogo, preço, configuração, branding e permissões
- Invalidação de cache do cardápio no edge (ADR-028)
- Propagação da alteração aos dispositivos por WebSocket
- Carga inicial completa na instalação
- Versão de configuração para verificação de consistência

### 3.2 Fora desta história

- Push de eventos operacionais (US-061)
- Resolução de conflitos (US-067)
- Atualização de versão do software (US-146, Fase 5)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Pull de configuração e cardápio

  Cenário: Alteração de preço chega à loja
    Dado que o gestor alterou o preço de um produto na nuvem
    Quando o próximo pull ocorrer
    Então o novo preço deve estar vigente no edge em no máximo 30 segundos
    E os dispositivos devem refletir a mudança

  Cenário: Cursor persistido
    Dado o edge com cursor na posição 98220
    Quando o pull for executado
    Então deve receber apenas eventos posteriores a essa posição
    E o cursor deve avançar somente após a aplicação bem-sucedida

  Cenário: Carga inicial
    Dado uma instalação nova com cursor zero
    Quando o primeiro pull ocorrer
    Então deve receber o catálogo e a configuração completos
    E deve concluir em lotes, sem estourar memória

  Cenário: Invalidação de cache
    Dado o cardápio em cache no edge
    Quando um evento de alteração de produto for aplicado
    Então o cache deve ser invalidado
    E a próxima leitura deve refletir a alteração

  Cenário: Versão de configuração divergente
    Dado o edge com configVersion 87 e a nuvem em 88
    Quando o health check for executado
    Então a divergência deve ser detectada
    E um pull deve ser disparado imediatamente

  Cenário: Pull com falha parcial
    Dado um lote em que um evento falha ao ser aplicado
    Quando o pull processar
    Então o cursor não deve avançar além do último aplicado com sucesso
    E o erro deve ser registrado para diagnóstico
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-016 | Regra específica é configuração, nunca código | Toda mudança de comportamento desce como dado |
| RN-019 | Em conflito, prevalece o menor `ocorrido_em` | Não se aplica: nestes domínios a autoridade é exclusiva da nuvem |

## 6. Eventos emitidos e consumidos

| ID | Evento | Quando | Payload principal | Sync |
|---|---|---|---|---|
| EVT-050 | `product.created` / `product.updated` | Cadastro alterado | productId, changedKeys[] | ↓ |
| EVT-052 | `price.changed` | Preço alterado | variantId, channel, newAmount, validFrom | ↓ |
| EVT-053 | `recipe.updated` | Ficha técnica alterada (Fase 2) | variantId, items[] | ↓ |
| EVT-054 | `tenant.config_updated` | Configuração ou limiar alterado | changedKeys[], configVersion | ↓ |
| EVT-055 | `tenant.branding_updated` | Identidade visual alterada | changedKeys[] | ↓ |

## 7. Contrato de API

```http
GET /v1/sync/pull?cursor=98220&limit=500
X-Installation-Id: <uuid>
X-Signature: <hmac-sha256>
→ { "events": [...], "nextCursor": 98720, "hasMore": true }

GET /v1/sync/health
→ { "serverTime": "...", "expectedVersion": "1.4.2", "configVersion": 88 }
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `sync_cursor` | Cursor de pull | `direction=PULL`, `cursor`, `updated_at` |
| Tabelas de catálogo e configuração | Réplica local | `product`, `product_variant`, `price`, `modifier*`, `tenant_config`, `app_user`, `role` |
| Cache Redis | Cardápio montado | Invalidado por evento (ADR-028) |

## 9. Comportamento offline

Com a internet caída, o edge continua operando com a última configuração recebida. Alterações feitas na nuvem nesse intervalo entram em vigor quando a conexão voltar.

Isso significa que um pedido criado offline usa o preço da última sincronização — o comportamento correto, porque foi o preço exibido ao cliente no cardápio.

A defasagem é visível no indicador de sincronização (US-065).

## 10. Interface e experiência

- Sem interface própria
- Efeito visível: alteração feita no painel aparece na loja em até 30 segundos, sem intervenção
- No painel de gestão, indicação de quando a última configuração foi aplicada em cada loja

## 11. Métricas, alertas e observabilidade

- Latência entre alteração na nuvem e aplicação no edge
- Divergência de `configVersion` por instalação — insumo do painel de saúde (US-140)
- Falhas de aplicação de evento por tipo

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Integração | Alteração de preço aplicada no edge em menos de 30 s |
| Integração | Cursor não avança além do último evento aplicado com sucesso |
| Integração | Carga inicial completa em instalação nova |
| Integração | Invalidação de cache após alteração de produto |
| Caos | Falha no meio da carga inicial permite retomada |

## 13. Dependências

**Depende de:** US-062, US-006  
**Habilita:** US-010, US-014, US-101

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

- Cache de cardápio não invalidado corretamente é a causa mais comum de "alterei o preço e não mudou". A invalidação por evento é obrigatória (ADR-028).

---

*US-063 · Épico E-06 · Pacote 004_DonaBetinha · Replay Studio.*