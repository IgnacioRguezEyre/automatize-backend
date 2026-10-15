using Integraciones.Application.Empleos;

namespace Integraciones.Tests.Fakes;

/// <summary>Simula Empleos: recuerda lo que recibe y, como el de verdad, salta los externalId que ya tiene.</summary>
internal sealed class FakeEmpleosClient : IEmpleosClient
{
    private readonly HashSet<string> _existing = new(StringComparer.OrdinalIgnoreCase);

    public List<IReadOnlyList<ImportJobOfferItem>> Calls { get; } = [];

    public Task<ImportResult> ImportAsync(IReadOnlyList<ImportJobOfferItem> items, CancellationToken cancellationToken)
    {
        Calls.Add(items);
        var created = items.Count(i => _existing.Add(i.ExternalId));
        return Task.FromResult(new ImportResult(items.Count, created, items.Count - created, []));
    }
}
