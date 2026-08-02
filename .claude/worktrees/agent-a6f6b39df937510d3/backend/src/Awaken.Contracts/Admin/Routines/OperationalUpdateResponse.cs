namespace Awaken.Contracts.Admin.Routines;

/// <summary>
/// US-221 (RN-004): item de histórico de atualização operacional controlada.
/// Fora desta US: não existe ainda uma fonte real desse histórico (nenhuma tabela/entidade dedicada) —
/// ver RoutinesOverviewResponse.OperationalUpdatesAvailable. Quando indisponível, a lista vem vazia.
/// </summary>
public record OperationalUpdateResponse(
    string Id,
    string Description,
    string Status,
    DateTime StartedAtUtc,
    DateTime? FinishedAtUtc,
    double? DurationSeconds);
