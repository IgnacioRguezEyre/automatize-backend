namespace Integraciones.Application.Sync;

/// <summary>Configuración de la sincronización (sección <c>Sync</c> de appsettings).</summary>
public sealed class SyncOptions
{
    public const string SectionName = "Sync";

    /// <summary>
    /// Cuántos días hacia atrás se miran en cada pasada. No se guarda "por dónde se iba": como Empleos ignora
    /// los <c>externalId</c> repetidos, volver a leer un correo no duplica nada. Así el servicio no necesita base de datos.
    /// </summary>
    public int LookbackDays { get; set; } = 3;

    /// <summary>Tope de correos por pasada, para no pedir de golpe cientos al buzón.</summary>
    public int MaxMessages { get; set; } = 200;

    /// <summary>Cada cuántos minutos se sincroniza solo. 0 desactiva la sincronización automática.</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Zona horaria con la que se decide el día de llegada de cada correo.</summary>
    public string TimeZone { get; set; } = "Europe/Madrid";
}
