using Awaken.Application.Admin.Security.Commands.ClassifyAlert;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Awaken.UnitTests.Admin.Security;

public class ClassifyAlertCommandValidatorTests
{
    private readonly ClassifyAlertCommandValidator _validator = new();

    [Theory]
    [InlineData("false_positive")]
    [InlineData("confirmed")]
    public void Validate_AllowedClassification_HasNoError(string classification)
    {
        var result = _validator.TestValidate(new ClassifyAlertCommand(Guid.NewGuid(), classification, null));
        result.ShouldNotHaveValidationErrorFor(x => x.Classification);
    }

    [Fact]
    public void Validate_UnknownClassification_HasError()
    {
        var result = _validator.TestValidate(new ClassifyAlertCommand(Guid.NewGuid(), "ignored_silently", null));
        result.ShouldHaveValidationErrorFor(x => x.Classification);
    }

    [Fact]
    public void Validate_NoteTooLong_HasError()
    {
        var longNote = new string('x', 501);
        var result = _validator.TestValidate(new ClassifyAlertCommand(Guid.NewGuid(), "false_positive", longNote));
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }
}
