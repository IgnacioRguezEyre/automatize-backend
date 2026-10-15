using System.Net.Mail;
using Empleos.Domain.Common;

namespace Empleos.Domain.Sources;

/// <summary>
/// Un portal del que llegan ofertas por correo (LinkedIn, InfoJobs…), con las direcciones que lo envían.
/// La lectura del correo solo procesa las fuentes activas.
/// </summary>
public sealed class Source
{
    public const int MaxNameLength = 100;
    public const int MaxRuleLength = 500;
    public const int MaxSenders = 20;

    public Guid Id { get; }
    public string Name { get; }

    /// <summary>Direcciones de correo en minúsculas. Se sustituye la lista entera en cada cambio.</summary>
    public IReadOnlyList<string> Senders { get; private set; } = [];

    public bool IsActive { get; private set; }

    /// <summary>Instrucción de filtrado en texto libre, por ejemplo "solo la alerta de backend en Pontevedra".</summary>
    public string Rule { get; private set; }

    private Source(Guid id, string name, string rule, bool isActive)
    {
        Id = id;
        Name = name;
        Rule = rule;
        IsActive = isActive;
    }

    public static Source Create(string name, IEnumerable<string>? senders, string? rule)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        var cleanName = name.Trim();
        if (cleanName.Length > MaxNameLength)
            throw new ArgumentException($"El nombre admite como máximo {MaxNameLength} caracteres.", nameof(name));

        var source = new Source(Guid.NewGuid(), cleanName, CleanRule(rule), isActive: true);
        foreach (var sender in senders ?? Enumerable.Empty<string>()) source.AddSender(sender);
        return source;
    }

    /// <summary>Añade un remitente. Devuelve false si ya estaba.</summary>
    public bool AddSender(string email)
    {
        var clean = NormalizeEmail(email);
        if (Senders.Contains(clean)) return false;
        if (Senders.Count >= MaxSenders)
            throw new DomainException($"Una fuente admite como máximo {MaxSenders} remitentes.");
        Senders = [.. Senders, clean];
        return true;
    }

    /// <summary>Quita un remitente. Devuelve false si no estaba.</summary>
    public bool RemoveSender(string email)
    {
        var clean = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!Senders.Contains(clean)) return false;
        Senders = Senders.Where(s => s != clean).ToArray();
        return true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void UpdateRule(string? rule) => Rule = CleanRule(rule);

    private static string CleanRule(string? rule)
    {
        var clean = (rule ?? string.Empty).Trim();
        if (clean.Length > MaxRuleLength)
            throw new ArgumentException($"La regla admite como máximo {MaxRuleLength} caracteres.", nameof(rule));
        return clean;
    }

    private static string NormalizeEmail(string email)
    {
        var clean = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(clean, out var parsed) || parsed.Address != clean)
            throw new ArgumentException($"'{email}' no es una dirección de correo válida.", nameof(email));
        return clean;
    }
}
