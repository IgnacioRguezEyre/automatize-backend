namespace Empleos.Application.Tests.Fakes;

/// <summary>Reloj parado en una fecha concreta, para que "hoy" sea siempre el mismo día en los tests.</summary>
internal sealed class FixedClock(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
