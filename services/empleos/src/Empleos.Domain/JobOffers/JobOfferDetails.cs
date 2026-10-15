namespace Empleos.Domain.JobOffers;

/// <summary>
/// Datos descriptivos de una oferta que no intervienen en ninguna regla: dónde es, cómo se trabaja, cuánto pagan…
/// Todos son opcionales porque cada portal manda unos u otros.
/// </summary>
public sealed record JobOfferDetails(
    string? Location = null,
    string? WorkMode = null,
    string? Salary = null,
    string? Url = null,
    string? Description = null)
{
    public const int MaxLocationLength = 200;
    public const int MaxWorkModeLength = 30;
    public const int MaxSalaryLength = 100;
    public const int MaxUrlLength = 500;
    public const int MaxDescriptionLength = 1000;

    public static readonly JobOfferDetails Empty = new();

    /// <summary>Recorta los textos, convierte los vacíos en null y comprueba longitudes y la URL.</summary>
    internal JobOfferDetails Normalize()
    {
        var url = Clean(Url, MaxUrlLength, nameof(Url));
        if (url is not null && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
            throw new ArgumentException($"'{url}' no es una dirección web válida.", nameof(Url));

        return new JobOfferDetails(
            Clean(Location, MaxLocationLength, nameof(Location)),
            Clean(WorkMode, MaxWorkModeLength, nameof(WorkMode)),
            Clean(Salary, MaxSalaryLength, nameof(Salary)),
            url,
            Clean(Description, MaxDescriptionLength, nameof(Description)));
    }

    private static string? Clean(string? value, int max, string field)
    {
        var clean = value?.Trim();
        if (string.IsNullOrEmpty(clean)) return null;
        if (clean.Length > max)
            throw new ArgumentException($"{field} admite como máximo {max} caracteres.", field);
        return clean;
    }
}
