# 10 — Estratégia de Testes e Qualidade
## Ecossistema Dona Betinha

| | |
|---|---|
| **Projeto** | 004_DonaBetinha |
| **Documento** | Estratégia de Testes e Qualidade |
| **Versão** | 1.0 |
| **Data** | 31/07/2026 |
| **Depende de** | `08-Requisitos-Nao-Funcionais.md`, `07-Backlog-Epicos-e-User-Stories.md` |

---

## 1. Onde a qualidade deste sistema realmente se decide

Este não é um CRUD. Três características tornam o teste diferente do usual:

| Característica | Consequência para o teste |
|---|---|
| **Opera offline e sincroniza depois** | Testar o caminho feliz online cobre metade do sistema. O outro caminho precisa de teste de caos deliberado |
| **Toda métrica é derivada de evento** | Um evento não emitido produz número errado silenciosamente — o pior defeito possível, porque o dono decide com ele |
| **Multi-tenant** | Um vazamento entre estabelecimentos é incidente de gravidade máxima, e não aparece em teste funcional comum |

> **Regra que orienta tudo:** um bug que faz o sistema parar é ruim. Um bug que faz o sistema apresentar um número errado com aparência de certo é pior — porque ninguém percebe.

---

## 2. Pirâmide de testes

```
                    ┌─────────────────┐
                    │   Exploratório  │   contínuo, manual
                    │   + Piloto real │
                    ├─────────────────┤
                    │      E2E        │   ~40 cenários (Playwright)
                    ├─────────────────┤
                    │   Integração    │   ~200 (Supertest + Postgres real)
                    ├─────────────────┤
                    │    Unitário     │   ~800 (Vitest)
                    └─────────────────┘

         ┌──────────────────────────────────────────┐
         │  TRANSVERSAIS (não cabem na pirâmide)    │
         │  Caos offline · Carga · Isolamento       │
         │  Integridade de evento · Contrato        │
         └──────────────────────────────────────────┘
```

| Nível | Cobertura alvo | Tempo máximo | Quando roda |
|---|---|---|---|
| Unitário | ≥ 70% global · ≥ 90% em `packages/domain` | 60 s | Todo commit |
| Integração | Fluxos principais de cada módulo | 5 min | Todo PR |
| E2E | Jornadas críticas | 15 min | PR para `main` |
| Carga | RNF-PER | 30 min | Semanal + antes de fase |
| Caos offline | RNF-OFF | 20 min | Diário (noturno) |
| Isolamento | RNF-SEG-08 | 30 s | **Todo PR — bloqueante** |

---

## 3. Testes unitários

Foco em `packages/domain` — regras puras, sem banco e sem framework.

### 3.1 O que sempre precisa de teste unitário

| Área | Exemplos |
|---|---|
| Máquinas de estado | Transições válidas e proibidas de pedido, item, mesa e caixa |
| Cálculo de preço | Meio a meio nas três regras, modificadores, taxa de serviço, desconto |
| Cálculo de custo | Ficha técnica, sub-receitas, perda percentual, proporcional em frações |
| Cálculo de tempo | T0–T5, p90, aderência ao prazo |
| Fire time | Sequenciamento reverso |
| Prioridade de fila | Score e ordenação |
| Prazo dinâmico | Fila, capacidade e margem |
| Autorização | Quem pode o quê, em que estado |

### 3.2 Exemplos normativos

```ts
describe('Precificação de meio a meio', () => {
  it('aplica o maior valor quando a regra é HIGHEST', () => {
    const item = buildItem({
      fractions: [
        { variant: variant({ price: 45 }), weight: 0.5 },
        { variant: variant({ price: 52 }), weight: 0.5 },
      ],
    });
    expect(calculatePrice(item, { halfAndHalfPricing: 'HIGHEST' })).toBe(52);
  });

  it('aplica a média quando a regra é AVERAGE', () => {
    // ... espera 48.50
  });
});

describe('Máquina de estados do item', () => {
  it('não permite cancelar item iniciado sem autorização', () => {
    const item = buildItem({ status: 'FIRED' });
    expect(() => cancel(item, { authorization: null }))
      .toThrow(AuthorizationRequiredError);
  });

  it('não permite retroceder de READY para FIRED', () => {
    const item = buildItem({ status: 'READY' });
    expect(() => transition(item, 'FIRED')).toThrow(InvalidTransitionError);
  });
});

describe('Baixa de estoque em meio a meio', () => {
  it('baixa metade dos insumos de cada ficha', () => {
    const movements = deductStock(halfAndHalfItem);
    expect(movements).toContainEqual(
      expect.objectContaining({ ingredient: 'mussarela', quantity: 0.09 }) // metade de 0,18
    );
  });
});
```

---

## 4. Testes de integração

Contra **PostgreSQL real** em container (nunca mock de banco — RLS não pode ser simulado).

### 4.1 Cenários obrigatórios por módulo

| Módulo | Cenários |
|---|---|
| Pedido | Criação com frações e modificadores; idempotência; roteamento por praça; emissão de eventos |
| KDS | Avanço de estado; carimbos gravados; fila filtrada por praça; all-day |
| Caixa | Conta montada; múltiplas formas; divisão; fechamento com divergência |
| Estoque | Baixa por ficha; entrada; contagem; CMV teórico × real |
| Sync | Push idempotente; pull com cursor; conflito; ordenação |
| Auth | PIN por dispositivo; bloqueio; autorização de ação sensível |
| Métricas | Agregado incremental; recálculo noturno corrige atraso |

### 4.2 Teste de emissão de evento — o mais importante deste nível

```ts
describe('Integridade de instrumentação', () => {
  it('toda transição de estado emite seu evento', async () => {
    const order = await createOrder(...);
    await advanceItem(order.items[0].id, 'FIRED');

    const events = await findEvents({ aggregateId: order.items[0].id });
    expect(events).toContainEqual(
      expect.objectContaining({
        type: 'order.item.fired',
        actorId: expect.any(String),
        deviceId: expect.any(String),
        occurredAt: expect.any(Date),
      })
    );
  });

  it('evento e estado são gravados na mesma transação', async () => {
    await simulateFailureAfterStateWrite();
    // nem estado nem evento devem ter sido persistidos
    expect(await findOrder(id)).toBeNull();
    expect(await findEvents({ aggregateId: id })).toHaveLength(0);
  });
});
```

---

## 5. Testes E2E

Playwright, contra ambiente completo (edge + nuvem).

### 5.1 Jornadas críticas

| # | Jornada | Prioridade |
|---|---|---|
| E2E-01 | Cliente lê QR → pede → cozinha produz → garçom entrega → caixa fecha | **Crítica** |
| E2E-02 | Garçom lança pelo celular → KDS → entrega | **Crítica** |
| E2E-03 | Meio a meio com modificador → preço e baixa corretos | **Crítica** |
| E2E-04 | Cancelamento com autorização de gerente | Alta |
| E2E-05 | Divisão de conta com múltiplas formas de pagamento | Alta |
| E2E-06 | Abertura e fechamento de caixa com divergência | Alta |
| E2E-07 | Produto marcado indisponível some de todos os canais | Alta |
| E2E-08 | Drill-down do painel até o pedido em ≤ 3 toques | Alta |
| E2E-09 | Operação completa offline e sincronização posterior | **Crítica** |
| E2E-10 | Entrada de compra → venda → CMV apurado | Alta |
| E2E-11 | Ciclo de delivery (Fase 4) | Alta |
| E2E-12 | Provisionar novo tenant e verificar isolamento | **Crítica** |

### 5.2 Exemplo

```ts
test('E2E-01 · ciclo completo do salão', async ({ page, kds, cashier }) => {
  await page.goto(qrUrl('mesa-12'));
  await page.getByText('Pizza Grande').click();
  await page.getByText('Mussarela').click();
  await page.getByRole('button', { name: 'Enviar pedido' }).click();
  await expect(page.getByText('Pedido recebido')).toBeVisible();

  // KDS recebe em menos de 2 s
  await expect(kds.getByTestId('order-card')).toBeVisible({ timeout: 2000 });
  await kds.keyboard.type('47');
  await kds.keyboard.press('Enter');                       // FIRED
  await expect(page.getByText('Em produção')).toBeVisible();

  await kds.keyboard.type('47'); await kds.keyboard.press('Enter'); // READY
  await expect(page.getByText('Pronto')).toBeVisible();

  await cashier.getByTestId('table-12').click();
  await cashier.getByRole('button', { name: 'Fechar conta' }).click();
  await cashier.getByLabel('Dinheiro').fill('60,00');
  await cashier.getByRole('button', { name: 'Confirmar' }).click();
  await expect(cashier.getByText('Mesa liberada')).toBeVisible();

  // todos os carimbos existem
  const order = await api.getOrder(orderId);
  expect(order.placedAt).toBeTruthy();
  expect(order.firedAt).toBeTruthy();
  expect(order.readyAt).toBeTruthy();
  expect(order.servedAt).toBeTruthy();
});
```

---

## 6. Teste de caos offline — o mais específico deste projeto

Executado diariamente em ambiente dedicado, com corte real de rede (regra de firewall no container).

| # | Cenário | Resultado esperado |
|---|---|---|
| C-01 | Corte de internet durante o serviço | Toda a operação continua; eventos acumulam |
| C-02 | Reconexão após 6 h com 4.000 eventos | Sincroniza em < 5 min, sem duplicar, com horários corretos |
| C-03 | Queda no meio de um lote de sync | Retoma do último confirmado; sem perda nem duplicação |
| C-04 | Reenvio do mesmo lote | Duplicados ignorados; contagem reportada |
| C-05 | Relógio do edge adiantado 10 min | Divergência detectada e sinalizada |
| C-06 | Compra na nuvem + baixa offline no mesmo insumo | Ambos os movimentos aplicados; saldo correto |
| C-07 | Servidor local reiniciado no pico | Volta em < 60 s; nenhum pedido perdido |
| C-08 | WebSocket derrubado | Fallback de polling em ≤ 5 s; sinalização visual |
| C-09 | Disco do edge cheio | Falha controlada com alerta; sem corrupção |
| C-10 | Dois dispositivos avançam o mesmo item | Idempotência: um avanço só |

### 6.1 Verificação de integridade pós-caos

```sql
-- nenhum evento duplicado
SELECT id, count(*) FROM domain_event GROUP BY id HAVING count(*) > 1;

-- nenhum pedido sem evento de origem
SELECT o.id FROM "order" o
LEFT JOIN domain_event e
  ON e.aggregate_id = o.id AND e.type = 'order.placed'
WHERE e.id IS NULL;

-- saldo materializado bate com a soma dos movimentos
SELECT i.id, i.current_stock, COALESCE(SUM(m.quantity),0) AS calculated
FROM ingredient i
LEFT JOIN stock_movement m ON m.ingredient_id = i.id
GROUP BY i.id, i.current_stock
HAVING i.current_stock <> COALESCE(SUM(m.quantity),0);

-- nenhum outbox pendente após a janela de sync
SELECT count(*) FROM outbox WHERE status = 'PENDING'
  AND created_at < now() - interval '10 minutes';
```

---

## 7. Teste de carga

k6, simulando o pico real de uma pizzaria.

| # | Cenário | Carga | Critério |
|---|---|---|---|
| L-01 | Pico de sexta | 120 pedidos/h por 2 h | p95 pedido→KDS < 2 s |
| L-02 | Rajada | 20 pedidos em 60 s | Nenhum erro; fila consistente |
| L-03 | KDS carregado | 100 itens na fila | Render < 500 ms; toque < 300 ms |
| L-04 | Cardápio simultâneo | 80 clientes | p75 carregamento < 2 s |
| L-05 | Painel sob carga | 10 gestores + operação | p95 < 3 s |
| L-06 | Sync em massa | 10.000 eventos | < 60 s, sem erro |
| L-07 | Multi-tenant na nuvem | 50 lojas ativas | Sem degradação cruzada |

---

## 8. Testes de segurança

### 8.1 Isolamento multi-tenant — bloqueante em todo PR

```ts
describe('Isolamento entre tenants', () => {
  it.each(ALL_BUSINESS_TABLES)('tabela %s não vaza entre tenants', async (table) => {
    const a = await createTenantWithData();
    const b = await createTenantWithData();

    await withTenant(a.id, async () => {
      const rows = await prisma.$queryRawUnsafe(`SELECT * FROM ${table}`);
      expect(rows.every(r => r.tenant_id === a.id)).toBe(true);
    });
  });

  it('acesso direto por ID de outro tenant retorna 404', async () => {
    const res = await api.as(userOfTenantA).get(`/v1/orders/${orderOfTenantB.id}`);
    expect(res.status).toBe(404);          // idêntico a inexistente
    expect(await findAuditLog({ action: 'CROSS_TENANT_ATTEMPT' })).toBeTruthy();
  });

  it('query sem contexto de tenant não retorna nada', async () => {
    const rows = await prismaWithoutTenantContext.order.findMany();
    expect(rows).toHaveLength(0);
  });
});
```

### 8.2 Demais verificações

| Verificação | Frequência |
|---|---|
| SCA de dependências | Todo PR — crítica bloqueia |
| SAST | Todo PR |
| Teste de autorização (cada endpoint × cada papel) | Todo PR |
| Bloqueio de PIN por tentativas | Suíte de integração |
| Imutabilidade da auditoria | Suíte de integração |
| Pentest externo | Antes do go-live e anualmente |
| Revisão de logs (sem dado pessoal) | Mensal |

---

## 9. Testes de usabilidade

Feitos com a equipe real, não com o time de desenvolvimento.

| # | Teste | Participantes | Critério |
|---|---|---|---|
| U-01 | Garçom lança pedido sem treinamento | 3 garçons | ≤ 5 toques; sem ajuda |
| U-02 | Cozinha avança pedido no pico simulado | 2 cozinheiros | 1 toque; sem erro |
| U-03 | Cliente pede pelo QR sem instrução | 5 clientes | ≥ 80% concluem sozinhos |
| U-04 | Caixa fecha conta dividida | 2 operadores | ≤ 2 min |
| U-05 | Gestor interpreta o painel | 1 dono | Responde 5 perguntas de negócio sem apoio |
| U-06 | Legibilidade do KDS a 1,5 m | 3 pessoas | 100% de leitura correta |

> U-05 é o teste que valida a diretriz de "controle e métrica total". Se o dono olha o painel e não consegue responder às próprias perguntas, o painel falhou — independentemente de estar tecnicamente correto.

---

## 10. Testes de aceitação de dado

Categoria específica deste projeto: garantir que **os números estão certos**.

| # | Verificação | Método |
|---|---|---|
| D-01 | Tempo total = soma dos intervalos | Conferência cruzada em amostra |
| D-02 | Faturamento do dia = soma dos pagamentos | Conciliação automática diária |
| D-03 | CMV teórico = soma dos custos dos itens produzidos | Recálculo independente |
| D-04 | Saldo de estoque = soma dos movimentos | Query de integridade |
| D-05 | Agregado horário = recálculo direto dos eventos | Job noturno comparativo |
| D-06 | Métrica por hora usa `occurredAt` | Teste com evento sincronizado atrasado |
| D-07 | Divisão de conta soma exatamente o total | Teste de arredondamento |
| D-08 | Margem = preço − custo, por produto | Conferência em amostra |

```ts
it('D-06 · métrica usa horário de ocorrência, não de sincronização', async () => {
  const event = await createEventOffline({
    type: 'order.placed',
    occurredAt: new Date('2026-07-31T20:03:00Z'),
  });
  await syncAt(new Date('2026-07-31T21:15:00Z'));

  const metrics = await getHourlyMetrics('2026-07-31');
  expect(metrics.find(m => m.hour === '20:00').orders).toBe(1);
  expect(metrics.find(m => m.hour === '21:00')?.orders ?? 0).toBe(0);
});
```

---

## 11. Ambientes

| Ambiente | Uso | Dados |
|---|---|---|
| Local | Desenvolvimento | Seed sintético |
| CI | Pipeline automatizado | Efêmero |
| Staging | Homologação e E2E | Anonimizado |
| Caos | Testes de rede e falha | Sintético |
| Piloto | Dona Betinha real | Produção |
| Produção | Parque instalado | Produção |

> Dado de produção **nunca** é copiado para staging sem anonimização (RNF-LGP).

---

## 12. Pipeline de qualidade

```
Commit
  ├─ Lint + formatação           (10 s)   bloqueante
  ├─ Type check                  (30 s)   bloqueante
  └─ Unitários                   (60 s)   bloqueante

Pull Request
  ├─ Integração                  (5 min)  bloqueante
  ├─ ISOLAMENTO MULTI-TENANT     (30 s)   bloqueante
  ├─ Verificação ADR-013         (5 s)    bloqueante
  ├─ SCA + SAST                  (2 min)  bloqueante se crítico
  ├─ Contrato de API (snapshot)  (20 s)   bloqueante
  └─ Cobertura                   (—)      bloqueante se cair

Merge em main
  ├─ E2E                         (15 min) bloqueante
  ├─ Build de imagens            (5 min)
  └─ Deploy em staging           (2 min)

Noturno
  ├─ Caos offline                (20 min)
  ├─ Integridade de dado         (5 min)
  └─ Recálculo comparativo       (10 min)

Semanal
  └─ Carga                       (30 min)
```

---

## 13. Métricas de qualidade

| Métrica | Alvo |
|---|---|
| Cobertura global | ≥ 70% |
| Cobertura de `packages/domain` | ≥ 90% |
| Testes instáveis (flaky) | < 1% |
| Duração do pipeline de PR | < 10 min |
| Defeitos escapados para produção | < 3 por release |
| Defeitos críticos em produção | 0 |
| Tempo de correção de crítico | < 4 h |
| Reabertura de defeito | < 5% |

---

## 14. Gestão de defeitos

| Severidade | Definição | Prazo | Exemplo |
|---|---|---|---|
| **S1 Crítico** | Operação parada ou dado incorreto em produção | 4 h | Pedido não chega ao KDS; vazamento entre tenants; faturamento errado |
| **S2 Alto** | Função importante quebrada com contorno | 2 dias | Divisão de conta falha; alerta não dispara |
| **S3 Médio** | Função secundária ou incômodo | 1 sprint | Filtro do histórico não funciona |
| **S4 Baixo** | Cosmético | Backlog | Alinhamento de texto |

**Regra:** defeito que produz **número errado** é sempre S1, mesmo que nada trave. É a categoria que corrói a confiança no produto — e a confiança no número é o produto.

---

*Documento 10 do pacote 004_DonaBetinha. Replay Studio.*
