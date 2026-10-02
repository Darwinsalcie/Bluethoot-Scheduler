namespace BluetoothScheduler.Core.Models;

public enum BluetoothRadioState
{
    Unknown,
    On,
    Off,
    Disabled
}

public enum ScheduleAction
{
    None,
    TurnOn,
    TurnOff
}

public enum TriggerSource
{
    Startup,
    ScheduledTimer,
    SystemResume,
    SessionUnlock,
    TimeChanged,
    RadioStateChanged,
    ManualEvaluation,
    ConfigChanged
}
