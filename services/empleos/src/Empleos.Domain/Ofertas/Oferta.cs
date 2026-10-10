namespace Empleos.Domain.Ofertas;

/// <summary>
/// Una oferta de empleo llegada desde un portal. El estado solo cambia a través de sus métodos,
/// y la fecha entra siempre como parámetro: la clase nunca consulta el reloj.
/// </summary>
public sealed class Oferta
{
    public const int DiasParaSerAntigua = 14;

    public Guid Id { get; }
    public string Titulo { get; }
    public string Empresa { get; }
    public string Portal { get; }
    public DateOnly FechaLlegada { get; }
    public EstadoOferta Estado { get; private set; }

    /// <summary>Día del último cambio de estado; null mientras sigue como recién llegada.</summary>
    public DateOnly? FechaEstado { get; private set; }

    private Oferta(Guid id, string titulo, string empresa, string portal, DateOnly fechaLlegada)
    {
        Id = id;
        Titulo = titulo;
        Empresa = empresa;
        Portal = portal;
        FechaLlegada = fechaLlegada;
        Estado = EstadoOferta.Nueva;
    }

    public static Oferta Crear(string titulo, string? empresa, string portal, DateOnly fechaLlegada)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ArgumentException("El título es obligatorio.", nameof(titulo));
        if (string.IsNullOrWhiteSpace(portal))
            throw new ArgumentException("El portal es obligatorio.", nameof(portal));

        return new Oferta(Guid.NewGuid(), titulo.Trim(), (empresa ?? string.Empty).Trim(), portal.Trim(), fechaLlegada);
    }

    /// <summary>Abrir la oferta la marca como vista. Si ya pasó de Nueva, no hace nada.</summary>
    public void MarcarVista(DateOnly hoy)
    {
        if (Estado != EstadoOferta.Nueva) return;
        CambiarEstado(EstadoOferta.Vista, hoy);
    }

    /// <summary>
    /// Válido desde Nueva o Vista. Si ya estaba aplicada conserva la fecha original.
    /// Una oferta descartada hay que recuperarla antes.
    /// </summary>
    public void MarcarAplicada(DateOnly fecha)
    {
        if (Estado == EstadoOferta.Aplicada) return;
        if (Estado == EstadoOferta.Descartada)
            throw new InvalidOperationException("Una oferta descartada no se puede marcar como aplicada; recupérala antes.");
        CambiarEstado(EstadoOferta.Aplicada, fecha);
    }

    /// <summary>Quita la marca de aplicada. Vuelve a Vista, nunca a Nueva.</summary>
    public void DesmarcarAplicada(DateOnly hoy)
    {
        if (Estado != EstadoOferta.Aplicada) return;
        CambiarEstado(EstadoOferta.Vista, hoy);
    }

    public void Descartar(DateOnly hoy)
    {
        if (Estado == EstadoOferta.Descartada) return;
        CambiarEstado(EstadoOferta.Descartada, hoy);
    }

    /// <summary>Recupera una oferta descartada. Vuelve a Vista.</summary>
    public void Recuperar(DateOnly hoy)
    {
        if (Estado != EstadoOferta.Descartada) return;
        CambiarEstado(EstadoOferta.Vista, hoy);
    }

    /// <summary>Lleva 14 días o más desde que llegó y todavía no has aplicado ni la has descartado.</summary>
    public bool EsAntigua(DateOnly hoy)
    {
        var pendiente = Estado is EstadoOferta.Nueva or EstadoOferta.Vista;
        return pendiente && hoy.DayNumber - FechaLlegada.DayNumber >= DiasParaSerAntigua;
    }

    private void CambiarEstado(EstadoOferta nuevo, DateOnly fecha)
    {
        Estado = nuevo;
        FechaEstado = fecha;
    }
}
