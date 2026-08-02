using Awaken.Contracts.AppConfig;
using MediatR;

namespace Awaken.Application.AppConfig.Queries.GetPlansConfig;

public record GetPlansConfigQuery(string? Platform, string? Locale) : IRequest<PlansConfigResponse>;
