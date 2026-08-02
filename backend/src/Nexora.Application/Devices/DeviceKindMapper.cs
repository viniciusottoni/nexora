using Nexora.Domain.Common;
using Nexora.Domain.Platform;

namespace Nexora.Application.Devices;

/// <summary>
/// Porta de <c>toPrismaType</c>/<c>fromPrismaType</c>
/// (<c>apps/api-edge/src/modules/devices/prisma-device-transactions.ts</c>). Os códigos de
/// contrato (CASHIER/KDS/WAITER/SUPPORT_TABLET) espelham <c>deviceKindSchema</c>
/// (<c>packages/contracts/src/devices.ts</c>) — o <see cref="DeviceType"/> do domínio C# tem
/// dois valores a mais (PrinterHost, Other) que não existiam no enum DeviceKind do TypeScript;
/// ambos degradam para "SUPPORT_TABLET" na saída, a mesma degradação que <c>fromPrismaType</c>
/// já fazia lá.
/// </summary>
internal static class DeviceKindMapper
{
    public static readonly IReadOnlyCollection<string> AllowedKinds =
        new[] { "CASHIER", "KDS", "WAITER", "SUPPORT_TABLET" };

    public static DeviceType ToDeviceType(string kind) => kind switch
    {
        "CASHIER" => DeviceType.Pos,
        "KDS" => DeviceType.Kds,
        "WAITER" => DeviceType.Waiter,
        "SUPPORT_TABLET" => DeviceType.Tablet,
        _ => throw new DomainException($"Tipo de dispositivo inválido: {kind}"),
    };

    public static string ToKindCode(DeviceType type) => type switch
    {
        DeviceType.Pos => "CASHIER",
        DeviceType.Kds => "KDS",
        DeviceType.Waiter => "WAITER",
        DeviceType.Tablet => "SUPPORT_TABLET",
        DeviceType.PrinterHost => "SUPPORT_TABLET",
        DeviceType.Other => "SUPPORT_TABLET",
        _ => "SUPPORT_TABLET",
    };
}
