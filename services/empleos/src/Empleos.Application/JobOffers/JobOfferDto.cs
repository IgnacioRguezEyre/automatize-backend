using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

public sealed record JobOfferDto(
    Guid Id,
    string Title,
    string Company,
    string Portal,
    DateOnly ArrivalDate,
    string? ExternalId,
    string? Location,
    string? WorkMode,
    string? Salary,
    string? Url,
    string? Description,
    JobOfferStatus Status,
    DateOnly? StatusDate,
    bool IsStale,
    bool IsClosed,
    string Notes,
    IReadOnlyList<ApplicationEventDto> Events)
{
    public static JobOfferDto From(JobOffer offer, DateOnly today) => new(
        offer.Id, offer.Title, offer.Company, offer.Portal, offer.ArrivalDate,
        offer.ExternalId, offer.Location, offer.WorkMode, offer.Salary, offer.Url, offer.Description,
        offer.Status, offer.StatusDate, offer.IsStale(today), offer.IsClosed, offer.Notes,
        offer.Events.OrderBy(e => e.Date).Select(e => new ApplicationEventDto(e.Date, e.Type, e.Text)).ToList());
}

public sealed record ApplicationEventDto(DateOnly Date, ApplicationEventType Type, string Text);

public sealed record CreateJobOfferRequest(
    string Title,
    string? Company,
    string Portal,
    DateOnly? ArrivalDate,
    string? Location = null,
    string? WorkMode = null,
    string? Salary = null,
    string? Url = null,
    string? Description = null);

public sealed record UpdateDetailsRequest(string? Location, string? WorkMode, string? Salary, string? Url, string? Description);

/// <summary>Si no se indica fecha, la novedad se anota con la de hoy.</summary>
public sealed record AddApplicationEventRequest(ApplicationEventType? Type, DateOnly? Date, string? Text);

public sealed record UpdateNotesRequest(string? Notes);
