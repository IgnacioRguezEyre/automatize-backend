namespace Integraciones.Infrastructure.Empleos;

/// <summary>Dónde está el microservicio Empleos y con qué clave se llama (sección <c>Empleos</c>).</summary>
public sealed class EmpleosOptions
{
    public const string SectionName = "Empleos";

    /// <summary>URL base, por ejemplo <c>http://localhost:5216</c> en local.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Clave de la cabecera X-Api-Key, si Empleos la exige. Vacía en local.</summary>
    public string? ApiKey { get; set; }
}
