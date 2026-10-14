namespace Empleos.Domain.JobOffers;

/// <summary>
/// Una novedad de la candidatura: la empresa leyó el CV, respondió, citó a entrevista o rechazó.
/// Es un objeto de valor: no tiene identidad propia y solo existe dentro de su oferta.
/// </summary>
public sealed class ApplicationEvent
{
    public const int MaxTextLength = 280;

    public DateOnly Date { get; }
    public ApplicationEventType Type { get; }
    public string Text { get; }

    internal ApplicationEvent(DateOnly date, ApplicationEventType type, string text)
    {
        Date = date;
        Type = type;
        Text = text;
    }
}
