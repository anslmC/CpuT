namespace CpuT.Core;

internal sealed class CooldownPolicy
{
    private static readonly TimeSpan InitialDuration = TimeSpan.FromSeconds(2);
    private DateTimeOffset? cooldownUntil;

    public bool IsActive(DateTimeOffset now) => cooldownUntil is { } until && now < until;

    public void Start(DateTimeOffset now) => cooldownUntil = now + InitialDuration;

    public void Clear() => cooldownUntil = null;
}
