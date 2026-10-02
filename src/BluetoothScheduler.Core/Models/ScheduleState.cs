namespace BluetoothScheduler.Core.Models;

public record ScheduleState
{
    public DateOnly? LastOnDate { get; init; }
    public DateOnly? LastOffDate { get; init; }
    public DateOnly? PausedUntilDate { get; init; }

    public bool IsPaused(DateOnly today) => PausedUntilDate.HasValue && today <= PausedUntilDate.Value;
}
