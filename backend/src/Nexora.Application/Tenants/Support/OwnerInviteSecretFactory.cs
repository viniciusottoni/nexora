using System.Security.Cryptography;

namespace Nexora.Application.Tenants.Support;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — mesma geração de segredo bruto de
/// <c>ProvisionTenantCommandHandler.CreateRawSecret</c> (US-002), extraída para cá porque agora dois
/// handlers precisam dela (o original ali é <c>private static</c>, não reaproveitável sem duplicar ou
/// tocar naquele arquivo — evitado de propósito, ver relatório da tarefa sobre isolamento entre
/// agentes). O valor NUNCA é persistido em lugar nenhum: só o hash (<c>ISecretDigester.Digest</c>)
/// vai para <see cref="Nexora.Domain.Platform.OwnerInvite.SecretHash"/>; o bruto só existe na memória
/// do request, até ser passado para <c>IEmailSender.EnqueueAsync</c>.
/// </summary>
public static class OwnerInviteSecretFactory
{
    public static string CreateRawSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
