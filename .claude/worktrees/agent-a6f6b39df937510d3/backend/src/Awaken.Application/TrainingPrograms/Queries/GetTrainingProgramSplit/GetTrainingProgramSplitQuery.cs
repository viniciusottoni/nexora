using Awaken.Contracts.TrainingPrograms;
using MediatR;

namespace Awaken.Application.TrainingPrograms.Queries.GetTrainingProgramSplit;

/// US-237: consulta o split map (dias + alvo muscular/padrão de movimento por
/// dia) de um programa. Lança NotFoundException para programas sem split
/// clássico (ex.: perfect_2, system — RN-009).
public record GetTrainingProgramSplitQuery(string ProgramKey) : IRequest<TrainingProgramSplitResponse>;
