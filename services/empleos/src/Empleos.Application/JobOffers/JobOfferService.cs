using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>Casos de uso de las ofertas. La fecha de hoy sale de TimeProvider para poder fijarla en los tests.</summary>
public sealed class JobOfferService(IJobOfferRepository repository, TimeProvider clock)
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public async Task<IReadOnlyList<JobOfferDto>> ListAsync(JobOfferStatus? status, CancellationToken ct)
    {
        var offers = await repository.ListAsync(status, ct);
        return offers.Select(o => JobOfferDto.From(o, Today)).ToList();
    }

    public async Task<JobOfferDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var offer = await repository.GetByIdAsync(id, ct);
        return offer is null ? null : JobOfferDto.From(offer, Today);
    }

    public async Task<JobOfferDto> CreateAsync(CreateJobOfferRequest request, CancellationToken ct)
    {
        var offer = JobOffer.Create(request.Title, request.Company, request.Portal, request.ArrivalDate ?? Today);
        repository.Add(offer);
        await repository.SaveChangesAsync(ct);
        return JobOfferDto.From(offer, Today);
    }

    /// <summary>Aplica un cambio de estado. Devuelve null si la oferta no existe.</summary>
    public async Task<JobOfferDto?> ChangeAsync(Guid id, Action<JobOffer, DateOnly> change, CancellationToken ct)
    {
        var offer = await repository.GetByIdAsync(id, ct);
        if (offer is null) return null;
        change(offer, Today);
        await repository.SaveChangesAsync(ct);
        return JobOfferDto.From(offer, Today);
    }
}
