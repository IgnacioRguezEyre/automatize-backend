using Empleos.Domain.Sources;

namespace Empleos.Application.Sources;

public sealed record SourceDto(Guid Id, string Name, IReadOnlyList<string> Senders, bool IsActive, string Rule)
{
    public static SourceDto From(Source source) =>
        new(source.Id, source.Name, source.Senders.ToList(), source.IsActive, source.Rule);
}

public sealed record CreateSourceRequest(string Name, IReadOnlyList<string>? Senders, string? Rule);

public sealed record AddSenderRequest(string Email);

public sealed record UpdateRuleRequest(string? Rule);
