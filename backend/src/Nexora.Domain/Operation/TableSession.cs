using Nexora.Domain.Common;

namespace Nexora.Domain.Operation;

/// <summary>
/// Comanda — sessão de consumo aberta em uma <see cref="DiningTable"/>, do momento em que o
/// garçom abre a mesa até o fechamento da conta (RN-020). <see cref="BusinessDay"/> segue a
/// virada configurável do tenant, não a meia-noite civil (ADR-018).
/// </summary>
public sealed class TableSession
{
    private readonly List<Order> _orders = new();

    private TableSession() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid TableId { get; private set; }
    public DateOnly BusinessDay { get; private set; }
    public TableSessionStatus Status { get; private set; } = TableSessionStatus.Open;
    public short GuestCount { get; private set; } = 1;

    /// <summary>
    /// Falso quando a sessão foi aberta pelo cliente via QR (US-021/US-022, cenário "Abertura pelo
    /// cliente via QR": "a contagem de pessoas deve ficar pendente de confirmação pelo garçom").
    /// <see cref="UpdateGuestCount"/> sempre marca como confirmado — é o garçom quem confirma,
    /// mesmo que o valor informado seja igual ao pendente.
    /// </summary>
    public bool GuestCountConfirmed { get; private set; } = true;

    public Guid? WaiterId { get; private set; }
    public Guid? OpenedBy { get; private set; }
    public string OpenedSource { get; private set; } = "WAITER";
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? BillRequestedAt { get; private set; }

    /// <summary>
    /// Preferência de divisão registrada na solicitação da conta (US-026 §8: <c>split_mode</c>) —
    /// um de <c>"BY_PERSON"</c>/<c>"BY_ITEM"</c>/<c>"SINGLE"</c>, validado na aplicação (Domain não
    /// depende de um enum do Postgres para isto porque a US-027, que ainda vai consumir este campo,
    /// não fechou o catálogo definitivo de modos — string simples evita uma migration de enum
    /// nativo duas vezes). Nulo enquanto a conta nunca foi solicitada nesta sessão; volta a nulo em
    /// <see cref="ReopenAfterNewItem"/> (a preferência é da SOLICITAÇÃO, não da sessão como um todo
    /// — um novo pedido invalida a divisão combinada até a conta ser pedida de novo).
    /// </summary>
    public string? SplitMode { get; private set; }

    /// <summary>Quantas pessoas dividem a conta (US-026 §8: <c>split_people</c>) — só relevante quando <see cref="SplitMode"/> é <c>"BY_PERSON"</c>.</summary>
    public short? SplitPeople { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal ServiceFeeAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public short? Rating { get; private set; }
    public string? RatingComment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public DiningTable Table { get; private set; } = null!;
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    /// <param name="occurredAt">
    /// Horário real da abertura (RN-020, US-022 §9: "o X-Occurred-At... preserva o horário real da
    /// abertura mesmo que o evento só sincronize horas depois"). Nulo usa o relógio do servidor no
    /// momento da chamada — o caso comum (abertura pelo QR, sem esse header).
    /// </param>
    /// <param name="guestCountConfirmed">
    /// Falso na abertura por QR (US-021): a contagem é um valor de espera até o garçom confirmar
    /// via <see cref="UpdateGuestCount"/>.
    /// </param>
    public static TableSession Create(
        Guid tenantId,
        Guid storeId,
        Guid tableId,
        DateOnly businessDay,
        short guestCount = 1,
        Guid? waiterId = null,
        Guid? openedBy = null,
        string openedSource = "WAITER",
        DateTimeOffset? occurredAt = null,
        bool guestCountConfirmed = true)
    {
        if (guestCount < 1)
            throw new DomainException("A comanda precisa ter pelo menos um cliente.");

        var now = DateTimeOffset.UtcNow;

        return new TableSession
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            TableId = tableId,
            BusinessDay = businessDay,
            Status = TableSessionStatus.Open,
            GuestCount = guestCount,
            GuestCountConfirmed = guestCountConfirmed,
            WaiterId = waiterId,
            OpenedBy = openedBy,
            OpenedSource = openedSource,
            OpenedAt = occurredAt ?? now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Altera a contagem de pessoas durante a sessão (US-022, cenário "Troca de garçom
    /// responsável" e RN da US-021 "contagem pendente de confirmação") — sempre confirma a
    /// contagem, mesmo vindo de uma sessão aberta por QR.
    /// </summary>
    public void UpdateGuestCount(short guestCount)
    {
        if (guestCount < 1)
            throw new DomainException("A comanda precisa ter pelo menos um cliente.");

        GuestCount = guestCount;
        GuestCountConfirmed = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Troca o garçom responsável pela mesa (US-022, cenário "Troca de garçom responsável"). O
    /// histórico de quem passou pela sessão (ex.: "João -&gt; Ana") não é modelado aqui — é
    /// responsabilidade da camada de aplicação registrar em <see cref="AuditLog"/>, porque
    /// <see cref="TableSession"/> guarda só o responsável atual, nunca uma lista de trocas.
    /// </summary>
    public void ReassignWaiter(Guid? waiterId)
    {
        WaiterId = waiterId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Transição Open→BillRequested (US-026 §4, cenários "Solicitação pelo cliente" e "Solicitação
    /// pelo garçom" — efeito idêntico nos dois). <paramref name="splitMode"/>/<paramref name="splitPeople"/>
    /// registram a preferência de divisão escolhida NA solicitação (US-026 §8) — a US-027 (divisão
    /// de conta de verdade) é quem lê estes dois campos depois; esta história só os persiste.
    /// </summary>
    public void RequestBill(string? splitMode = null, short? splitPeople = null)
    {
        if (Status is not TableSessionStatus.Open)
            throw new DomainException("Só é possível pedir a conta de uma comanda aberta.");

        Status = TableSessionStatus.BillRequested;
        BillRequestedAt = DateTimeOffset.UtcNow;
        SplitMode = splitMode;
        SplitPeople = splitPeople;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Volta BillRequested→Open (US-026 §4, cenário "Novo pedido após solicitar a conta": "Quando o
    /// cliente adicionar um novo item, Então a sessão deve voltar para OPEN"). Chamado por
    /// <c>AddOrderItemCommandHandler</c> sempre que um item é lançado numa sessão que já não está
    /// mais <see cref="TableSessionStatus.Open"/>. Zera <see cref="SplitMode"/>/<see cref="SplitPeople"/>
    /// e <see cref="BillRequestedAt"/>: a preferência de divisão pertence à SOLICITAÇÃO anterior, que
    /// foi invalidada por este novo pedido — uma nova solicitação de conta precisa escolher de novo
    /// (evita o caixa preparar a divisão errada com base numa comanda que mudou depois).
    /// Não faz nada (idempotente) se a sessão já não estiver em <see cref="TableSessionStatus.BillRequested"/>
    /// — só essa transição específica é revertida por um novo item; sessão fechada/paga não volta.
    /// </summary>
    public void ReopenAfterNewItem()
    {
        if (Status is not TableSessionStatus.BillRequested)
        {
            return;
        }

        Status = TableSessionStatus.Open;
        BillRequestedAt = null;
        SplitMode = null;
        SplitPeople = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Atualiza só a preferência de divisão de uma sessão que JÁ está em
    /// <see cref="TableSessionStatus.BillRequested"/> — ex.: o grupo corrigiu de 3 para 4 pessoas
    /// antes do caixa fechar a conta. Diferente de <see cref="RequestBill"/>: não é uma transição de
    /// estado (a sessão já estava em <c>BillRequested</c>), por isso o handler que chama isto NÃO
    /// reemite EVT-022 nem cria um segundo <c>Alert</c> — só a PRIMEIRA solicitação faz isso (mesma
    /// lógica de "chamada repetida não duplica" da US-025, aplicada aqui à solicitação de conta).
    /// </summary>
    public void UpdateSplitPreference(string? splitMode, short? splitPeople)
    {
        if (Status is not TableSessionStatus.BillRequested)
            throw new DomainException("Só é possível atualizar a divisão de uma conta já solicitada.");

        SplitMode = splitMode;
        SplitPeople = splitPeople;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsPaid(decimal subtotal, decimal discountAmount, decimal serviceFeeAmount, decimal totalAmount)
    {
        if (Status is not TableSessionStatus.BillRequested)
            throw new DomainException("Só é possível pagar uma comanda com conta solicitada.");

        Status = TableSessionStatus.Paid;
        Subtotal = subtotal;
        DiscountAmount = discountAmount;
        ServiceFeeAmount = serviceFeeAmount;
        TotalAmount = totalAmount;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Close()
    {
        if (Status is not TableSessionStatus.Paid)
            throw new DomainException("Só é possível fechar uma comanda paga.");

        Status = TableSessionStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Release()
    {
        ReleasedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Rate(short rating, string? comment)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("A avaliação precisa estar entre 1 e 5.");

        Rating = rating;
        RatingComment = comment;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
