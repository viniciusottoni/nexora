using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Queries.GetCurrentSessionBill;

/// <summary>
/// Porta de <c>GET /v1/public/sessions/current/bill</c> (US-027 §10: "Cliente pode pré-visualizar a
/// divisão no celular antes de o caixa começar") — SEM parâmetro de sessão, mesmo raciocínio de
/// <see cref="Orders.Queries.GetCurrentSessionConsumption.GetCurrentSessionConsumptionQuery"/>
/// (US-024): a sessão é sempre resolvida pela claim <c>ses</c> do token de sessão de mesa corrente,
/// nunca por um id informado pelo cliente (RN-015). Só leitura — nenhuma escrita (atribuição por
/// item, retirada de taxa, pagamento parcial) é exposta ao cliente final nesta história; essas são
/// ações do caixa, feitas pelos endpoints de staff.
/// </summary>
public sealed record GetCurrentSessionBillQuery(
    string? SplitMode,
    short? People,
    decimal? Amount,
    string? Waived) : IQuery<BillResponse>;
