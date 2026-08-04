using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Alerts.Commands.DeliverPendingPush;

/// <summary>
/// US-081 §4 "Push com o sistema fechado", §9 "com a loja offline... após a reconexão" — entrega
/// push só para alertas de severidade alta/crítica ainda não empurrados (<c>Alert.PushedAt</c>),
/// tanto os nativos da nuvem (CASH_DIVERGENCE/SYNC_DELAY) quanto os do edge, assim que chegam à
/// nuvem pela sincronização (o alerta só existe nesta base depois de sincronizado — não há um canal
/// separado "avise a nuvem que um alerta local foi criado"). <c>TenantId</c> explícito, mesmo padrão
/// dos demais comandos de worker Cloud deste módulo.
/// </summary>
public sealed record DeliverPendingPushCommand(Guid TenantId) : ICommand<int>;
