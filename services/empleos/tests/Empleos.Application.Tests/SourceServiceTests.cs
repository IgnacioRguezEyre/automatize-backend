using Empleos.Application.Sources;
using Empleos.Application.Tests.Fakes;
using Empleos.Domain.Common;

namespace Empleos.Application.Tests;

public class SourceServiceTests
{
    private readonly InMemorySourceRepository _repository = new();
    private readonly SourceService _service;

    public SourceServiceTests() => _service = new SourceService(_repository);

    private Task<SourceDto> CreateLinkedIn() =>
        _service.CreateAsync(new CreateSourceRequest("LinkedIn", ["jobalerts-noreply@linkedin.com"], null), default);

    [Fact]
    public async Task Create_saves_an_active_source()
    {
        var source = await CreateLinkedIn();

        Assert.True(source.IsActive);
        Assert.Single(_repository.Sources);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Create_with_an_existing_name_throws_and_saves_nothing()
    {
        await CreateLinkedIn();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new CreateSourceRequest("linkedin", null, null), default));
        Assert.Single(_repository.Sources);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task AddSender_adds_it_and_saves()
    {
        var source = await CreateLinkedIn();

        var result = await _service.AddSenderAsync(source.Id, new AddSenderRequest("jobs-noreply@linkedin.com"), default);

        Assert.Equal(2, result!.Senders.Count);
        Assert.Equal(2, _repository.SaveCount);
    }

    [Fact]
    public async Task Deactivate_on_a_missing_source_returns_null()
    {
        Assert.Null(await _service.DeactivateAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Delete_removes_it_and_returns_false_the_second_time()
    {
        var source = await CreateLinkedIn();

        Assert.True(await _service.DeleteAsync(source.Id, default));
        Assert.False(await _service.DeleteAsync(source.Id, default));
        Assert.Empty(_repository.Sources);
    }
}
