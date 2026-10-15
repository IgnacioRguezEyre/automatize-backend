using System.Net;

namespace Integraciones.Tests.Fakes;

/// <summary>
/// Sustituye a la red en los tests del cliente HTTP: devuelve las respuestas que se le dan, en orden,
/// y guarda las peticiones para poder comprobarlas.
/// </summary>
internal sealed class StubHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        var index = Math.Min(Requests.Count, responses.Length) - 1;
        return responses[index]();
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
}
