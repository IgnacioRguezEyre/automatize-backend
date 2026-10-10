namespace Empleos.Domain.JobOffers;

/// <summary>
/// Una oferta de empleo llegada desde un portal. El estado solo cambia a través de sus métodos,
/// y la fecha entra siempre como parámetro: la clase nunca consulta el reloj.
/// </summary>
public sealed class JobOffer
{
    public const int DaysUntilStale = 14;

    public Guid Id { get; }
    public string Title { get; }
    public string Company { get; }
    public string Portal { get; }
    public DateOnly ArrivalDate { get; }
    public JobOfferStatus Status { get; private set; }

    /// <summary>Día del último cambio de estado; null mientras sigue como recién llegada.</summary>
    public DateOnly? StatusDate { get; private set; }

    private JobOffer(Guid id, string title, string company, string portal, DateOnly arrivalDate)
    {
        Id = id;
        Title = title;
        Company = company;
        Portal = portal;
        ArrivalDate = arrivalDate;
        Status = JobOfferStatus.New;
    }

    public static JobOffer Create(string title, string? company, string portal, DateOnly arrivalDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("El título es obligatorio.", nameof(title));
        if (string.IsNullOrWhiteSpace(portal))
            throw new ArgumentException("El portal es obligatorio.", nameof(portal));

        return new JobOffer(Guid.NewGuid(), title.Trim(), (company ?? string.Empty).Trim(), portal.Trim(), arrivalDate);
    }

    /// <summary>Abrir la oferta la marca como vista. Si ya pasó de nueva, no hace nada.</summary>
    public void MarkViewed(DateOnly today)
    {
        if (Status != JobOfferStatus.New) return;
        ChangeStatus(JobOfferStatus.Viewed, today);
    }

    /// <summary>
    /// Válido desde nueva o vista. Si ya estaba aplicada conserva la fecha original.
    /// Una oferta descartada hay que recuperarla antes.
    /// </summary>
    public void MarkApplied(DateOnly date)
    {
        if (Status == JobOfferStatus.Applied) return;
        if (Status == JobOfferStatus.Discarded)
            throw new InvalidOperationException("Una oferta descartada no se puede marcar como aplicada; recupérala antes.");
        ChangeStatus(JobOfferStatus.Applied, date);
    }

    /// <summary>Quita la marca de aplicada. Vuelve a vista, nunca a nueva.</summary>
    public void UnmarkApplied(DateOnly today)
    {
        if (Status != JobOfferStatus.Applied) return;
        ChangeStatus(JobOfferStatus.Viewed, today);
    }

    public void Discard(DateOnly today)
    {
        if (Status == JobOfferStatus.Discarded) return;
        ChangeStatus(JobOfferStatus.Discarded, today);
    }

    /// <summary>Recupera una oferta descartada. Vuelve a vista.</summary>
    public void Restore(DateOnly today)
    {
        if (Status != JobOfferStatus.Discarded) return;
        ChangeStatus(JobOfferStatus.Viewed, today);
    }

    /// <summary>Lleva 14 días o más desde que llegó y todavía no has aplicado ni la has descartado.</summary>
    public bool IsStale(DateOnly today)
    {
        var pending = Status is JobOfferStatus.New or JobOfferStatus.Viewed;
        return pending && today.DayNumber - ArrivalDate.DayNumber >= DaysUntilStale;
    }

    private void ChangeStatus(JobOfferStatus newStatus, DateOnly date)
    {
        Status = newStatus;
        StatusDate = date;
    }
}
