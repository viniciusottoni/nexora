using Awaken.Domain.Entities.Training;

namespace Awaken.Domain.Repositories;

public interface ITrainingProgramSplitRepository
{
    /// US-237/US-238/US-240: split map ativo de um programa, com os dias
    /// carregados na ordem certa (DayIndex asc). Retorna null para programas
    /// sem split clássico (ex.: perfect_2, system — RN-009).
    Task<TrainingProgramSplit?> GetByProgramKeyAsync(string programKey, CancellationToken ct = default);
}
