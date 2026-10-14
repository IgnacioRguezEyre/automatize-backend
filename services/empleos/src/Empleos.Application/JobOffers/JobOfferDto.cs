using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

public sealed record JobOfferDto(
    Guid Id,
    string Title,
    string Company,
    string Portal,
    DateOnly ArrivalDate,
    string Status,
    DateOnly? StatusDate,
    bool IsStale)
{
    public static JobOfferDto From(JobOffer offer, DateOnly today) => new(
        offer.Id, offer.Title, offer.Company, offer.Portal, offer.ArrivalDate,
        offer.Status.ToString(), offer.StatusDate, offer.IsStale(today));
}

public sealed record CreateJobOfferRequest(string Title, string? Company, string Portal, DateOnly? ArrivalDate);
