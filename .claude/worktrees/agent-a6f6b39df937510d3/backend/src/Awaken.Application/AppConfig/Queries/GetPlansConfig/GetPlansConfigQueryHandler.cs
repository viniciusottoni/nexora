using Awaken.Contracts.AppConfig;
using MediatR;

namespace Awaken.Application.AppConfig.Queries.GetPlansConfig;

public class GetPlansConfigQueryHandler : IRequestHandler<GetPlansConfigQuery, PlansConfigResponse>
{
    public Task<PlansConfigResponse> Handle(GetPlansConfigQuery request, CancellationToken cancellationToken)
        => Task.FromResult(new PlansConfigResponse(
            TrialDays: 7,
            Plans: BuildPlans(request.Locale ?? "pt-BR")));

    private static IReadOnlyList<PlanConfigResponse> BuildPlans(string locale)
    {
        var isEnglish = locale.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var isSpanish = locale.StartsWith("es", StringComparison.OrdinalIgnoreCase);
        var isFrench = locale.StartsWith("fr", StringComparison.OrdinalIgnoreCase);

        var monthlyPrice = isEnglish ? "R$ 14.90" : "R$ 14,90";
        var annualPrice = isEnglish ? "R$ 99.90" : "R$ 99,90";
        var annualSaving = isEnglish
            ? "≈ R$ 8.32/month · save 45% vs. monthly"
            : isFrench
                ? "≈ R$ 8,32/mois · économisez 45% par rapport au mensuel"
                : "≈ R$ 8,32/mês · economize 45% vs. mensal";

        var monthlyPeriod = isEnglish ? "/month" : isSpanish ? "/mes" : isFrench ? "/mois" : "/mês";
        var annualPeriod = isEnglish ? "/year" : isSpanish ? "/año" : isFrench ? "/an" : "/ano";

        return
        [
            new PlanConfigResponse(
                Id: "monthly",
                Label: isEnglish ? "Monthly" : isSpanish ? "Mensual" : isFrench ? "Mensuel" : "Mensal",
                PriceLabel: monthlyPrice,
                PeriodLabel: monthlyPeriod,
                SavingLabel: null,
                Highlighted: false),
            new PlanConfigResponse(
                Id: "annual",
                Label: isEnglish ? "Annual" : isSpanish ? "Anual" : isFrench ? "Annuel" : "Anual",
                PriceLabel: annualPrice,
                PeriodLabel: annualPeriod,
                SavingLabel: annualSaving,
                Highlighted: true)
        ];
    }
}
