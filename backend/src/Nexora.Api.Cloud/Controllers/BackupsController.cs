using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Backups.Commands.RecordBackupAlert;
using Nexora.Application.Backups.Commands.UploadBackup;
using Nexora.Contracts.Backups;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Recebe o backup periódico enviado por cada servidor edge (contingência de falha do servidor
/// local — pendência aberta no pacote de especificação sobre o destino definitivo). Rota
/// autenticada pelo esquema "Installation" (assinatura Ed25519 do edge — ver
/// <see cref="Infrastructure.Auth.InstallationAuthenticationHandler"/>), não por JWT de usuário.
/// O <c>installationId</c> autenticado vem da claim <c>installation_id</c> populada pelo handler
/// a partir do resultado de <c>AuthenticateInstallationRequestCommand</c> — nunca de um cabeçalho
/// lido manualmente aqui; a checagem de que ele bate com o <c>installationId</c> da rota
/// continua explícita no handler de aplicação (<c>UploadBackupCommandHandler</c>/
/// <c>RecordBackupAlertCommandHandler</c>). Porta de <c>backups.controller.ts</c>.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = "Installation")]
[Route("v1/platform/installations/{installationId:guid}")]
public sealed class BackupsController : ControllerBase
{
    private readonly ISender _sender;

    public BackupsController(ISender sender)
    {
        _sender = sender;
    }

    private Guid AuthenticatedInstallationId =>
        Guid.TryParse(User.FindFirst("installation_id")?.Value, out var value) ? value : Guid.Empty;

    /// <summary>Envia o dump de backup (six-hour ou daily) — corpo <c>application/octet-stream</c>.</summary>
    /// <remarks>
    /// Isento de Idempotency-Key (ADR-020, decisão registrada no relatório da tarefa de
    /// plumbing): já garante idempotência de CONTEÚDO pelo hash SHA-256 declarado em
    /// <c>X-Content-Sha256</c> e comparado ao computado (ver <c>UploadBackupCommandHandler</c>) —
    /// reenviar o mesmo dump é uma operação nula. O corpo pode ter até 1&#160;GB
    /// (<see cref="RequestSizeLimitAttribute"/> abaixo); bufferizá-lo de novo só para calcular uma
    /// chave genérica não paga o custo, e quem chama este endpoint é um worker agendado do edge,
    /// não um usuário clicando duas vezes.
    /// </remarks>
    [IdempotencyExempt]
    [HttpPut("backups")]
    [RequestSizeLimit(1_073_741_824)]
    [ProducesResponseType(typeof(UploadBackupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload(
        [FromRoute] Guid installationId,
        [FromHeader(Name = "X-Backup-Class")] string backupClass,
        [FromHeader(Name = "X-Content-Sha256")] string contentSha256,
        CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);

        var command = new UploadBackupCommand(
            installationId,
            AuthenticatedInstallationId,
            backupClass,
            contentSha256,
            buffer.ToArray());

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Registra que o edge falhou ao enviar o backup periódico.</summary>
    [HttpPost("backup-alerts")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Alert(
        [FromRoute] Guid installationId,
        [FromBody] RecordBackupAlertRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordBackupAlertCommand(installationId, AuthenticatedInstallationId, request.Reason, request.OccurredAt);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsSuccess)
            return Accepted();

        return result.ToActionResult(HttpContext);
    }
}
