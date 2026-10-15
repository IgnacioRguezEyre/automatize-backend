using Empleos.Domain.Common;
using Empleos.Domain.Sources;

namespace Empleos.Domain.Tests.Sources;

public class SourceTests
{
    [Fact]
    public void Create_is_active_and_normalizes_the_senders()
    {
        var source = Source.Create("  LinkedIn ", ["JobAlerts-NoReply@LinkedIn.com "], null);

        Assert.Equal("LinkedIn", source.Name);
        Assert.True(source.IsActive);
        Assert.Equal(string.Empty, source.Rule);
        Assert.Equal(new[] { "jobalerts-noreply@linkedin.com" }, source.Senders);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_without_name_fails(string name)
    {
        Assert.Throws<ArgumentException>(() => Source.Create(name, null, null));
    }

    [Fact]
    public void AddSender_twice_is_ignored()
    {
        var source = Source.Create("Indeed", ["donotreply@match.indeed.com"], null);

        var added = source.AddSender("DONOTREPLY@match.indeed.com");

        Assert.False(added);
        Assert.Single(source.Senders);
    }

    [Theory]
    [InlineData("no-es-un-correo")]
    [InlineData("Juan <juan@ejemplo.com>")]
    [InlineData("")]
    public void AddSender_rejects_invalid_addresses(string email)
    {
        var source = Source.Create("Indeed", null, null);

        Assert.Throws<ArgumentException>(() => source.AddSender(email));
        Assert.Empty(source.Senders);
    }

    [Fact]
    public void AddSender_beyond_the_limit_fails()
    {
        var senders = Enumerable.Range(1, Source.MaxSenders).Select(i => $"alerta{i}@portal.com");
        var source = Source.Create("Portal", senders, null);

        Assert.Throws<DomainException>(() => source.AddSender("otra@portal.com"));
    }

    [Fact]
    public void RemoveSender_removes_it_ignoring_case()
    {
        var source = Source.Create("Glassdoor", ["noreply@glassdoor.com", "jobs@glassdoor.com"], null);

        var removed = source.RemoveSender("NoReply@Glassdoor.com");

        Assert.True(removed);
        Assert.Equal(new[] { "jobs@glassdoor.com" }, source.Senders);
    }

    [Fact]
    public void RemoveSender_returns_false_if_it_was_not_there()
    {
        var source = Source.Create("Glassdoor", ["noreply@glassdoor.com"], null);

        Assert.False(source.RemoveSender("otro@glassdoor.com"));
    }

    [Fact]
    public void Deactivate_and_Activate()
    {
        var source = Source.Create("InfoJobs", null, null);

        source.Deactivate();
        Assert.False(source.IsActive);

        source.Activate();
        Assert.True(source.IsActive);
    }

    [Fact]
    public void UpdateRule_too_long_fails()
    {
        var source = Source.Create("InfoJobs", null, "Solo backend en Pontevedra");

        Assert.Throws<ArgumentException>(() => source.UpdateRule(new string('x', Source.MaxRuleLength + 1)));
        Assert.Equal("Solo backend en Pontevedra", source.Rule);
    }
}
