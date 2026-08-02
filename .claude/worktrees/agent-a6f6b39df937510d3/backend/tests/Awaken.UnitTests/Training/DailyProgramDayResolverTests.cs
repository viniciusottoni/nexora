using Awaken.Domain.Services.Training;
using FluentAssertions;

namespace Awaken.UnitTests.Training;

// US-238: rotação cíclica determinística do dia do programa a partir do
// último dia efetivamente concluído (RN-001/RN-007/RN-008). Serviço puro,
// sem I/O — os cenários abaixo seguem as CAs/RNs literalmente da US-238.
public class DailyProgramDayResolverTests
{
    [Fact]
    public void CA001_AbcdeUltimoC_ResolveD()
    {
        var result = DailyProgramDayResolver.Resolve(["A", "B", "C", "D", "E"], "C");

        result.DayKey.Should().Be("D");
        result.DayIndex.Should().Be(4);
        result.Reason.Should().Be("cyclic_successor");
    }

    [Fact]
    public void CA002_AbUltimoB_VoltaParaA()
    {
        var result = DailyProgramDayResolver.Resolve(["A", "B"], "B");

        result.DayKey.Should().Be("A");
        result.Reason.Should().Be("cyclic_successor");
    }

    [Fact]
    public void CA003_SemHistorico_PrimeiroDia()
    {
        var result = DailyProgramDayResolver.Resolve(["A", "B", "C"], null);

        result.DayKey.Should().Be("A");
        result.DayIndex.Should().Be(1);
        result.Reason.Should().Be("first_workout");
    }

    [Fact]
    public void CA005_TrocaDePrograma_LastKeyNaoPerteceAosDias_ReiniciaNoPrimeiro()
    {
        // "Z" simula um dayKey que não pertence ao novo split (troca de programa / dado legado).
        var resultAfterSwitch = DailyProgramDayResolver.Resolve(["A", "B", "C", "D"], "Z");

        resultAfterSwitch.DayKey.Should().Be("A");
        resultAfterSwitch.DayIndex.Should().Be(1);
        resultAfterSwitch.Reason.Should().Be("program_changed");
    }

    [Fact]
    public void CA006_FullBody_SempreResolveUnicoDia()
    {
        var result = DailyProgramDayResolver.Resolve(["FB"], "FB");

        result.DayKey.Should().Be("FB");
        result.DayIndex.Should().Be(1);
        result.Reason.Should().Be("full_body_single_day");
    }

    [Fact]
    public void RN007_FolgaLonga_ContinuaDoUltimoConcluido()
    {
        // Independente de quanto tempo se passou, o resolver só olha lastCompletedDayKey — nunca a data.
        var result = DailyProgramDayResolver.Resolve(["A", "B", "C"], "A");

        result.DayKey.Should().Be("B");
        result.Reason.Should().Be("cyclic_successor");
    }
}
