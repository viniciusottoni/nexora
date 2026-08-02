using Awaken.Contracts.Admin.Dashboard;
using MediatR;

namespace Awaken.Application.Admin.Dashboard.Queries.GetAdminDashboard;

/// <summary>
/// US-161 — dashboard operacional administrativo.
/// RN-004: período padrão prioriza visão recente (últimos 7 dias) quando não informado.
/// </summary>
public record GetAdminDashboardQuery(DateTime? From, DateTime? To) : IRequest<AdminDashboardResponse>;
