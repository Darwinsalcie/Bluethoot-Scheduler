namespace BluetoothScheduler.Core.Models;

public record ScheduleConfig
{
    public HashSet<DayOfWeek> ActiveDays { get; init; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    public TimeOnly OnTime { get; init; } = new(8, 0);
    public TimeOnly OffTime { get; init; } = new(16, 0);

    public bool ForceOff { get; init; } = true;
    public bool StartWithWindows { get; init; } = false;
    public bool ShowNotifications { get; init; } = true;
    public bool DarkMode { get; init; } = false;

    public bool IsValid() => OnTime < OffTime;
}
