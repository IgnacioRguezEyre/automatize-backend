using Empleos.Application.Sources;
using Empleos.Domain.Sources;

namespace Empleos.Application.Tests.Fakes;

internal sealed class InMemorySourceRepository : ISourceRepository
{
    public List<Source> Sources { get; } = [];
    public int SaveCount { get; private set; }

    public Task<Source?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Sources.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Source>> ListAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Source>>(Sources.OrderBy(s => s.Name).ToList());

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        Task.FromResult(Sources.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)));

    public void Add(Source source) => Sources.Add(source);

    public void Remove(Source source) => Sources.Remove(source);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
