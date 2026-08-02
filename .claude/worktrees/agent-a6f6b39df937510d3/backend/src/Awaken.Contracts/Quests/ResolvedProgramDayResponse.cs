namespace Awaken.Contracts.Quests;

/// US-238 §18: resposta de auditoria do dia do programa resolvido pela rotação cíclica.
public record ResolvedProgramDayResponse(
    string ProgramKey,
    string? LastCompletedDayKey,
    string ResolvedDayKey,
    int ResolvedDayIndex,
    string SplitMapVersion,
    string Reason);
