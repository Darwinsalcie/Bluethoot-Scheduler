using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Core.Logic;
using BluetoothScheduler.Core.Models;
using Xunit;

namespace BluetoothScheduler.Tests;

public class MockClock : IClock
{
    public DateTime CurrentTime { get; set; }
    public DateTime Now => CurrentTime;
}

public class SchedulePolicyTests
{
    private readonly ScheduleConfig _defaultConfig = new()
    {
        ActiveDays = new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
        },
        OnTime = new TimeOnly(8, 0),
        OffTime = new TimeOnly(16, 0),
        ForceOff = true,
        ShowNotifications = true
    };

    [Fact]
    public void Weekend_DoesNothing_NextCheckIsMidnight()
    {
        // Sábado 2026-10-03 10:00
        var now = new DateTime(2026, 10, 3, 10, 0, 0);
        var state = new ScheduleState();

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
        Assert.Equal(new DateTime(2026, 10, 4, 0, 0, 0), decision.NextCheckTime);
        Assert.Contains("no es un día activo", decision.Reason);
    }

    [Fact]
    public void BeforeOnTime_DoesNothing_NextCheckIsOnTime()
    {
        // Lunes 2026-10-05 07:15
        var now = new DateTime(2026, 10, 5, 7, 15, 0);
        var state = new ScheduleState();

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
        Assert.Equal(new DateTime(2026, 10, 5, 8, 0, 0), decision.NextCheckTime);
    }

    [Fact]
    public void FirstStartupOfDay_At10AM_TurnsOn()
    {
        // Lunes 2026-10-05 10:00
        var now = new DateTime(2026, 10, 5, 10, 0, 0);
        var state = new ScheduleState();

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.TurnOn, decision.Action);
        Assert.Equal(DateOnly.FromDateTime(now), decision.NewState.LastOnDate);
        Assert.Equal(new DateTime(2026, 10, 5, 16, 0, 0), decision.NextCheckTime);
    }

    [Fact]
    public void ManualOffAt9AM_RestartAt10AM_DoesNotTurnOnAgain()
    {
        // Supongamos que a las 08:00 se encendió y registró LastOnDate
        var today = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { LastOnDate = today };
        
        // El usuario lo apagó a las 09:00. Se reinicia la app a las 10:00
        var now = new DateTime(2026, 10, 5, 10, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
        Assert.Equal(new DateTime(2026, 10, 5, 16, 0, 0), decision.NextCheckTime);
    }

    [Fact]
    public void ManualOnAt11AM_TransitionsToOffAt16PM_TurnsOff()
    {
        // Usuario encendió manualmente. Llegan las 16:00 (límite de ventana OFF)
        var today = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { LastOnDate = today };
        var now = new DateTime(2026, 10, 5, 16, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.On,
            TriggerSource.ScheduledTimer
        );

        Assert.Equal(ScheduleAction.TurnOff, decision.Action);
        Assert.Equal(today, decision.NewState.LastOffDate);
        Assert.Equal(new DateTime(2026, 10, 6, 0, 0, 0), decision.NextCheckTime);
    }

    [Fact]
    public void ForceOff_UserTurnsOnAt17PM_TurnsOffImmediately()
    {
        var today = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { LastOnDate = today, LastOffDate = today };
        var now = new DateTime(2026, 10, 5, 17, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.On,
            TriggerSource.RadioStateChanged
        );

        Assert.Equal(ScheduleAction.TurnOff, decision.Action);
    }

    [Fact]
    public void ForceOffDisabled_UserTurnsOnAt17PM_DoesNotTurnOff()
    {
        var configNoForce = _defaultConfig with { ForceOff = false };
        var today = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { LastOnDate = today, LastOffDate = today };
        var now = new DateTime(2026, 10, 5, 17, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            configNoForce,
            state,
            now,
            BluetoothRadioState.On,
            TriggerSource.RadioStateChanged
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
    }

    [Fact]
    public void PausedToday_DoesNothingUntilTomorrow()
    {
        var today = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { PausedUntilDate = today };
        var now = new DateTime(2026, 10, 5, 10, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
        Assert.Equal(new DateTime(2026, 10, 6, 0, 0, 0), decision.NextCheckTime);
        Assert.Contains("Pausado", decision.Reason);
    }

    [Fact]
    public void DayRollOver_ResetsBehaviorNextDay()
    {
        // Día 1
        var day1 = new DateOnly(2026, 10, 5);
        var state = new ScheduleState { LastOnDate = day1, LastOffDate = day1 };

        // Día 2 a las 09:00
        var day2Now = new DateTime(2026, 10, 6, 9, 0, 0);

        var decision = SchedulePolicy.Evaluate(
            _defaultConfig,
            state,
            day2Now,
            BluetoothRadioState.Off,
            TriggerSource.Startup
        );

        Assert.Equal(ScheduleAction.TurnOn, decision.Action);
        Assert.Equal(new DateOnly(2026, 10, 6), decision.NewState.LastOnDate);
    }

    [Fact]
    public void HotConfigChange_InvalidConfig_ReturnsNoneWithRetry()
    {
        var invalidConfig = _defaultConfig with
        {
            OnTime = new TimeOnly(18, 0),
            OffTime = new TimeOnly(8, 0) // On > Off es inválido
        };
        var now = new DateTime(2026, 10, 5, 10, 0, 0);
        var state = new ScheduleState();

        var decision = SchedulePolicy.Evaluate(
            invalidConfig,
            state,
            now,
            BluetoothRadioState.Off,
            TriggerSource.ConfigChanged
        );

        Assert.Equal(ScheduleAction.None, decision.Action);
        Assert.Contains("Configuración inválida", decision.Reason);
    }
}
