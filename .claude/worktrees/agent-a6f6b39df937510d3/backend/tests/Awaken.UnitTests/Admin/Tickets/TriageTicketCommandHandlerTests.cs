using Awaken.Application.Admin.Tickets.Commands.TriageTicket;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Tickets;

public class TriageTicketCommandHandlerTests
{
    private readonly Mock<ISupportTicketRepository> _supportTicketRepository = new();
    private readonly Mock<ISupportTicketEventRepository> _supportTicketEventRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public TriageTicketCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private TriageTicketCommandHandler CreateHandler() => new(
        _supportTicketRepository.Object,
        _supportTicketEventRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SupportTicket CreateTicket() =>
        SupportTicket.Create(Guid.NewGuid(), "report", "pt-BR", "App travou", "1.0.0", null, UtcNow);

    [Fact]
    public async Task HandleStatusChangeCreatesEventAndAudit()
    {
        var ticket = CreateTicket();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var command = new TriageTicketCommand(ticket.Id, "in_triagem", null, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        ticket.Status.Should().Be("in_triagem");

        _supportTicketEventRepository.Verify(r => r.AddAsync(
            It.Is<SupportTicketEvent>(e =>
                e.TicketId == ticket.Id &&
                e.EventType == "status_change" &&
                e.OldValue == "open" &&
                e.NewValue == "in_triagem" &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminTicketStatusChanged,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SupportTicket,
            ticket.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePriorityChangeCreatesEvent()
    {
        var ticket = CreateTicket();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var command = new TriageTicketCommand(ticket.Id, null, "high", null);
        await CreateHandler().Handle(command, CancellationToken.None);

        ticket.Priority.Should().Be("high");

        _supportTicketEventRepository.Verify(r => r.AddAsync(
            It.Is<SupportTicketEvent>(e =>
                e.TicketId == ticket.Id &&
                e.EventType == "priority_change" &&
                e.OldValue == null &&
                e.NewValue == "high" &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminTicketTriaged,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SupportTicket,
            ticket.Id,
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAssigneeChangeCreatesEvent()
    {
        var ticket = CreateTicket();
        var newAdminId = Guid.NewGuid();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var command = new TriageTicketCommand(ticket.Id, null, null, newAdminId);
        await CreateHandler().Handle(command, CancellationToken.None);

        ticket.AssignedAdminId.Should().Be(newAdminId);

        _supportTicketEventRepository.Verify(r => r.AddAsync(
            It.Is<SupportTicketEvent>(e =>
                e.TicketId == ticket.Id &&
                e.EventType == "assigned" &&
                e.NewValue == newAdminId.ToString() &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsNotFoundWhenTicketMissing()
    {
        var ticketId = Guid.NewGuid();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicket?)null);

        var command = new TriageTicketCommand(ticketId, "open", null, null);
        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task HandleDoesNothingWhenStatusUnchanged()
    {
        var ticket = CreateTicket();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var command = new TriageTicketCommand(ticket.Id, "open", null, null);
        await CreateHandler().Handle(command, CancellationToken.None);

        _supportTicketEventRepository.Verify(r => r.AddAsync(
            It.IsAny<SupportTicketEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditLogService.Verify(a => a.RecordAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<AuditActorType>(),
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
