namespace Nexora.Domain.Inventory;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Natureza de um movimento de estoque — a origem que explica a variação de saldo.</summary>
public enum StockMovementType
{
    Purchase,
    Production,
    Waste,
    Adjustment,
    Transfer,
    Return,
    Count
}

/// <summary>Motivo de uma perda de insumo, obrigatório quando o movimento é do tipo <see cref="StockMovementType.Waste"/>.</summary>
public enum WasteReason
{
    Breakage,
    Expiration,
    ProductionError,
    Courtesy,
    Theft,
    Other
}
