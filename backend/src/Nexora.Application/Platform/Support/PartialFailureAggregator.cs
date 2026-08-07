namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — mecanismo de isolamento de falha
/// por FONTE (Gherkin "Falha parcial": "dados administrativos disponíveis devem continuar visíveis"
/// e "a seção de saúde deve indicar falha"). Cada fonte da fila de atenção (saúde de instalação,
/// convites, ciclo de vida do provisionamento) é buscada de forma independente — uma fonte lançando
/// exceção nunca derruba as outras nem a resposta inteira. Classe genérica e sem I/O próprio (as
/// funções de busca são passadas pelo chamador), por isso testável sem banco: dado um conjunto de
/// funções, algumas que lançam e outras que não, prova que só as que lançaram aparecem em
/// <c>UnavailableSources</c> e que os itens das demais chegam intactos.
/// </summary>
public static class PartialFailureAggregator
{
    public static async Task<PartialFailureResult<TItem>> CollectAsync<TItem>(
        IReadOnlyList<AttentionSource<TItem>> sources,
        CancellationToken cancellationToken)
    {
        var items = new List<TItem>();
        var unavailable = new List<string>();

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fetched = await source.Fetch(cancellationToken);
                items.AddRange(fetched);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested is false)
            {
                // Falha isolada por fonte (Gherkin "Falha parcial") — nunca propaga: as demais
                // fontes continuam sendo tentadas e o que já foi coletado permanece na resposta.
                unavailable.Add(source.Name);
            }
        }

        return new PartialFailureResult<TItem>(items, unavailable);
    }
}

/// <summary>Uma fonte nomeada de itens — <paramref name="Name"/> é o identificador estável que aparece em <c>unavailableSources</c> quando <paramref name="Fetch"/> lança.</summary>
public sealed record AttentionSource<TItem>(string Name, Func<CancellationToken, Task<IReadOnlyList<TItem>>> Fetch);

public sealed record PartialFailureResult<TItem>(IReadOnlyList<TItem> Items, IReadOnlyList<string> UnavailableSources);
