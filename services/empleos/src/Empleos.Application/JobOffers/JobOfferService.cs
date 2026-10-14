using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>
/// Casos de uso de las ofertas: carga la oferta, le pide al dominio que haga el cambio y guarda.
/// La fecha de hoy sale de TimeProvider para poder fijarla en los tests.
/// Los métodos que reciben un id devuelven null si la oferta no existe.
/// </summary>
public sealed class JobOfferService(IJobOfferRepository repository, TimeProvider clock)
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public async Task<IReadOnlyList<JobOfferDto>> ListAsync(JobOfferStatus? status, CancellationToken ct)
    {
        var offers = await repository.ListAsync(status, ct);
        var today = Today;
        return offers.Select(o => JobOfferDto.From(o, today)).ToList();
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

    /// <summary>Cambios sin datos de entrada: vista, aplicada, descartar, recuperar, cerrar, reabrir.</summary>
    public Task<JobOfferDto?> ChangeAsync(Guid id, Action<JobOffer, DateOnly> change, CancellationToken ct) =>
        UpdateAsync(id, change, ct);

    public Task<JobOfferDto?> AddEventAsync(Guid id, AddApplicationEventRequest request, CancellationToken ct)
    {
        // Sin esta comprobación, un JSON sin "type" llegaría como CvRead, el primer valor del enum.
        var type = request.Type ?? throw new ArgumentException("Indica el tipo de novedad.", nameof(request));
        return UpdateAsync(id, (offer, today) => offer.AddEvent(request.Date ?? today, type, request.Text), ct);
    }

    public Task<JobOfferDto?> UpdateNotesAsync(Guid id, UpdateNotesRequest request, CancellationToken ct) =>
        UpdateAsync(id, (offer, _) => offer.UpdateNotes(request.Notes), ct);

    private async Task<JobOfferDto?> UpdateAsync(Guid id, Action<JobOffer, DateOnly> change, CancellationToken ct)
    {
        var offer = await repository.GetByIdAsync(id, ct);
        if (offer is null) return null;

        var today = Today;
        change(offer, today);
        await repository.SaveChangesAsync(ct);
        return JobOfferDto.From(offer, today);
    }
}
