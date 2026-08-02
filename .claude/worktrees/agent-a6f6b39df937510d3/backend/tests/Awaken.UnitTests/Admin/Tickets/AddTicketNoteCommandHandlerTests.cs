using Awaken.Application.Admin.Tickets.Commands.AddTicketNote;
using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Support;
using Awaken.Domain.Repositories;
using FluentAssertions;
using MediatR;
using Moq;

namespace Awaken.UnitTests.Admin.Tickets;

public class AddTicketNoteCommandHandlerTests
{
    private readonly Mock<ISupportTicketRepository> _supportTicketRepository = new();
    private readonly Mock<ISupportTicketEventRepository> _supportTicketEventRepository = new();
    private readonly Mock<ICurrentAdminService> _currentAdminService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAuditLogService> _auditLogService = new();

    private static readonly DateTime UtcNow = new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _adminId = Guid.NewGuid();

    public AddTicketNoteCommandHandlerTests()
    {
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
        _currentAdminService.Setup(s => s.AdminUserId).Returns(_adminId);
    }

    private AddTicketNoteCommandHandler CreateHandler() => new(
        _supportTicketRepository.Object,
        _supportTicketEventRepository.Object,
        _currentAdminService.Object,
        _dateTimeService.Object,
        _unitOfWork.Object,
        _auditLogService.Object);

    private static SupportTicket CreateTicket() =>
        SupportTicket.Create(Guid.NewGuid(), "report", "pt-BR", "App travou", "1.0.0", null, UtcNow);

    [Fact]
    public async Task HandleAddsNoteEventWithCorrectAdminId()
    {
        var ticket = CreateTicket();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var command = new AddTicketNoteCommand(ticket.Id, "Cliente confirmou reprodução do bug.");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);

        _supportTicketEventRepository.Verify(r => r.AddAsync(
            It.Is<SupportTicketEvent>(e =>
                e.TicketId == ticket.Id &&
                e.EventType == "internal_note" &&
                e.NoteContent == "Cliente confirmou reprodução do bug." &&
                e.AdminId == _adminId),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditLogService.Verify(a => a.RecordAsync(
            AuditActions.AdminTicketNoteAdded,
            _adminId,
            AuditActorType.Admin,
            AuditResourceTypes.SupportTicket,
            ticket.Id,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleThrowsNotFoundWhenTicketMissing()
    {
        var ticketId = Guid.NewGuid();
        _supportTicketRepository.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicket?)null);

        var command = new AddTicketNoteCommand(ticketId, "Nota qualquer.");
        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
