using Awaken.Api.Controllers.V1;
using Awaken.Application.Users.Queries.GetEffectiveExperienceLevel;
using Awaken.Application.TrainingPrograms.Commands.SelectTrainingProgram;
using Awaken.Contracts.TrainingPrograms;
using Awaken.Contracts.Users;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Awaken.UnitTests.Api;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task GetEffectiveExperienceLevel_ReturnsOkWithMediatorResult()
    {
        var response = new EffectiveExperienceLevelResponse(
            "advanced",
            "does_not_train",
            "sedentary");

        _mediator.Setup(m => m.Send(
                It.IsAny<GetEffectiveExperienceLevelQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new UsersController(_mediator.Object);

        var result = await controller.GetEffectiveExperienceLevel(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(response);
        _mediator.Verify(m => m.Send(
            It.IsAny<GetEffectiveExperienceLevelQuery>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SelectTrainingProgram_ReturnsNoContentAndForwardsRequest()
    {
        _mediator.Setup(m => m.Send(
                It.IsAny<SelectTrainingProgramCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var controller = new UsersController(_mediator.Object);

        var result = await controller.SelectTrainingProgram(
            new SelectTrainingProgramRequest("full_body"),
            CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediator.Verify(m => m.Send(
            It.Is<SelectTrainingProgramCommand>(cmd => cmd.ProgramKey == "full_body"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
