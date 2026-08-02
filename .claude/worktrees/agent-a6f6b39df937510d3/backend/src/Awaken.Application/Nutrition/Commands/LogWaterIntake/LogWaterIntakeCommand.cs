using Awaken.Contracts.Nutrition;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.LogWaterIntake;

public record LogWaterIntakeCommand(int AmountMl) : IRequest<LogWaterIntakeResponse>;
