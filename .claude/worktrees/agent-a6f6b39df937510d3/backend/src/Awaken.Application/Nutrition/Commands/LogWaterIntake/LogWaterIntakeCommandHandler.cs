using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Nutrition;
using Awaken.Domain.Entities.Nutrition;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.LogWaterIntake;

public class LogWaterIntakeCommandHandler(
    ICurrentUserService currentUserService,
    INutritionLogRepository nutritionLogRepository,
    IUserDateService userDateService,
    IUnitOfWork unitOfWork) : IRequestHandler<LogWaterIntakeCommand, LogWaterIntakeResponse>
{
    public async Task<LogWaterIntakeResponse> Handle(
        LogWaterIntakeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var today = userDateService.TodayLocal;

        var log = await nutritionLogRepository.GetByUserIdAndDateAsync(userId, today, cancellationToken);

        // US-087 Fluxo 9.1: cria o log do dia se ainda não existe.
        if (log is null)
        {
            log = NutritionLog.Create(userId, today);
            await nutritionLogRepository.AddAsync(log, cancellationToken);
        }

        // US-087 RN-002: soma volume em ml. EF Core rastreia a alteração automaticamente.
        log.AddWater(request.AmountMl);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LogWaterIntakeResponse(WaterConsumedMl: log.WaterMl);
    }
}
