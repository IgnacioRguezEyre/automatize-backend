using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Integraciones.Application.Parsing;

/// <summary>Utilidades de texto que comparten los analizadores.</summary>
public static partial class TextTools
{
    private const int MaxExternalIdLength = 150; // el mismo límite que tiene Empleos

    /// <summary>
    /// Quita espacios sobrantes, espacios duros (&amp;nbsp;) y caracteres invisibles que meten los correos
    /// para que el texto no se vea en la vista previa. Devuelve null si no queda nada.
    /// </summary>
    public static string? Clean(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var cleaned = InvisibleChars().Replace(text, string.Empty).Replace(' ', ' ');
        cleaned = Whitespace().Replace(cleaned, " ").Trim();
        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>
    /// Convierte un texto en un trozo de identificador: minúsculas, sin tildes y con guiones.
    /// "Desarrollador .NET (Híbrido)" → "desarrollador-net-hibrido".
    /// </summary>
    public static string Slug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var withoutAccents = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                withoutAccents.Append(c);
            }
        }

        return NonAlphanumeric().Replace(withoutAccents.ToString(), "-").Trim('-');
    }

    /// <summary>
    /// Identificador para portales cuyo correo no trae un id de oferta: prefijo + empresa + título.
    /// Es el mismo formato que usaba la página de Automatize, así no se duplican las ofertas ya importadas.
    /// </summary>
    public static string ExternalIdFromText(string prefix, string? company, string title)
    {
        var parts = new[] { Slug(company), Slug(title) }.Where(p => p.Length > 0);
        var id = $"{prefix}-{string.Join('-', parts)}";
        return id.Length <= MaxExternalIdLength ? id : id[..MaxExternalIdLength].TrimEnd('-');
    }

    /// <summary>
    /// Deduce la modalidad (Remoto, Híbrido, Presencial) a partir de un texto libre: ubicación, título...
    /// Devuelve null si no hay pistas claras.
    /// </summary>
    public static string? DetectWorkMode(params string?[] texts)
    {
        var all = Slug(string.Join(' ', texts.Where(t => t is not null)));
        if (all.Length == 0)
        {
            return null;
        }

        var words = all.Split('-');
        if (words.Any(w => w is "hibrido" or "hybrid" or "ibrido"))
        {
            return "Híbrido";
        }

        if (words.Any(w => w is "remoto" or "remote" or "teletrabajo" or "remota"))
        {
            return "Remoto";
        }

        if (words.Any(w => w is "presencial" or "presenza" or "onsite") || all.Contains("on-site"))
        {
            return "Presencial";
        }

        return null;
    }

    [GeneratedRegex(@"[​-‏͏⁠﻿]")]
    private static partial Regex InvisibleChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
