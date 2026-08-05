using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Onboarding.Commands.RecalculateOnboardingSteps;
using Nexora.Application.Tables.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Onboarding.Commands.ActivateTenant;

internal sealed class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ISender _sender;
    private readonly ICurrentTenantContext _tenantContext;

    public ActivateTenantCommandHandler(IApplicationDbContext db, ISender sender, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _sender = sender;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // Recalcula os passos derivados antes de checar — nested Send reaproveita a transação já
        // aberta por este comando (TransactionBehavior só abre uma nova quando não há uma corrente,
        // ver docstring da classe), então isto continua atômico com o restante do handler.
        await _sender.Send(new RecalculateOnboardingStepsCommand(request.TenantId), cancellationToken);

        var steps = await _db.OnboardingSteps
            .Where(s => s.TenantId == request.TenantId)
            .ToListAsync(cancellationToken);

        var pending = steps
            .Where(s => s.Key != OnboardingStepKey.Activation && s.Status != OnboardingStepStatus.Done)
            .OrderBy(s => s.Key)
            .Select(s => OnboardingStepKeyWireFormat.ToWireKey(s.Key))
            .ToList();

        if (pending.Count > 0)
        {
            return Result.Failure(
                "Ainda há passos pendentes no roteiro de implantação.",
                ApiErrorCodes.OnboardingIncomplete,
                BuildPendingMetaErrors(pending));
        }

        var now = DateTimeOffset.UtcNow;
        tenant.CompleteOnboarding(now);

        var activationStep = steps.SingleOrDefault(s => s.Key == OnboardingStepKey.Activation);
        activationStep?.Complete(now, _tenantContext.UserId);

        return Result.Success();
    }

    /// <summary>
    /// Reaproveita a MESMA convenção de "chave reservada em <c>Result.Errors</c>" que
    /// <c>PendingItemsClosePolicy.BuildMetaErrors</c> (US-035) já usa para chegar em
    /// <c>ProblemDetails.Extensions["meta"]</c> — ver <c>ResultExtensions.ExtractPendingItemsMeta</c>
    /// (Nexora.Api.Cloud/Infrastructure). [DESVIO DOCUMENTADO] Esse método está hardcoded ao NOME de
    /// saída <c>meta.pendingItems</c> (não um mecanismo genérico configurável por chamador, apesar da
    /// docstring dele sugerir isso) — o contrato desta história (US-141 §7) pede <c>meta.pending</c>.
    /// Instrução explícita desta tarefa proibiu editar <c>ResultExtensions.cs</c>; reaproveitar a
    /// chave <c>PendingItemsClosePolicy.MetaErrorsKey</c> tal como está é a única forma de produzir
    /// QUALQUER <c>meta</c> estruturado no 422 sem tocar naquele arquivo. Resultado: a resposta real
    /// hoje é <c>meta.pendingItems: ["MENU", "TABLES", ...]</c>, não <c>meta.pending</c>. Ver o
    /// relatório desta tarefa para a adição de duas linhas necessária em
    /// <c>ResultExtensions.MapErrorCode</c> para corrigir tanto o status 422 quanto (opcionalmente)
    /// o nome do campo.
    /// </summary>
    private static IReadOnlyDictionary<string, string[]> BuildPendingMetaErrors(IReadOnlyList<string> pendingStepKeys) =>
        new Dictionary<string, string[]> { [PendingItemsClosePolicy.MetaErrorsKey] = new[] { JsonSerializer.Serialize(pendingStepKeys) } };
}
