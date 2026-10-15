using Empleos.Domain.Sources;

namespace Empleos.Application.Sources;

public interface ISourceRepository
{
    Task<Source?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Source>> ListAsync(CancellationToken ct);
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
    void Add(Source source);
    void Remove(Source source);
    Task SaveChangesAsync(CancellationToken ct);
}
