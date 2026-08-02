using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Commands.DeactivateVariant;

/// <summary>
/// Porta de <c>POST /v1/catalog/variants/{id}/deactivate</c> — nunca exclui fisicamente a
/// variante (US-011 §3.1/§12, cenário "Exclusão com histórico"). Como o módulo de pedidos ainda
/// não existe neste código-base, não há hoje como uma variante ter "histórico de pedidos" — por
/// isso não existe nenhum endpoint de exclusão física: desativar é a única operação de remoção
/// disponível, sempre (não só quando há histórico).
/// </summary>
public sealed record DeactivateVariantCommand(Guid VariantId) : ICommand<VariantResponse>;
