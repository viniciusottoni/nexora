using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Quests;
using Awaken.Domain.Repositories;
using Awaken.Domain.Services.Training;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Quests.Queries.GetResolvedProgramDay;

/// US-238: resolve o dia do programa (rotação cíclica) para o programa ativo
/// do usuário atual, combinando split map (US-237) + último Quest concluído
/// no mesmo programa (US-062) + <see cref="DailyProgramDayResolver"/>.
public class GetResolvedProgramDayQueryHandler(
    ICurrentUserService currentUserService,
    IUserWorkoutPreferenceRepository userWorkoutPreferenceRepository,
    ITrainingProgramSplitRepository trainingProgramSplitRepository,
    IQuestRepository questRepository,
    ILogger<GetResolvedProgramDayQueryHandler> logger)
    : IRequestHandler<GetResolvedProgramDayQuery, ResolvedProgramDayResponse>
{
    public async Task<ResolvedProgramDayResponse> Handle(
        GetResolvedProgramDayQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var preference = await userWorkoutPreferenceRepository.GetByUserIdAsync(userId, cancellationToken);
        var programKey = preference?.PreferredProgramId
            ?? throw new ConflictException(
                "NO_PROGRAM_SELECTED", "Usuário não tem um programa de treino selecionado.");

        var split = await trainingProgramSplitRepository.GetByProgramKeyAsync(programKey, cancellationToken)
            ?? throw new NotFoundException("TrainingProgramSplit", programKey);

        var days = split.Days
            .OrderBy(d => d.DayIndex)
            .Select(d => d.DayKey)
            .ToList();

        // RN-004: o último concluído só conta se for do MESMO programa (garantido pelo filtro do repositório).
        var lastCompleted = await questRepository.GetLastCompletedByUserAndProgramAsync(
            userId, programKey, cancellationToken);

        var resolution = DailyProgramDayResolver.Resolve(days, lastCompleted?.ResolvedDayKey);

        // R4 (analytics): dia do programa resolvido pela rotação, para auditoria/uso de produto.
        logger.LogInformation(
            "program_day_resolved userId={UserId} programKey={ProgramKey} resolvedDayKey={ResolvedDayKey} reason={Reason}",
            userId, programKey, resolution.DayKey, resolution.Reason);

        // RN-004: troca de programa reseta a rotacao para o primeiro dia do novo split.
        if (resolution.Reason == "program_changed")
        {
            logger.LogInformation(
                "program_rotation_reset userId={UserId} programKey={ProgramKey}",
                userId, programKey);
        }

        return new ResolvedProgramDayResponse(
            ProgramKey: programKey,
            LastCompletedDayKey: lastCompleted?.ResolvedDayKey,
            ResolvedDayKey: resolution.DayKey,
            ResolvedDayIndex: resolution.DayIndex,
            SplitMapVersion: split.SplitMapVersion,
            Reason: resolution.Reason);
    }
}
