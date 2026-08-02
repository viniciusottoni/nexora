using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Inventory;

/// Escopo minimo de inventario/loja necessario para o fluxo de regeneracao de
/// quest com o item "Pergaminho da Reforja" (US-048). Catalogo completo de
/// itens, moeda e integracao real de compra ficam fora desta US (ver ADR-022).
public class InventoryItem : BaseEntity
{
    public Guid UserId { get; private set; }
    public string ItemKey { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    /// US-227 / RN-006: token de concorrencia otimista, mapeado para a coluna
    /// de sistema "xmin" do PostgreSQL (mesmo padrao de GoldWallet.Version),
    /// evitando "lost update" em incrementos simultaneos do mesmo item.
    public uint Version { get; private set; }

    private InventoryItem() { }

    public static InventoryItem Create(Guid userId, string itemKey, int quantity = 0)
    {
        return new InventoryItem
        {
            UserId = userId,
            ItemKey = itemKey,
            Quantity = quantity,
        };
    }

    /// Incrementa (ou decrementa, se amount for negativo) a quantidade do
    /// item. RN-005: a quantidade nunca pode ficar negativa - lanca antes de
    /// alterar o estado quando o resultado seria negativo.
    public void Add(int amount, DateTime utcNow)
    {
        var newQuantity = Quantity + amount;
        if (newQuantity < 0)
            throw new InvalidOperationException("Quantidade de item nao pode ficar negativa.");

        Quantity = newQuantity;
        UpdatedAtUtc = utcNow;
    }

    public void ConsumeOne(DateTime utcNow)
    {
        if (Quantity <= 0)
            throw new InvalidOperationException("Item indisponivel para consumo.");

        Quantity--;
        UpdatedAtUtc = utcNow;
    }
}
