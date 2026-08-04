# US-057 · Comprovante nao fiscal de consumo

|  |  |
|---|---|
| **Épico** | [E-05 · Caixa e Pagamento](./README.md) |
| **Fase** | 1 — MVP |
| **Prioridade** | M — Must have |
| **Estimativa** | 3 pontos |
| **Sprint sugerida** | Sprint 5 |
| **Requisitos funcionais** | RF-CXA-12 |
| **Regras de negócio** | RN-023 |
| **ADRs** | ADR-026 |
| **Eventos** | — |
| **Aplicações** | web-pos, api-edge |
| **Autoridade do dado** | Local |

---

## 1. História

> **Como** caixa (P4) e cliente do salão (P1),
> **quero** entregar ao cliente um comprovante do que foi consumido e pago,
> **para** que ele tenha o registro da compra, mesmo antes de existir emissão fiscal.

## 2. Contexto e motivação

Enquanto a **pendência crítica de emissão fiscal** não é resolvida (RN-023, pendência 1 do índice), o produto entrega comprovante não fiscal. Isso não substitui NFC-e ou SAT e não pode ser apresentado como se substituísse.

A decisão de impressão térmica está no ADR-026. O comprovante também é entregue em versão digital, por link ou QR Code, o que reduz consumo de papel e serve ao cliente que não quer a via impressa.

## 3. Escopo

### 3.1 Dentro desta história

- Geração do comprovante com itens, modificadores, frações, taxa, desconto e formas de pagamento
- Impressão térmica não fiscal
- Versão digital acessível por link ou QR Code
- Identificação explícita de que não é documento fiscal
- Reimpressão registrada em auditoria

### 3.2 Fora desta história

- Emissão fiscal — NFC-e e SAT (**pendência crítica**, fora do escopo desta etapa)
- Envio por e-mail ou WhatsApp (Fase 6)

## 4. Critérios de aceite

```gherkin
Funcionalidade: Comprovante não fiscal

  Cenário: Geração após o pagamento
    Dado uma conta paga
    Quando o pagamento for confirmado
    Então o comprovante deve ser gerado com itens, valores e formas de pagamento
    E deve indicar claramente que não é documento fiscal

  Cenário: Detalhamento completo
    Dado uma conta com meio a meio, adicional e desconto
    Quando o comprovante for gerado
    Então os dois sabores, o adicional e o desconto devem estar discriminados

  Cenário: Versão digital
    Dado um comprovante gerado
    Quando o cliente ler o QR Code impresso ou exibido na tela
    Então deve acessar a versão digital do mesmo comprovante

  Cenário: Reimpressão auditada
    Dado um comprovante já impresso
    Quando for reimpresso
    Então a reimpressão deve ser registrada em audit_log com autor e horário

  Cenário: Impressora indisponível
    Dado que a impressora térmica está offline
    Quando o comprovante for gerado
    Então o pagamento não deve ser bloqueado
    E deve ser oferecida a versão digital
```

## 5. Regras de negócio aplicáveis

| ID | Regra | Como se manifesta nesta história |
|---|---|---|
| RN-023 | Emissão de documento fiscal | **[PENDÊNCIA CRÍTICA]** — esta história entrega apenas comprovante não fiscal |

## 6. Eventos emitidos e consumidos

_Não se aplica a esta história._

## 7. Contrato de API

```http
GET /v1/sessions/{id}/receipt
→ { "receipt": { "url": "...", "number": "NF-000123",
                 "isFiscal": false,
                 "issuedAt": "...", "items": [...],
                 "payments": [...], "total": 19800 } }

POST /v1/sessions/{id}/receipt/print
{ "printerId": "..." }
→ 202 { "queued": true }

POST /v1/sessions/{id}/receipt/reprint     # registrado em auditoria
```

## 8. Modelo de dados

| Tabela | Papel nesta história | Campos relevantes |
|---|---|---|
| `table_session` | Origem do comprovante | `total`, `service_fee`, `discount` |
| `payment` | Formas de pagamento | `method`, `amount` |
| `audit_log` | Reimpressões | `action=RECEIPT_REPRINTED` |

## 9. Comportamento offline

Integralmente local: geração e impressão acontecem no edge, com a impressora térmica na rede da loja (ADR-026).

A versão digital fica acessível pela rede local; o acesso de fora da loja depende de sincronização.

## 10. Interface e experiência

- Impressão automática após o pagamento, com opção de desativar por dispositivo
- Aviso claro e não escondido de que o documento não é fiscal
- QR Code para a versão digital sempre presente no impresso
- Falha de impressora nunca bloqueia o pagamento

## 11. Métricas, alertas e observabilidade

- Percentual de comprovantes impressos versus digitais — insumo de economia de papel
- Falhas de impressão por dispositivo
- Reimpressões por operador — padrão anômalo merece atenção

## 12. Estratégia de teste

| Nível | O que verificar |
|---|---|
| Unitário | Formatação do comprovante com todos os elementos |
| Integração | Falha de impressora não bloqueia o pagamento |
| Integração | Reimpressão registrada em auditoria |
| Integração | Versão digital acessível pelo QR Code |

## 13. Dependências

**Depende de:** US-052  
**Habilita:** —

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

- **Pendência crítica (RN-023)** — a emissão fiscal não foi abordada na descoberta e é exigência legal no varejo alimentar. Precisa ser esclarecida com o cliente e o contador antes do lançamento em produção. Altera escopo, custo e prazo de forma significativa.
- O modelo de impressora térmica ainda não foi definido; o ADR-026 estabelece a abstração, mas o hardware precisa ser validado.

---

*US-057 · Épico E-05 · Pacote 004_DonaBetinha · Replay Studio.*