using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using BluetoothScheduler.App.UI;
using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Core.Logic;
using BluetoothScheduler.Core.Models;
using Timer = System.Threading.Timer;

namespace BluetoothScheduler.App.Services;

public class AppScheduler : IDisposable
{
    private readonly IClock _clock;
    private readonly IBluetoothController _bluetoothController;
    private readonly IConfigStore _configStore;
    private readonly IStateStore _stateStore;
    private readonly IStartupRegistrar _startupRegistrar;
    private readonly ILogger _logger;

    private ScheduleConfig _config = new();
    private ScheduleState _state = new();
    private BluetoothRadioState _currentRadioState = BluetoothRadioState.Unknown;

    private Timer? _nextScheduledTimer;
    private readonly SemaphoreSlim _evalLock = new(1, 1);
    private bool _isDisposed;

    public ScheduleConfig Config => _config;
    public ScheduleState State => _state;
    public BluetoothRadioState CurrentRadioState => _currentRadioState;
    public Icon AppIcon { get; private set; }

    public event EventHandler? StatusChanged;
    public event EventHandler<(string Title, string Message)>? ShowNotificationRequested;

    public AppScheduler(
        IClock clock,
        IBluetoothController bluetoothController,
        IConfigStore configStore,
        IStateStore stateStore,
        IStartupRegistrar startupRegistrar,
        ILogger logger)
    {
        _clock = clock;
        _bluetoothController = bluetoothController;
        _configStore = configStore;
        _stateStore = stateStore;
        _startupRegistrar = startupRegistrar;
        _logger = logger;

        AppIcon = IconGenerator.CreateBluetoothIcon(true, Theme.IsDark);
    }

    public async Task StartAsync()
    {
        _logger.Info("Iniciando AppScheduler...");

        _config = await _configStore.LoadAsync();
        _state = await _stateStore.LoadAsync();
        _currentRadioState = await _bluetoothController.GetRadioStateAsync();

        _bluetoothController.StateChanged += OnBluetoothRadioStateChanged;

        // Suscribirse a eventos de Windows
        SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;
        SystemEvents.SessionSwitch += OnSystemSessionSwitch;
        SystemEvents.TimeChanged += OnSystemTimeChanged;

        await EvaluateAndScheduleAsync(TriggerSource.Startup);
    }

    private async void OnBluetoothRadioStateChanged(object? sender, BluetoothRadioState newState)
    {
        _logger.Info($"AppScheduler recibió cambio de radio a: {newState}");
        _currentRadioState = newState;
        UpdateAppIcon();
        await EvaluateAndScheduleAsync(TriggerSource.RadioStateChanged);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void OnSystemPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            _logger.Info("Detección de evento de energía: Resume del sistema.");
            await EvaluateAndScheduleAsync(TriggerSource.SystemResume);
        }
    }

    private async void OnSystemSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            _logger.Info("Detección de evento de sesión: Desbloqueo de usuario (SessionUnlock).");
            await EvaluateAndScheduleAsync(TriggerSource.SessionUnlock);
        }
    }

    private async void OnSystemTimeChanged(object? sender, EventArgs e)
    {
        _logger.Info("Detección de cambio en reloj o zona horaria del sistema.");
        await EvaluateAndScheduleAsync(TriggerSource.TimeChanged);
    }

    public async Task UpdateConfigAsync(ScheduleConfig newConfig)
    {
        _logger.Info("Actualizando configuración de ScheduleConfig...");
        bool startWithWindowsChanged = _config.StartWithWindows != newConfig.StartWithWindows;
        _config = newConfig;
        await _configStore.SaveAsync(_config);

        if (startWithWindowsChanged)
        {
            _startupRegistrar.SetRegistered(_config.StartWithWindows);
        }

        await EvaluateAndScheduleAsync(TriggerSource.ConfigChanged);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task TogglePauseTodayAsync()
    {
        DateOnly today = _clock.Today;
        if (_state.IsPaused(today))
        {
            _logger.Info("Despausando scheduler...");
            _state = _state with { PausedUntilDate = null };
        }
        else
        {
            _logger.Info($"Pausando scheduler por hoy ({today:yyyy-MM-dd})...");
            _state = _state with { PausedUntilDate = today };
        }

        await _stateStore.SaveAsync(_state);
        await EvaluateAndScheduleAsync(TriggerSource.ManualEvaluation);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task EvaluateAndScheduleAsync(TriggerSource trigger)
    {
        await _evalLock.WaitAsync();
        try
        {
            DateTime now = _clock.Now;
            _currentRadioState = await _bluetoothController.GetRadioStateAsync();

            _logger.Info($"Evaluando política con trigger: {trigger} a las {now:yyyy-MM-dd HH:mm:ss} | Radio: {_currentRadioState}");
            var decision = SchedulePolicy.Evaluate(_config, _state, now, _currentRadioState, trigger);

            _logger.Info($"Decisión de política: Acción={decision.Action}, Razón={decision.Reason}, PróximoCheck={decision.NextCheckTime:yyyy-MM-dd HH:mm:ss}");

            if (decision.NewState != _state)
            {
                _state = decision.NewState;
                await _stateStore.SaveAsync(_state);
            }

            // Ejecutar acción si aplica
            if (decision.Action == ScheduleAction.TurnOn)
            {
                _logger.Info("Ejecutando acción: Encender Bluetooth...");
                bool success = await _bluetoothController.SetRadioStateAsync(BluetoothRadioState.On);
                if (success)
                {
                    _currentRadioState = BluetoothRadioState.On;
                    if (_config.ShowNotifications)
                    {
                        ShowNotificationRequested?.Invoke(this, (Strings.NotifTitle, Strings.NotifTurnedOn));
                    }
                }
            }
            else if (decision.Action == ScheduleAction.TurnOff)
            {
                _logger.Info("Ejecutando acción: Apagar Bluetooth...");
                bool success = await _bluetoothController.SetRadioStateAsync(BluetoothRadioState.Off);
                if (success)
                {
                    _currentRadioState = BluetoothRadioState.Off;
                    if (_config.ShowNotifications)
                    {
                        string msg = (trigger == TriggerSource.RadioStateChanged && _config.ForceOff) 
                            ? Strings.NotifForceOff 
                            : Strings.NotifTurnedOff;
                        ShowNotificationRequested?.Invoke(this, (Strings.NotifTitle, msg));
                    }
                }
            }

            UpdateAppIcon();
            ScheduleNextTimer(decision.NextCheckTime, now);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.Error("Excepción durante EvaluateAndScheduleAsync.", ex);
        }
        finally
        {
            _evalLock.Release();
        }
    }

    private void ScheduleNextTimer(DateTime? nextCheckTime, DateTime now)
    {
        _nextScheduledTimer?.Dispose();
        _nextScheduledTimer = null;

        if (!nextCheckTime.HasValue)
        {
            nextCheckTime = SchedulePolicy.CalculateNextScheduledTime(_config, _state, now);
        }

        var delay = nextCheckTime.Value - now;
        if (delay < TimeSpan.FromSeconds(2))
        {
            delay = TimeSpan.FromSeconds(2);
        }

        _logger.Info($"Timer programado para {nextCheckTime.Value:yyyy-MM-dd HH:mm:ss} (en {delay.TotalMinutes:F1} minutos)");

        _nextScheduledTimer = new Timer(async _ =>
        {
            _logger.Info("Timer programado disparado.");
            await EvaluateAndScheduleAsync(TriggerSource.ScheduledTimer);
        }, null, delay, Timeout.InfiniteTimeSpan);
    }

    private void UpdateAppIcon()
    {
        bool isRadioOn = (_currentRadioState == BluetoothRadioState.On);
        var oldIcon = AppIcon;
        AppIcon = IconGenerator.CreateBluetoothIcon(isRadioOn, Theme.IsDark);
        oldIcon?.Dispose();
    }

    public string GetStatusDescription()
    {
        DateTime now = _clock.Now;
        DateOnly today = _clock.Today;
        TimeOnly currentTime = _clock.TimeOfDay;

        if (_state.IsPaused(today))
        {
            return $"{Strings.StatusBluetoothOn} · {Strings.StatusPaused}";
        }

        if (!_config.ActiveDays.Contains(now.DayOfWeek))
        {
            return Strings.StatusInactiveDay;
        }

        string radioText = _currentRadioState switch
        {
            BluetoothRadioState.On => Strings.StatusBluetoothOn,
            BluetoothRadioState.Off => Strings.StatusBluetoothOff,
            _ => Strings.StatusBluetoothUnknown
        };

        if (currentTime < _config.OnTime)
        {
            return $"{radioText} · {string.Format(Strings.StatusTurnsOnAt, _config.OnTime.ToString("HH:mm"))}";
        }
        else if (currentTime < _config.OffTime)
        {
            return $"{radioText} · {string.Format(Strings.StatusTurnsOffAt, _config.OffTime.ToString("HH:mm"))}";
        }
        else
        {
            return $"{radioText} · {string.Format(Strings.StatusTurnsOnAt, _config.OnTime.ToString("HH:mm"))}";
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSystemSessionSwitch;
        SystemEvents.TimeChanged -= OnSystemTimeChanged;

        _bluetoothController.StateChanged -= OnBluetoothRadioStateChanged;
        _nextScheduledTimer?.Dispose();
        _evalLock.Dispose();
        AppIcon?.Dispose();
    }
}
