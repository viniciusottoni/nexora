namespace Nexora.Domain.Finance;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Natureza de um lançamento financeiro.</summary>
public enum FinancialEntryType
{
    Revenue,
    Expense
}

/// <summary>Grupo de despesa usado para classificar categorias no DRE (RF-FIN).</summary>
public enum ExpenseGroup
{
    Fixed,
    Variable,
    Payroll,
    Tax,
    Other
}
