using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Security;

public class SecurityAlert : BaseEntity
{
    public string AlertType { get; private set; } = string.Empty; // "brute_force"|"invalid_token"|"rbac_denied"|"rate_limit_hit"|"suspicious_traffic"
    public string Severity { get; private set; } = string.Empty; // "low"|"medium"|"high"|"critical"
    public string Status { get; private set; } = "open"; // "open"|"analyzed"
    public string? Origin { get; private set; }
    public string? MaskedIp { get; private set; }
    public Guid? AffectedUserId { get; private set; }
    public string Environment { get; private set; } = string.Empty; // "dev"|"staging"|"prod"
    public Guid? AnalyzedByAdminId { get; private set; }
    public DateTime? AnalyzedAtUtc { get; private set; }

    /// <summary>
    /// US-219 RN-005: classificação de triagem além de "analisado".
    /// null (sem classificação) | "false_positive" | "confirmed".
    /// Classificar como falso positivo NUNCA remove/apaga o alerta — apenas altera este campo.
    /// </summary>
    public string? Classification { get; private set; }

    /// <summary>US-219: nota de triagem livre, sempre truncada/sanitizada pelo handler antes de persistir.</summary>
    public string? Note { get; private set; }

    /// <summary>US-219: vínculo opcional a um bug/incidente operacional (Awaken.Domain.Entities.Bugs.OperationalBug).</summary>
    public Guid? RelatedBugId { get; private set; }

    private SecurityAlert() { }

    public static SecurityAlert Create(string alertType, string severity, string environment, DateTime utcNow, string? origin = null, string? maskedIp = null, Guid? affectedUserId = null) =>
        new() { AlertType = alertType, Severity = severity, Environment = environment, CreatedAtUtc = utcNow, Origin = origin, MaskedIp = maskedIp, AffectedUserId = affectedUserId };

    public void MarkAnalyzed(Guid adminId, DateTime utcNow) { AnalyzedByAdminId = adminId; AnalyzedAtUtc = utcNow; Status = "analyzed"; UpdatedAtUtc = utcNow; }

    /// <summary>
    /// US-219 RN-005: classifica o alerta (ex.: "false_positive" ou "confirmed").
    /// Não altera Status nem remove o registro — o alerta permanece visível e auditável.
    /// </summary>
    public void Classify(string classification, Guid adminId, DateTime utcNow)
    {
        Classification = classification;
        AnalyzedByAdminId = adminId;
        AnalyzedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>US-219: adiciona/substitui a nota de triagem do alerta.</summary>
    public void SetNote(string note, Guid adminId, DateTime utcNow)
    {
        Note = note;
        UpdatedByUserId = adminId;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>US-219: vincula o alerta a um bug/incidente operacional já existente.</summary>
    public void LinkToBug(Guid bugId, Guid adminId, DateTime utcNow)
    {
        RelatedBugId = bugId;
        UpdatedByUserId = adminId;
        UpdatedAtUtc = utcNow;
    }
}
