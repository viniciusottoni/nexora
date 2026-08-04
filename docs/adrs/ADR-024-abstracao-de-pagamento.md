# ADR-024 · Abstração de provedor de pagamento

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, PO |
| **Relacionados** | ADR-013, ADR-021, ADR-031 |
| **Requisitos afetados** | RF-CXA-03, RF-CXA-09, RF-CXA-10, RF-CXA-11 |

---

## Contexto

A descoberta registrou dois provedores em uso: **maquininha Cielo** (conta Banco do Brasil) e **maquininha Mercado Pago**, além da intenção de pagamento online pelo aplicativo via Mercado Pago.

Duas observações importantes:

1. **Maquininha (TEF) e gateway online são coisas técnicas distintas.** Uma exige integração com terminal físico via Pinpad ou API local; a outra é uma API web. O cliente citou ambas, mas ainda **[PENDÊNCIA]** definir o comportamento esperado em cada canal.
2. Como o produto é replicável, o próximo estabelecimento pode usar Stone, PagSeguro, SumUp ou qualquer outro. Acoplar o código a um provedor específico violaria o ADR-013 na prática.

## Decisão

**Toda integração de pagamento vive atrás de uma interface de domínio (`PaymentProvider`), com adaptadores por provedor, selecionados por configuração do tenant.**

O provedor `MANUAL` — registro da forma de pagamento sem integração — é sempre disponível e é o **fallback obrigatório**.

## Detalhamento

### Interface

```csharp
// Nexora.Domain/Payment/IPaymentProvider.cs
public interface IPaymentProvider
{
    string Code { get; }
    IReadOnlyList<PaymentCapability> Capabilities { get; }   // Online | Terminal | Pix | Refund | Reconcile

    Task<Charge> CreateChargeAsync(CreateChargeInput input, CancellationToken ct = default);
    Task<Charge> GetChargeAsync(string reference, CancellationToken ct = default);
    Task<Refund> RefundAsync(string reference, decimal amount, string reason, CancellationToken ct = default);
    Task<PaymentEvent> HandleWebhookAsync(object payload, string signature, CancellationToken ct = default);
    Task<IReadOnlyList<Settlement>> ReconcileAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
```

`HandleWebhookAsync` e `ReconcileAsync` são opcionais na prática: adaptadores que não suportam a capacidade correspondente lançam `NotSupportedException`, verificado antes da chamada via `Capabilities`.

### Adaptadores previstos

| Adaptador | Capacidades | Fase |
|---|---|---|
| `manual` | Registro apenas | 1 |
| `mercadopago-online` | ONLINE, PIX, REFUND, webhook | 4 |
| `mercadopago-terminal` | TERMINAL | **[PENDÊNCIA]** |
| `cielo-terminal` | TERMINAL | **[PENDÊNCIA]** |
| `pix-direct` | PIX | A avaliar |

### Seleção por configuração

```json
{
  "payments": {
    "dineIn":  { "provider": "manual",  "methods": ["CASH","CREDIT","DEBIT","PIX"] },
    "delivery":{ "provider": "mercadopago-online", "methods": ["ONLINE","PIX","CASH"] }
  }
}
```

Nenhum código verifica qual é o provedor. O container de injeção de dependência do ASP.NET Core resolve o adaptador correto (`IPaymentProvider`) a partir da configuração, via uma factory registrada em `Nexora.Infrastructure` (`services.AddKeyedScoped<IPaymentProvider, ...>(...)` ou factory equivalente resolvida em tempo de execução).

### Fallback obrigatório

```
Provedor indisponível (503 DEPENDENCY_UNAVAILABLE)
   │
   ▼ Sistema oferece: "Registrar pagamento manualmente"
   │
   ▼ Venda é concluída; conciliação posterior sinaliza a pendência
```

A operação **nunca** pode parar por indisponibilidade de adquirente (RNF-DIS-08). Numa pizzaria cheia, isso é inegociável.

### Comportamento offline

Pagamento presencial em maquininha física funciona offline por natureza — a maquininha tem a própria conectividade. O sistema apenas **registra** a forma, o valor e a referência. Pagamento online exige internet e degrada junto com o canal de delivery.

### Segurança

| Regra | Motivo |
|---|---|
| Credenciais nunca no cliente | Sempre server-side (ADR-031) |
| Nenhum dado de cartão trafega ou é armazenado | Fora do escopo de PCI-DSS por construção |
| Webhook com verificação de assinatura | Impede notificação forjada |
| Webhook idempotente por `providerRef` | Reenvio do provedor não duplica |

### Conciliação

O adaptador que implementa `reconcile()` permite comparar o registrado no sistema com o liquidado pelo provedor, apurando taxa efetiva por transação (RF-FIN-10) — número que costuma ser invisível ao dono.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Integrar diretamente com Mercado Pago | Mais rápido de fazer | Próximo cliente com outro provedor exigiria refazer | Viola ADR-013 na prática |
| Usar um agregador (Pagar.me, Asaas) | Um só contrato técnico | Custo adicional; o cliente já tem contratos | Não elimina a necessidade da abstração |
| Só registro manual, sem integração | Simples | Não atende ao pagamento online do delivery | Falha em RF-CXA-09 |
| Adaptador escolhido em build | — | Impediria configuração por tenant | Viola ADR-010 e ADR-013 |

## Consequências

**Positivas**

- Novo provedor é um adaptador novo, sem tocar no domínio
- Cada estabelecimento usa o adquirente que já tem contratado
- Fallback manual garante que a venda nunca para
- Conciliação revela o custo real de taxa por transação

**Negativas**

- Camada de abstração adiciona indireção
- Nem todo provedor implementa todas as capacidades
- Adaptadores de terminal dependem de hardware e SDK específicos

**Mitigações**

- `capabilities` declara explicitamente o que o adaptador suporta; a interface se adapta
- Adaptador `manual` sempre presente
- Testes de contrato do adaptador rodam contra ambiente sandbox do provedor

## Como validar

- Teste de contrato: todo adaptador satisfaz a interface e declara capacidades corretamente
- Teste: provedor indisponível → sistema oferece registro manual e conclui a venda
- Teste: webhook reenviado não duplica pagamento
- Teste: webhook com assinatura inválida é rejeitado e registrado

## Revisitar quando

- **[PENDÊNCIA] resolvida:** o cliente definir se quer integração TEF real com as maquininhas ou apenas registro da forma de pagamento. A decisão altera escopo e custo da Fase 3.
