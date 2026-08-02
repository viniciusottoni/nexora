namespace Nexora.Domain.Catalog;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Canal de venda de um pedido ou de uma tabela de preço.</summary>
public enum Channel
{
    DineIn,
    Delivery,
    Takeout,
    Marketplace
}
