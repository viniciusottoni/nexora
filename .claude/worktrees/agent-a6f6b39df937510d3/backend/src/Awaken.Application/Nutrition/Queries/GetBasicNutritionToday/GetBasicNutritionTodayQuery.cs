using Awaken.Contracts.Nutrition;
using MediatR;

namespace Awaken.Application.Nutrition.Queries.GetBasicNutritionToday;

public record GetBasicNutritionTodayQuery : IRequest<BasicNutritionTodayResponse>;
