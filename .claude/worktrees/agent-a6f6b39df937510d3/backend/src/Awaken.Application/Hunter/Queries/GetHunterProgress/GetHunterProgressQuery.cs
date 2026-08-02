using Awaken.Contracts.Progression;
using MediatR;

namespace Awaken.Application.Hunter.Queries.GetHunterProgress;

public record GetHunterProgressQuery : IRequest<ProgressionResponse>;
