using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace Integraciones.Application.Parsing.Portals;

/// <summary>Ayudas para recorrer el HTML de los correos con AngleSharp.</summary>
internal static class HtmlTools
{
    private static readonly HtmlParser Parser = new();

    public static IHtmlDocument? Parse(string? html) =>
        string.IsNullOrWhiteSpace(html) ? null : Parser.ParseDocument(html);

    /// <summary>
    /// Textos de los elementos "hoja" (sin elementos hijos), limpios y en orden. Sirve para tarjetas maquetadas
    /// con tablas anidadas, donde lo único estable es el orden en que aparecen los datos.
    /// </summary>
    public static IReadOnlyList<string> LeafTexts(IElement root) =>
        root.QuerySelectorAll("*")
            .Where(e => e.ChildElementCount == 0)
            .Select(e => TextTools.Clean(e.TextContent))
            .OfType<string>()
            .ToList();

    /// <summary>Texto de los nodos de texto que cuelgan directamente del elemento (sin el de sus hijos).</summary>
    public static string? OwnText(IElement element) =>
        TextTools.Clean(string.Concat(element.ChildNodes.OfType<IText>().Select(t => t.TextContent)));
}
