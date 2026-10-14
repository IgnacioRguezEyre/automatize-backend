using Empleos.Application.Sources;
using Empleos.Domain.Sources;
using Microsoft.EntityFrameworkCore;

namespace Empleos.Infrastructure.Persistence;

internal sealed class SourceRepository(JobsDbContext db) : ISourceRepository
{
    public Task<Source?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Sources.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Source>> ListAsync(CancellationToken ct) =>
        await db.Sources.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, CancellationToken ct) =>
        db.Sources.AnyAsync(s => s.Name == name, ct);

    public void Add(Source source) => db.Sources.Add(source);

    public void Remove(Source source) => db.Sources.Remove(source);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
