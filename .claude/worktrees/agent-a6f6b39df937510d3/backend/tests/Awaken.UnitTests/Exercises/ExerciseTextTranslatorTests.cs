using Awaken.Domain.Services.Exercises;
using FluentAssertions;

namespace Awaken.UnitTests.Exercises;

/// <summary>
/// US-144 (R3.1) — <see cref="ExerciseTextTranslator"/> é um tradutor determinístico por dicionário,
/// deliberadamente pequeno/incompleto (ver comentário na classe). Estes testes cobrem o caso feliz
/// (frase conhecida totalmente traduzida), o caso de termo desconhecido (marca IsFullyTranslated=false
/// sem quebrar) e o caso vazio (não é erro).
/// </summary>
public class ExerciseTextTranslatorTests
{
    [Fact]
    public void TranslateReturnsFullyTranslatedResultForKnownPhrase()
    {
        var result = ExerciseTextTranslator.Translate("Barbell Bench Press");

        result.TranslatedText.Should().Be("Barra supino");
        result.IsFullyTranslated.Should().BeTrue();
    }

    [Fact]
    public void TranslateMarksResultAsNotFullyTranslatedWhenWordIsNotInDictionary()
    {
        var result = ExerciseTextTranslator.Translate("Zurricanguru Twist");

        result.IsFullyTranslated.Should().BeFalse();
    }

    [Fact]
    public void TranslateReturnsEmptyResultWithoutErrorForEmptyInput()
    {
        var result = ExerciseTextTranslator.Translate("");

        result.TranslatedText.Should().Be("");
        result.IsFullyTranslated.Should().BeTrue();
    }

    [Fact]
    public void TranslatePreservesUntranslatableWordsInsteadOfDroppingThem()
    {
        var result = ExerciseTextTranslator.Translate("Zurricanguru Twist");

        result.TranslatedText.Should().Be("Zurricanguru twist");
    }

    [Fact]
    public void TranslateMarksPunctuatedUnknownWordAsNotFullyTranslated()
    {
        var result = ExerciseTextTranslator.Translate("Barbell squat position.");

        result.IsFullyTranslated.Should().BeFalse();
    }
}
