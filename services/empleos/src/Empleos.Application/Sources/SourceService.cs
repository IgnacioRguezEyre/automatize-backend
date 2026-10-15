using Empleos.Domain.Common;
using Empleos.Domain.Sources;

namespace Empleos.Application.Sources;

/// <summary>Casos de uso de las fuentes. Los métodos que reciben un id devuelven null si la fuente no existe.</summary>
public sealed class SourceService(ISourceRepository repository)
{
    public async Task<IReadOnlyList<SourceDto>> ListAsync(CancellationToken ct) =>
        (await repository.ListAsync(ct)).Select(SourceDto.From).ToList();

    public async Task<SourceDto?> GetAsync(Guid id, CancellationToken ct) =>
        await repository.GetByIdAsync(id, ct) is { } source ? SourceDto.From(source) : null;

    public async Task<SourceDto> CreateAsync(CreateSourceRequest request, CancellationToken ct)
    {
        var source = Source.Create(request.Name, request.Senders, request.Rule);
        // La unicidad del nombre no la puede comprobar la entidad sola: necesita mirar las demás fuentes.
        if (await repository.NameExistsAsync(source.Name, ct))
            throw new DomainException($"Ya existe una fuente llamada '{source.Name}'.");

        repository.Add(source);
        await repository.SaveChangesAsync(ct);
        return SourceDto.From(source);
    }

    public Task<SourceDto?> AddSenderAsync(Guid id, AddSenderRequest request, CancellationToken ct) =>
        UpdateAsync(id, s => s.AddSender(request.Email), ct);

    public Task<SourceDto?> RemoveSenderAsync(Guid id, string email, CancellationToken ct) =>
        UpdateAsync(id, s => s.RemoveSender(email), ct);

    public Task<SourceDto?> ActivateAsync(Guid id, CancellationToken ct) => UpdateAsync(id, s => s.Activate(), ct);

    public Task<SourceDto?> DeactivateAsync(Guid id, CancellationToken ct) => UpdateAsync(id, s => s.Deactivate(), ct);

    public Task<SourceDto?> UpdateRuleAsync(Guid id, UpdateRuleRequest request, CancellationToken ct) =>
        UpdateAsync(id, s => s.UpdateRule(request.Rule), ct);

    /// <summary>Devuelve false si la fuente no existe.</summary>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var source = await repository.GetByIdAsync(id, ct);
        if (source is null) return false;
        repository.Remove(source);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private async Task<SourceDto?> UpdateAsync(Guid id, Action<Source> change, CancellationToken ct)
    {
        var source = await repository.GetByIdAsync(id, ct);
        if (source is null) return null;
        change(source);
        await repository.SaveChangesAsync(ct);
        return SourceDto.From(source);
    }
}
