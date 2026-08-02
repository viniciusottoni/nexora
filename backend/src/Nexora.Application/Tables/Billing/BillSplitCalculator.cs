namespace Nexora.Application.Tables.Billing;

/// <summary>
/// US-027 (Dividir a conta) — função PURA de cálculo de divisão de conta, sem efeito colateral e
/// sem dependência de EF Core/MediatR: recebe itens/valores já carregados pelo handler e devolve as
/// partes. Deliberadamente sem acesso a banco (US-027 §9: "Cálculo integralmente local, em função
/// pura... a divisão precisa funcionar com internet caída").
/// </summary>
/// <remarks>
/// <para>
/// <b>Invariante normativa (ADR-017 + US-027 §12):</b> a soma de <c>Parts[].Amount</c> é SEMPRE
/// exatamente igual a <c>Total</c>. Isso é garantido por construção, nunca por arredondamento
/// "torcido" no fim: toda distribuição de centavos (<see cref="DistributeEqually"/> e
/// <see cref="DistributeProportional"/>) fecha a diferença de arredondamento na PRIMEIRA posição da
/// lista ordenada — a mesma regra ("a sobra vai para a primeira parcela") usada em toda a solution
/// para dinheiro.
/// </para>
/// <para>
/// <b>Taxa de serviço e retirada por parte (RN-010):</b> quando uma pessoa opta por não pagar a
/// taxa, o valor dela simplesmente deixa de ser cobrado — não é redistribuído para as demais partes
/// (a casa recebe menos taxa no total, não repassa a diferença para o grupo). Por isso
/// <see cref="BillSplitResult.Total"/> é o total EFETIVAMENTE devido (subtotal + taxa apenas das
/// partes que não retiraram), não um total nominal fixo — o que mantém a invariante
/// "soma das partes == total" válida mesmo com retirada parcial.
/// </para>
/// </remarks>
public static class BillSplitCalculator
{
    /// <summary>
    /// Divide <paramref name="total"/> em <paramref name="parts"/> pedaços de duas casas decimais,
    /// cuja soma é EXATAMENTE <paramref name="total"/> — sobra do arredondamento vai para o índice 0
    /// (ADR-017: "a sobra vai para a primeira parcela"). Ex.: R$100,00 ÷ 3 = [33.34, 33.33, 33.33].
    /// </summary>
    public static IReadOnlyList<decimal> DistributeEqually(decimal total, int parts)
    {
        if (parts < 1)
            throw new ArgumentOutOfRangeException(nameof(parts), parts, "O número de partes precisa ser pelo menos 1.");

        var baseShare = Math.Round(total / parts, 2, MidpointRounding.AwayFromZero);
        var shares = new decimal[parts];
        for (var i = 0; i < parts; i++)
        {
            shares[i] = baseShare;
        }

        var residue = total - (baseShare * parts);
        shares[0] += residue;

        return shares;
    }

    /// <summary>
    /// Distribui <paramref name="pool"/> proporcionalmente aos <paramref name="weights"/> (ex.: taxa
    /// de serviço proporcional ao subtotal de cada parte), com a MESMA garantia de soma exata —
    /// resíduo do arredondamento vai para o índice 0. Peso zero gera parte zero (pessoa sem item
    /// atribuído não paga taxa de ninguém).
    /// </summary>
    public static IReadOnlyList<decimal> DistributeProportional(decimal pool, IReadOnlyList<decimal> weights)
    {
        if (weights.Count == 0)
            throw new ArgumentException("É preciso pelo menos um peso para distribuir proporcionalmente.", nameof(weights));

        var totalWeight = weights.Sum();
        var shares = new decimal[weights.Count];

        if (totalWeight <= 0)
        {
            // Sem base nenhuma para proporção (ex.: subtotal zerado) — sobra inteira na primeira posição.
            shares[0] = pool;
            return shares;
        }

        for (var i = 0; i < weights.Count; i++)
        {
            shares[i] = Math.Round(pool * weights[i] / totalWeight, 2, MidpointRounding.AwayFromZero);
        }

        var residue = pool - shares.Sum();
        shares[0] += residue;

        return shares;
    }

    /// <summary>
    /// Modo <c>BY_PERSON</c> (US-027 §4, cenário "Divisão por pessoa com resíduo"): total dividido
    /// igualmente por <paramref name="people"/>, taxa de serviço proporcional (aqui, igual, já que
    /// as partes do subtotal são iguais) a cada parte, com possibilidade de retirada individual da
    /// taxa (<paramref name="serviceFeeWaivedPersons"/>, RN-010).
    /// </summary>
    public static BillSplitResult CalculateByPerson(
        decimal subtotal,
        decimal serviceFeePercent,
        int people,
        IReadOnlySet<int>? serviceFeeWaivedPersons = null)
    {
        if (people < 1)
            throw new ArgumentOutOfRangeException(nameof(people), people, "A conta precisa ser dividida entre pelo menos 1 pessoa.");

        var waived = serviceFeeWaivedPersons ?? new HashSet<int>();
        var serviceFeeNominal = Math.Round(subtotal * serviceFeePercent / 100m, 2, MidpointRounding.AwayFromZero);

        var baseShares = DistributeEqually(subtotal, people);
        var feeShares = DistributeProportional(serviceFeeNominal, baseShares);

        var parts = new List<BillSplitPart>(people);
        for (var i = 0; i < people; i++)
        {
            var person = i + 1;
            var isWaived = waived.Contains(person);
            var feeAmount = isWaived ? 0m : feeShares[i];
            parts.Add(new BillSplitPart(person, baseShares[i] + feeAmount, feeAmount, isWaived));
        }

        var total = subtotal + parts.Sum(p => p.ServiceFeeAmount);

        return new BillSplitResult(subtotal, serviceFeeNominal, total, parts, UnassignedItemIds: Array.Empty<Guid>());
    }

    /// <summary>
    /// Modo <c>BY_ITEM</c> (US-027 §4, cenário "Divisão por item"): cada pessoa assume o subtotal
    /// exato dos itens que lhe foram atribuídos; taxa de serviço proporcional ao subtotal de cada
    /// pessoa. Itens sem <see cref="BillSplitItem.AssignedPerson"/> aparecem em
    /// <see cref="BillSplitResult.UnassignedItemIds"/> e NÃO entram no cálculo das partes — cabe ao
    /// chamador (o handler da Application) recusar a conclusão quando essa lista não está vazia
    /// (RN-017/US-027 §5, código <c>BILL_ITEM_NOT_ASSIGNED</c>); este método nunca lança exceção
    /// para item órfão, porque uma consulta apenas informativa (ex.: pré-visualização) precisa
    /// funcionar mesmo com atribuição incompleta.
    /// </summary>
    public static BillSplitResult CalculateByItem(
        IReadOnlyList<BillSplitItem> items,
        decimal serviceFeePercent,
        IReadOnlySet<int>? serviceFeeWaivedPersons = null)
    {
        var waived = serviceFeeWaivedPersons ?? new HashSet<int>();

        var unassigned = items.Where(i => i.AssignedPerson is null).Select(i => i.Id).ToArray();

        var byPerson = items
            .Where(i => i.AssignedPerson is not null)
            .GroupBy(i => i.AssignedPerson!.Value)
            .OrderBy(g => g.Key)
            .Select(g => (Person: g.Key, Subtotal: g.Sum(i => i.Total)))
            .ToList();

        var assignedSubtotal = byPerson.Sum(p => p.Subtotal);
        var serviceFeeNominal = Math.Round(assignedSubtotal * serviceFeePercent / 100m, 2, MidpointRounding.AwayFromZero);

        if (byPerson.Count == 0)
        {
            return new BillSplitResult(assignedSubtotal, serviceFeeNominal, assignedSubtotal, Array.Empty<BillSplitPart>(), unassigned);
        }

        var weights = byPerson.Select(p => p.Subtotal).ToList();
        var feeShares = DistributeProportional(serviceFeeNominal, weights);

        var parts = new List<BillSplitPart>(byPerson.Count);
        for (var i = 0; i < byPerson.Count; i++)
        {
            var isWaived = waived.Contains(byPerson[i].Person);
            var feeAmount = isWaived ? 0m : feeShares[i];
            parts.Add(new BillSplitPart(byPerson[i].Person, byPerson[i].Subtotal + feeAmount, feeAmount, isWaived));
        }

        var total = assignedSubtotal + parts.Sum(p => p.ServiceFeeAmount);

        return new BillSplitResult(assignedSubtotal, serviceFeeNominal, total, parts, unassigned);
    }

    /// <summary>
    /// Modo <c>BY_AMOUNT</c> (US-027 §4, cenário "Divisão por valor"): alguém paga um valor
    /// arbitrário; o resto fica em aberto. Não gera <see cref="BillSplitResult.Parts"/> nomeadas —
    /// é o chamador (comando de registro de pagamento parcial) quem de fato cria o <c>Payment</c>;
    /// este método só calcula o saldo, com a MESMA garantia de soma exata:
    /// <c>AlreadyPaid + AmountNow + Remaining == Total</c>, sempre.
    /// </summary>
    public static BillAmountSplit CalculateByAmount(decimal total, decimal alreadyPaid, decimal amountNow)
    {
        if (amountNow <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountNow), amountNow, "O valor pago precisa ser maior que zero.");

        var openBalance = total - alreadyPaid;
        if (amountNow > openBalance)
            throw new ArgumentOutOfRangeException(nameof(amountNow), amountNow, "O valor pago não pode ser maior que o saldo em aberto.");

        var remaining = openBalance - amountNow;

        return new BillAmountSplit(total, alreadyPaid, amountNow, remaining);
    }
}

/// <summary>Item a dividir — projeção mínima de <c>OrderItem</c> (Domain) que o calculador precisa enxergar.</summary>
public sealed record BillSplitItem(Guid Id, string Name, decimal Total, bool Pending, int? AssignedPerson);

/// <summary>Parte de uma divisão (uma pessoa) — <see cref="Amount"/> já inclui <see cref="ServiceFeeAmount"/>.</summary>
public sealed record BillSplitPart(int Person, decimal Amount, decimal ServiceFeeAmount, bool ServiceFeeWaived);

/// <summary>
/// Resultado de <see cref="BillSplitCalculator.CalculateByPerson"/>/<see cref="BillSplitCalculator.CalculateByItem"/>.
/// <see cref="Total"/> é sempre exatamente igual à soma de <see cref="Parts"/>[].Amount (US-027 §12).
/// </summary>
public sealed record BillSplitResult(
    decimal Subtotal,
    decimal ServiceFeeNominal,
    decimal Total,
    IReadOnlyList<BillSplitPart> Parts,
    IReadOnlyList<Guid> UnassignedItemIds);

/// <summary>Resultado de <see cref="BillSplitCalculator.CalculateByAmount"/> — <c>AlreadyPaid + AmountNow + Remaining == Total</c>, sempre.</summary>
public sealed record BillAmountSplit(decimal Total, decimal AlreadyPaid, decimal AmountNow, decimal Remaining);
