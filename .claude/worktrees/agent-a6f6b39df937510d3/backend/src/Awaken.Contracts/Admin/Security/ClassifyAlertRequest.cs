namespace Awaken.Contracts.Admin.Security;

/// <summary>
/// US-219 RN-005: classifica um alerta de segurança (ex.: "false_positive", "confirmed").
/// Classificar como falso positivo NÃO apaga o alerta — apenas muda sua classificação.
/// </summary>
public record ClassifyAlertRequest(string Classification, string? Note = null);
