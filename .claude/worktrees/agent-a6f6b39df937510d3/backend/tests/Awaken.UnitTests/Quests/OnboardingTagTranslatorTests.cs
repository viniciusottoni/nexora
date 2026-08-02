using Awaken.Domain.Services.Quests;
using FluentAssertions;

namespace Awaken.UnitTests.Quests;

public class OnboardingTagTranslatorTests
{
    [Fact]
    public void TranslateLimitations_MapsKneeProblemToKneeHighStress()
    {
        var result = OnboardingTagTranslator.TranslateLimitations(["knee_problem"]);

        result.Should().Contain("knee_high_stress");
    }

    [Fact]
    public void TranslatePains_MapsLowerBackAndKneesToCatalogTags()
    {
        var result = OnboardingTagTranslator.TranslatePains(["lower_back", "knees"]);

        result.Should().Contain("lumbar_high_stress");
        result.Should().Contain("knee_high_stress");
    }

    [Fact]
    public void TranslateLimitations_PassesUnknownValueThroughUnchanged()
    {
        var result = OnboardingTagTranslator.TranslateLimitations(["xyz"]);

        result.Should().ContainSingle().Which.Should().Be("xyz");
    }

    [Fact]
    public void TranslateLimitations_MedicalRestrictionIsConservativeAndExpandsToMultipleCatalogTags()
    {
        // medical_restriction e generico por natureza (nao ha como saber qual restricao real
        // o usuario tem) - por isso bloqueia as categorias de risco mais comuns em vez de nenhuma.
        var result = OnboardingTagTranslator.TranslateLimitations(["medical_restriction"]);

        result.Should().BeEquivalentTo(
        [
            "lumbar_high_stress", "shoulder_high_stress", "knee_high_stress",
            "high_impact", "high_technical_complexity"
        ]);
    }

    [Fact]
    public void TranslatePains_MixOfKnownAndUnknownValues_TranslatesKnownAndKeepsUnknown()
    {
        var result = OnboardingTagTranslator.TranslatePains(["neck", "some_future_tag"]);

        result.Should().Contain("cervical_high_stress");
        result.Should().Contain("some_future_tag");
    }
}
