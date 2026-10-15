namespace Integraciones.Application.Sync;

/// <summary>
/// Evita dos sincronizaciones a la vez (la automática y una pedida a mano por la API). Es un singleton:
/// quien llega mientras otra está en marcha no espera: recibe <c>null</c>.
/// </summary>
public sealed class SyncGate
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<SyncReport?> TryRunAsync(Func<Task<SyncReport>> sync)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero))
        {
            return null;
        }

        try
        {
            return await sync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
