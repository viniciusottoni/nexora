using Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Platform;

public sealed class AttentionCommandValidationTests
{
    [Fact]
    public void AcknowledgeAttentionItem_Rejeita_Motivo_Com_Apenas_Espacos()
    {
        var command = new AcknowledgeAttentionItemCommand("item-valido", "   ", Guid.NewGuid());

        var result = new AcknowledgeAttentionItemCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(command.Reason));
    }
}
