using Empleos.Domain.JobOffers;

namespace Empleos.Application.JobOffers;

/// <summary>Filtros y página del listado de ofertas. Valida los valores para no lanzar consultas absurdas a la base de datos.</summary>
public sealed record JobOfferQuery(JobOfferStatus? Status = null, string? Search = null, int Page = 1, int PageSize = DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public JobOfferQuery Validate()
    {
        if (Page < 1)
            throw new ArgumentException("La página empieza en 1.", nameof(Page));
        if (PageSize is < 1 or > MaxPageSize)
            throw new ArgumentException($"El tamaño de página debe estar entre 1 y {MaxPageSize}.", nameof(PageSize));
        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (search?.Length > 100)
            throw new ArgumentException("La búsqueda admite como máximo 100 caracteres.", nameof(Search));
        return this with { Search = search };
    }

    public int Skip => (Page - 1) * PageSize;
}
