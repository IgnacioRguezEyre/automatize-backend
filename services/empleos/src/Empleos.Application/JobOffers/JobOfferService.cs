using Empleos.Application.Common;
using Empleos.Domain.Common;
using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>
/// Casos de uso de las ofertas: carga la oferta, le pide al dominio que haga el cambio y guarda.
/// La fecha de hoy sale de TimeProvider para poder fijarla en los tests.
/// Los métodos que reciben un id devuelven null si la oferta no existe.
/// </summary>
public sealed class JobOfferService(IJobOfferRepository repository, TimeProvider clock)
{
    public const int MaxImportSize = 1000;

    private DateOnly Today => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

    public async Task<PagedResult<JobOfferDto>> ListAsync(JobOfferQuery query, CancellationToken ct)
    {
        var valid = query.Validate();
        var (items, total) = await repository.ListAsync(valid, ct);
        var today = Today;
        return new PagedResult<JobOfferDto>(items.Select(o => JobOfferDto.From(o, today)).ToList(), valid.Page, valid.PageSize, total);
    }

    public async Task<JobOfferDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var offer = await repository.GetByIdAsync(id, ct);
        return offer is null ? null : JobOfferDto.From(offer, Today);
    }

    public async Task<JobOfferDto> CreateAsync(CreateJobOfferRequest request, CancellationToken ct)
    {
        var details = new JobOfferDetails(request.Location, request.WorkMode, request.Salary, request.Url, request.Description);
        var offer = JobOffer.Create(request.Title, request.Company, request.Portal, request.ArrivalDate ?? Today, details);
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

    public Task<JobOfferDto?> UpdateDetailsAsync(Guid id, UpdateDetailsRequest request, CancellationToken ct) =>
        UpdateAsync(id, (offer, _) => offer.UpdateDetails(
            new JobOfferDetails(request.Location, request.WorkMode, request.Salary, request.Url, request.Description)), ct);

    /// <summary>
    /// Importa ofertas con su historial. Es idempotente: las que ya existen (mismo ExternalId) se saltan, así que
    /// se puede repetir sin duplicar nada. Una oferta con datos no válidos se descarta y se informa, sin parar las demás.
    /// Todo se guarda de una vez al final.
    /// </summary>
    public async Task<ImportResult> ImportAsync(IReadOnlyList<ImportJobOfferItem> items, CancellationToken ct)
    {
        if (items.Count > MaxImportSize)
            throw new ArgumentException($"Se pueden importar como máximo {MaxImportSize} ofertas por llamada.", nameof(items));

        var ids = items.Select(i => i.ExternalId?.Trim() ?? string.Empty).Where(id => id.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existing = await repository.GetExistingExternalIdsAsync(ids, ct);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ImportError>();
        int created = 0, skipped = 0;

        foreach (var item in items)
        {
            var externalId = item.ExternalId?.Trim() ?? string.Empty;
            if (externalId.Length == 0)
            {
                errors.Add(new ImportError(string.Empty, $"'{item.Title}': falta el identificador externo."));
                continue;
            }
            if (existing.Contains(externalId) || !seen.Add(externalId))
            {
                skipped++;
                continue;
            }

            try
            {
                repository.Add(Rebuild(item, externalId));
                created++;
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                errors.Add(new ImportError(externalId, ex.Message));
            }
        }

        if (created > 0) await repository.SaveChangesAsync(ct);
        return new ImportResult(items.Count, created, skipped, errors);
    }

    /// <summary>
    /// Reconstruye la oferta pasando por los mismos métodos del dominio que usaría una persona, en el orden en que
    /// ocurrieron las cosas. Así se respetan las reglas también al importar: no hay atajos que dejen datos imposibles.
    /// </summary>
    private static JobOffer Rebuild(ImportJobOfferItem item, string externalId)
    {
        var details = new JobOfferDetails(item.Location, item.WorkMode, item.Salary, item.Url, item.Description);
        var offer = JobOffer.Create(item.Title, item.Company, item.Portal, item.ArrivalDate, details, externalId);
        var statusDate = item.StatusDate ?? item.ArrivalDate;

        switch (item.Status ?? JobOfferStatus.New)
        {
            case JobOfferStatus.Viewed: offer.MarkViewed(statusDate); break;
            case JobOfferStatus.Applied: offer.MarkApplied(statusDate); break;
            case JobOfferStatus.Discarded: offer.Discard(statusDate); break;
        }

        foreach (var e in (item.Events ?? Array.Empty<ImportApplicationEvent>()).OrderBy(e => e.Date))
            offer.AddEvent(e.Date, e.Type, e.Text);

        offer.UpdateNotes(item.Notes);
        if (item.IsClosed) offer.Close();
        return offer;
    }

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
