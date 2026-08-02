using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Economy;
using Awaken.Domain.Repositories;
using Awaken.Shared.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

public class GoldWalletService(
    IGoldWalletRepository walletRepository,
    IGoldLedgerEntryRepository ledgerRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IHttpContextAccessor httpContextAccessor,
    IAuditLogService auditLogService,
    ILogger<GoldWalletService> logger) : IGoldWalletService
{
    /// US-227 / RN-004/RN-005: numero maximo de tentativas para um
    /// debito/credito em caso de conflito de concorrencia otimista (xmin)
    /// antes de desistir e sinalizar erro controlado ao chamador.
    private const int MaxConcurrencyRetries = 3;

    public async Task<GoldWallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet is not null)
            return wallet;

        wallet = GoldWallet.CreateEmpty(userId, dateTimeService.UtcNow);
        await walletRepository.AddAsync(wallet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return wallet;
    }

    public async Task<GoldLedgerEntry> CreditAsync(
        Guid userId,
        long amount,
        string reason,
        string? referenceType = null,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);

        var entry = await PersistWithConcurrencyRetryAsync(
            wallet,
            () => wallet.Credit(amount, reason, referenceType, referenceId, CorrelationId, dateTimeService.UtcNow),
            cancellationToken);

        // US-190 / CA-003: auditoria de crédito de Gold.
        // RN-002: sem valor de saldo ou quantidade nos metadados (ADR-015).
        await TryAuditAsync(
            AuditActions.GoldWalletCredited,
            userId,
            AuditActorType.System,
            AuditResourceTypes.GoldWallet,
            wallet.Id,
            AuditMetadata.Safe(new { reason, referenceType = referenceType ?? string.Empty }),
            cancellationToken);

        return entry;
    }

    public async Task<GoldLedgerEntry> DebitAsync(
        Guid userId,
        long amount,
        string reason,
        string? referenceType = null,
        string? referenceId = null,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);

        // RN-003/CA-002: GoldWallet.Debit lanca antes de alterar o saldo
        // quando o saldo for insuficiente, garantindo que nada seja gravado.
        var entry = await PersistWithConcurrencyRetryAsync(
            wallet,
            () => wallet.Debit(amount, reason, referenceType, referenceId, CorrelationId, dateTimeService.UtcNow),
            cancellationToken);

        // US-190 / CA-003: auditoria de débito de Gold.
        // RN-002: sem valor de saldo ou quantidade nos metadados (ADR-015).
        await TryAuditAsync(
            AuditActions.GoldWalletDebited,
            userId,
            AuditActorType.User,
            AuditResourceTypes.GoldWallet,
            wallet.Id,
            AuditMetadata.Safe(new { reason, referenceType = referenceType ?? string.Empty }),
            cancellationToken);

        return entry;
    }

    /// <summary>
    /// US-227 / RN-004/RN-005: aplica a mutação de saldo (credito ou debito)
    /// e persiste wallet + ledger, com retry controlado em caso de conflito
    /// de concorrência otimista (xmin). Antes de cada nova tentativa, recarrega
    /// o estado real da wallet do banco (RN-004) para que a mutação seja
    /// recalculada sobre o saldo atual. Esgotadas as tentativas, lança
    /// ConcurrencyConflictException (RN-005) em vez de propagar o erro de
    /// infraestrutura cru.
    /// </summary>
    private async Task<GoldLedgerEntry> PersistWithConcurrencyRetryAsync(
        GoldWallet wallet,
        Func<GoldLedgerEntry> mutate,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var entry = mutate();
            walletRepository.Update(wallet);
            await ledgerRepository.AddAsync(entry, cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return entry;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt == MaxConcurrencyRetries)
                {
                    logger.LogWarning(ex,
                        "Conflito de concorrencia esgotou tentativas para a carteira {WalletId}", wallet.Id);
                    throw new ConcurrencyConflictException();
                }

                logger.LogWarning(
                    "Conflito de concorrencia na carteira {WalletId}, tentativa {Attempt}/{MaxAttempts} - recarregando estado",
                    wallet.Id, attempt, MaxConcurrencyRetries);

                // O GoldLedgerEntry criado nesta tentativa falhou junto com o
                // SaveChanges e permaneceria rastreado como "Added" — sem
                // limpar o tracker antes de recarregar, a próxima tentativa
                // adicionaria uma SEGUNDA entrada de ledger (duplicada, com
                // BalanceAfter desatualizado) além da bem-sucedida.
                unitOfWork.ClearChangeTracker();
                await walletRepository.ReloadAsync(wallet, cancellationToken);
            }
        }

        // Inalcancavel: o laco sempre retorna ou lanca na ultima tentativa.
        throw new ConcurrencyConflictException();
    }

    private string? CorrelationId => httpContextAccessor.HttpContext?.Items["CorrelationId"] as string;

    /// <summary>
    /// US-190 / RN-005: falha ao auditar deve ser observável (warning), mas não
    /// pode cancelar a transação principal.
    /// </summary>
    private async Task TryAuditAsync(
        string action,
        Guid actorUserId,
        AuditActorType actorType,
        string resourceType,
        Guid resourceId,
        string? metadataSafe,
        CancellationToken cancellationToken)
    {
        try
        {
            await auditLogService.RecordAsync(
                action, actorUserId, actorType, resourceType, resourceId, metadataSafe, cancellationToken);
        }
        catch (Exception ex)
        {
            // RN-005: observável, mas não bloqueia a transação.
            logger.LogWarning(ex,
                "Falha ao registrar auditoria: action={Action} resourceType={ResourceType} resourceId={ResourceId}",
                action, resourceType, resourceId);
        }
    }
}
