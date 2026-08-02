using Nexora.Domain.Common;

namespace Nexora.Domain.Inventory;

/// <summary>
/// Unidade de medida de insumo (ex.: "KG", "UN", "L"). Diferente das demais entidades do
/// pacote, a chave primária é o próprio código — não é gerada UUIDv7, pois é uma tabela de
/// referência compartilhada entre tenants, não um agregado de negócio por tenant.
/// </summary>
public sealed class UnitOfMeasure
{
    private UnitOfMeasure() { }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? BaseCode { get; private set; }
    public decimal Factor { get; private set; } = 1m;

    public static UnitOfMeasure Create(string code, string name, string? baseCode = null, decimal factor = 1m)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código da unidade de medida é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da unidade de medida é obrigatório.");

        if (factor <= 0)
            throw new DomainException("O fator de conversão da unidade de medida deve ser maior que zero.");

        return new UnitOfMeasure
        {
            Code = code,
            Name = name,
            BaseCode = baseCode,
            Factor = factor
        };
    }
}
