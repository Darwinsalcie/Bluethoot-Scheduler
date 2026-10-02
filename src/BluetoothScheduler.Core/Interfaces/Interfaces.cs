using BluetoothScheduler.Core.Models;

namespace BluetoothScheduler.Core.Interfaces;

public interface IClock
{
    DateTime Now { get; }
    DateOnly Today => DateOnly.FromDateTime(Now);
    TimeOnly TimeOfDay => TimeOnly.FromDateTime(Now);
}

public interface IBluetoothController
{
    Task<BluetoothRadioState> GetRadioStateAsync(CancellationToken cancellationToken = default);
    Task<bool> SetRadioStateAsync(BluetoothRadioState state, CancellationToken cancellationToken = default);
    event EventHandler<BluetoothRadioState>? StateChanged;
    bool IsAvailable { get; }
}

public interface IConfigStore
{
    Task<ScheduleConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ScheduleConfig config, CancellationToken cancellationToken = default);
}

public interface IStateStore
{
    Task<ScheduleState> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ScheduleState state, CancellationToken cancellationToken = default);
}

public interface IStartupRegistrar
{
    bool IsRegistered();
    void SetRegistered(bool enable);
}

public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
}
