using Awaken.Contracts.Nutrition;
using MediatR;

namespace Awaken.Application.Nutrition.Commands.UpdateCupVolume;

public record UpdateCupVolumeCommand(int CupVolumeMl) : IRequest<UpdateCupVolumeResponse>;
