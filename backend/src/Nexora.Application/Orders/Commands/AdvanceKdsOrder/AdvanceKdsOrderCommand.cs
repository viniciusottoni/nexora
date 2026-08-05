using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.AdvanceKdsOrder;

/// <summary>
/// Porta de <c>POST /v1/kds/orders/{shortCode}/advance</c> (US-041 §1/§3/§7) — o caminho PRINCIPAL
/// do teclado numérico: o operador digita o código curto do pedido e aperta Enter.
///
/// [DECISÃO DE ESCOPO] O documento da história descreve um único fluxo de "digitar código +
/// Enter" que ora se comporta como "avança o item" (cenário "Sequência completa do ciclo", pedido
/// de um item só) ora como "avança em lote" (cenário "Avanço em lote do pedido", pedido de vários
/// itens, com "confirmar o avanço em lote" como passo explícito à parte). Os dois cenários
/// convergem neste único comando com <see cref="Batch"/>: sem confirmação
/// (<c>Batch=false</c>, o Enter comum) avança só o item MAIS ANTIGO ainda ativo do pedido NESTA
/// praça — em um pedido de um item só (o caso comum de pizza) isso já cobre exatamente o cenário
/// "digitar 4 vezes percorre os 4 estados"; com a confirmação de lote (<c>Batch=true</c>) avança
/// TODOS os itens ativos do pedido nesta praça, cada um um passo, cada um com seu próprio evento —
/// nunca junta os dois em uma transação parcial (cada item avança de forma independente; se um
/// falhar os demais já avançados continuam avançados, mesma filosofia de "não travar a fila por um
/// item problemático" do resto do domínio).
/// </summary>
public sealed record AdvanceKdsOrderCommand(
    string ShortCode,
    Guid StationId,
    bool Batch,
    DateTimeOffset? OccurredAt = null) : ICommand<AdvanceKdsOrderResponse>;
