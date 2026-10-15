using Integraciones.Application.Sync;

namespace Integraciones.Tests.Sync;

public class SyncGateTests
{
    [Fact]
    public async Task A_second_sync_is_rejected_while_the_first_is_running()
    {
        var gate = new SyncGate();
        var release = new TaskCompletionSource();
        var report = new SyncReport(DateTimeOffset.UtcNow, "q", false, 0, 0, 0, 0, new Dictionary<string, int>(), null, null);

        var first = gate.TryRunAsync(async () => { await release.Task; return report; });
        var second = await gate.TryRunAsync(() => Task.FromResult(report));

        Assert.Null(second);
        release.SetResult();
        Assert.Same(report, await first);
        Assert.Same(report, await gate.TryRunAsync(() => Task.FromResult(report))); // libre otra vez
    }
}
