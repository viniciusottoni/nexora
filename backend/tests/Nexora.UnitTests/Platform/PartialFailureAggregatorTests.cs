using Nexora.Application.Platform.Support;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — Gherkin "Falha parcial": "dados
/// administrativos disponíveis devem continuar visíveis" e "a seção de saúde deve indicar falha".
/// Testa o mecanismo de isolamento de falha por fonte de forma totalmente determinística (delegados
/// em memória, sem banco) — prova exatamente o contrato que <c>GetAttentionQueueQueryHandler</c>
/// depende para não derrubar a fila inteira quando UMA fonte (ex.: saúde de instalação) lança.
/// </summary>
public sealed class PartialFailureAggregatorTests
{
    [Fact]
    public async Task Todas_As_Fontes_Bem_Sucedidas_Retorna_Itens_De_Todas_Sem_Fonte_Indisponivel()
    {
        var sources = new List<AttentionSource<string>>
        {
            new("A", _ => Task.FromResult<IReadOnlyList<string>>(new[] { "a1", "a2" })),
            new("B", _ => Task.FromResult<IReadOnlyList<string>>(new[] { "b1" })),
        };

        var result = await PartialFailureAggregator.CollectAsync(sources, CancellationToken.None);

        result.Items.Should().BeEquivalentTo(new[] { "a1", "a2", "b1" });
        result.UnavailableSources.Should().BeEmpty();
    }

    [Fact]
    public async Task Uma_Fonte_Falhando_Nao_Derruba_As_Outras_E_E_Marcada_Indisponivel()
    {
        var sources = new List<AttentionSource<string>>
        {
            new("INSTALLATION_HEALTH", _ => throw new InvalidOperationException("saúde de instalação temporariamente indisponível")),
            new("OWNER_INVITES", _ => Task.FromResult<IReadOnlyList<string>>(new[] { "invite-1" })),
            new("PROVISIONING_LIFECYCLE", _ => Task.FromResult<IReadOnlyList<string>>(new[] { "tenant-1" })),
        };

        var result = await PartialFailureAggregator.CollectAsync(sources, CancellationToken.None);

        result.Items.Should().BeEquivalentTo(new[] { "invite-1", "tenant-1" },
            "as fontes que não falharam devem continuar visíveis");
        result.UnavailableSources.Should().ContainSingle().Which.Should().Be("INSTALLATION_HEALTH");
    }

    [Fact]
    public async Task Fonte_Que_Lanca_Exception_Assincrona_Tambem_E_Isolada()
    {
        var sources = new List<AttentionSource<string>>
        {
            new("SLOW_SOURCE", async ct =>
            {
                await Task.Yield();
                throw new TimeoutException("fonte lenta expirou");
            }),
            new("FAST_SOURCE", _ => Task.FromResult<IReadOnlyList<string>>(new[] { "ok" })),
        };

        var result = await PartialFailureAggregator.CollectAsync(sources, CancellationToken.None);

        result.Items.Should().Equal("ok");
        result.UnavailableSources.Should().Equal("SLOW_SOURCE");
    }

    [Fact]
    public async Task Todas_As_Fontes_Falhando_Retorna_Lista_Vazia_Com_Todas_Marcadas_Indisponiveis()
    {
        var sources = new List<AttentionSource<string>>
        {
            new("A", _ => throw new InvalidOperationException("falha A")),
            new("B", _ => throw new InvalidOperationException("falha B")),
        };

        var result = await PartialFailureAggregator.CollectAsync(sources, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.UnavailableSources.Should().BeEquivalentTo(new[] { "A", "B" });
    }
}
