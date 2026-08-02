namespace Awaken.Contracts.BattleLog;

public record BattleLogItemResponse(
    Guid QuestLogId,
    Guid QuestId,
    string QuestType,
    long XpEarned,
    long? XpPenaltyApplied,
    IReadOnlyList<string> ItemsEarned,
    DateTime CompletedAt);

public record BattleLogResponse(IReadOnlyList<BattleLogItemResponse> Items);

/// US-083: metadados de paginacao para assinantes.
public record BattleLogPageMetadata(int Page, int PageSize, bool HasMore);

/// US-083: historico completo paginado para assinantes.
public record PagedBattleLogResponse(
    IReadOnlyList<BattleLogItemResponse> Items,
    BattleLogPageMetadata Metadata);

/// US-209: resposta de paginacao por cursor — estavel sob insercoes concorrentes.
public record CursorPagedResponse<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore);
