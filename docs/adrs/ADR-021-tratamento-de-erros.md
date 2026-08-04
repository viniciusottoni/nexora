# ADR-021 · Tratamento e taxonomia de erros

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead |
| **Relacionados** | ADR-003, ADR-022 |
| **Requisitos afetados** | RNF-MAN-06, RNF-USA-12, RNF-SEG-15 |

---

## Contexto

Erro em sistema operacional de restaurante tem uma característica particular: **quem o recebe está sob pressão e não é técnico**. Um garçom no meio do salão precisa saber, em dois segundos, se deve tentar de novo, chamar o gerente ou seguir em frente.

Além disso, o cliente precisa distinguir programaticamente entre erro recuperável (reenviar), erro de regra (mostrar mensagem) e erro que exige autorização (pedir PIN do gerente) — comportamentos completamente diferentes.

Há ainda uma exigência de segurança: erro não pode vazar existência de recurso de outro tenant (ADR-004) nem dado pessoal em log (RNF-SEG-15).

## Decisão

**Erros seguem RFC 7807 (Problem Details), com taxonomia própria em `code` e classificação explícita de recuperabilidade.**

## Detalhamento

### Formato

```json
{
  "type": "https://docs.<plataforma>/errors/insufficient-stock",
  "title": "Estoque insuficiente",
  "status": 422,
  "detail": "O insumo 'Mussarela' não possui saldo para produzir este item.",
  "instance": "/v1/orders/018f2c4a.../items",
  "code": "STOCK_INSUFFICIENT",
  "recoverable": false,
  "requiresAuthorization": false,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "meta": { "ingredientId": "...", "required": "0.350", "available": "0.120" }
}
```

| Campo | Uso |
|---|---|
| `code` | Chave estável para o cliente decidir o comportamento |
| `title` / `detail` | Texto em português, exibível ao usuário final |
| `recoverable` | Se `true`, o cliente pode reenviar automaticamente |
| `requiresAuthorization` | Se `true`, o cliente abre o diálogo de PIN do gerente (ADR-014) |
| `traceId` | Correlação com o log (ADR-022) — o suporte pede esse número |
| `meta` | Dados estruturados, nunca texto solto |

### Taxonomia

| Família | HTTP | Exemplos | Comportamento do cliente |
|---|---|---|---|
| `VALIDATION_*` | 400 | `VALIDATION_FAILED` | Corrigir entrada; destacar campo |
| `AUTH_*` | 401 | `AUTH_TOKEN_EXPIRED` | Renovar token e repetir |
| `PERMISSION_*` | 403 | `PERMISSION_DENIED` | Mensagem clara; sem retry |
| `AUTHORIZATION_REQUIRED` | 403 | — | **Abrir diálogo de PIN do gerente** |
| `NOT_FOUND` | 404 | — | Mensagem neutra |
| `CONFLICT_*` | 409 | `INVALID_STATE_TRANSITION`, `REQUEST_IN_PROGRESS` | Recarregar estado; possivelmente repetir |
| `BUSINESS_*` | 422 | `STOCK_INSUFFICIENT`, `PRODUCT_UNAVAILABLE`, `DISCOUNT_ABOVE_LIMIT` | Mensagem ao usuário; sem retry |
| `IDEMPOTENCY_*` | 422 | `IDEMPOTENCY_KEY_REUSED` | Bug do cliente; reportar |
| `RATE_LIMIT` | 429 | — | Backoff |
| `INTERNAL_*` | 500 | — | Retry com backoff; reportar |
| `DEPENDENCY_*` | 503 | `PAYMENT_PROVIDER_UNAVAILABLE` | Oferecer caminho alternativo |

### Hierarquia no domínio

```csharp
// Nexora.Domain/Errors
public abstract class DomainError : Exception
{
    public abstract string Code { get; }
    public abstract int Status { get; }
    public virtual bool Recoverable => false;
    public virtual bool RequiresAuthorization => false;
    public IReadOnlyDictionary<string, object>? Meta { get; }

    protected DomainError(string message, IReadOnlyDictionary<string, object>? meta = null)
        : base(message) => Meta = meta;
}

public sealed class InsufficientStockError : DomainError
{
    public override string Code => "STOCK_INSUFFICIENT";
    public override int Status => 422;

    public InsufficientStockError(string message, IReadOnlyDictionary<string, object>? meta = null)
        : base(message, meta) { }
}

public sealed class AuthorizationRequiredError : DomainError
{
    public override string Code => "AUTHORIZATION_REQUIRED";
    public override int Status => 403;
    public override bool RequiresAuthorization => true;

    public AuthorizationRequiredError(string message) : base(message) { }
}
```

O suporte **nativo** do ASP.NET Core a `ProblemDetails` (`AddProblemDetails()` + `IExceptionHandler` customizado) converte `DomainError` no formato acima — sem exigir um filtro de exceção construído à mão como o `ExceptionFilter` do NestJS original. É uma simplificação real: o framework já fala RFC 7807 por padrão, o trabalho do time se resume a mapear `DomainError` → `ProblemDetails` uma única vez, em `Nexora.Api.Edge`/`Api.Cloud`. Erro não mapeado vira `INTERNAL_ERROR` com `traceId`, **sem expor stack trace nem mensagem interna**.

### Regras de segurança

| Regra | Motivo |
|---|---|
| Recurso de outro tenant retorna **404**, nunca 403 | 403 confirmaria a existência do recurso |
| `detail` nunca contém dado pessoal, token ou consulta SQL | RNF-SEG-15 |
| Stack trace só em log, nunca na resposta | — |
| Mensagem de erro de autenticação é genérica | Não revela se o usuário existe |

### Erros na operação offline

O cliente distingue três situações e as trata de forma diferente:

| Situação | Tratamento |
|---|---|
| Sem conexão com o edge | Enfileira localmente (ADR-027) — não é erro visível |
| Edge respondeu com erro de regra | Mostra ao usuário; não enfileira |
| Edge respondeu 5xx | Enfileira e tenta novamente com backoff |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Só código HTTP | Simples | Insuficiente para o cliente decidir comportamento | 422 pode ser dez coisas diferentes |
| Formato próprio | Liberdade | Reinventa padrão existente | RFC 7807 é bem suportado |
| GraphQL errors | Rico | Não usamos GraphQL | — |
| Mensagens só em inglês | Padrão técnico | Usuário final é o garçom | Exigiria camada de tradução no cliente |

## Consequências

**Positivas**

- Cliente decide o comportamento pelo `code`, não por texto
- `requiresAuthorization` habilita o fluxo de PIN do gerente sem lógica ad hoc
- `traceId` na resposta acelera o suporte drasticamente
- Formato padronizado facilita cliente gerado a partir do OpenAPI
- Suporte nativo do ASP.NET Core a `ProblemDetails` torna a implementação mais simples que o `ExceptionFilter` customizado do NestJS — menos código de infraestrutura para manter

**Negativas**

- Mais classes de erro para manter
- Textos em português no domínio (aceitável: um idioma por ora)
- Risco de proliferação de códigos

**Mitigações**

- Catálogo de códigos centralizado em `Nexora.Shared/Errors/ApiErrorCodes.cs`
- Código novo exige entrada no catálogo e no OpenAPI (verificado no CI)
- Revisão periódica de códigos não utilizados

## Como validar

- Todo `DomainError` mapeado para código HTTP e `code` correspondente
- Teste: acesso a recurso de outro tenant retorna 404 com corpo idêntico ao de recurso inexistente
- Teste: nenhum log contém dado pessoal ou token (varredura automatizada)
- Teste E2E: erro `AUTHORIZATION_REQUIRED` abre o diálogo de PIN no cliente

## Revisitar quando

- O produto precisar de mais de um idioma
