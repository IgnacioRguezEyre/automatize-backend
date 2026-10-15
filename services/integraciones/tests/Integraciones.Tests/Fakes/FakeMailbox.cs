using Integraciones.Application.Mail;

namespace Integraciones.Tests.Fakes;

internal sealed class FakeMailbox(params EmailMessage[] messages) : IMailboxReader
{
    public string? LastQuery { get; private set; }

    public Task<IReadOnlyList<EmailMessage>> SearchAsync(string query, int maxMessages, CancellationToken cancellationToken)
    {
        LastQuery = query;
        return Task.FromResult<IReadOnlyList<EmailMessage>>(messages.Take(maxMessages).ToList());
    }
}
