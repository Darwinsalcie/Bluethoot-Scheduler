using BluetoothScheduler.Core.Models;

namespace BluetoothScheduler.Core.Logic;

public record PolicyDecision(
    ScheduleAction Action,
    ScheduleState NewState,
    DateTime? NextCheckTime,
    string Reason
);

public static class SchedulePolicy
{
    public static PolicyDecision Evaluate(
        ScheduleConfig config,
        ScheduleState state,
        DateTime now,
        BluetoothRadioState currentRadioState,
        TriggerSource trigger)
    {
        DateOnly today = DateOnly.FromDateTime(now);
        TimeOnly currentTime = TimeOnly.FromDateTime(now);
        DayOfWeek currentDay = now.DayOfWeek;

        // 1. Validar configuración
        if (!config.IsValid())
        {
            DateTime nextRetry = now.AddMinutes(5);
            return new PolicyDecision(ScheduleAction.None, state, nextRetry, "Configuración inválida (OnTime >= OffTime)");
        }

        // 2. Comprobar si está pausado hoy
        if (state.IsPaused(today))
        {
            DateTime midnightTomorrow = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
            return new PolicyDecision(ScheduleAction.None, state, midnightTomorrow, "Pausado hasta mañana");
        }

        // 3. Comprobar si hoy es un día activo
        bool isActiveDay = config.ActiveDays.Contains(currentDay);
        if (!isActiveDay)
        {
            DateTime midnightTomorrow = today.AddDays(1).ToDateTime(TimeOnly.MinValue);
            return new PolicyDecision(ScheduleAction.None, state, midnightTomorrow, $"Hoy ({currentDay}) no es un día activo");
        }

        // 4. Ventana Antes de OnTime: [00:00, OnTime)
        if (currentTime < config.OnTime)
        {
            DateTime nextOnTime = today.ToDateTime(config.OnTime);
            return new PolicyDecision(ScheduleAction.None, state, nextOnTime, $"Antes de la ventana activa. Próximo encendido a las {config.OnTime:HH\\:mm}");
        }

        // 5. Ventana ON: [OnTime, OffTime)
        if (currentTime >= config.OnTime && currentTime < config.OffTime)
        {
            DateTime nextOffTime = today.ToDateTime(config.OffTime);

            // Si el trigger fue un cambio de estado del radio (ej. usuario lo apagó manualmente dentro de la ventana ON)
            // No peleamos con el usuario ni lo forzamos.
            if (trigger == TriggerSource.RadioStateChanged)
            {
                return new PolicyDecision(ScheduleAction.None, state, nextOffTime, "Cambio manual durante ventana activa respetado");
            }

            // Si ya se aplicó el encendido hoy, no volver a encender (incluso tras reinicio, suspensión, etc.)
            if (state.LastOnDate == today)
            {
                return new PolicyDecision(ScheduleAction.None, state, nextOffTime, $"Ventana ON ya ejecutada hoy ({today:yyyy-MM-dd}). Próximo evento: {config.OffTime:HH\\:mm}");
            }

            // Primer arranque o entrada a la ventana ON sin haber ejecutado hoy
            var updatedState = state with { LastOnDate = today };
            var action = (currentRadioState == BluetoothRadioState.On) ? ScheduleAction.None : ScheduleAction.TurnOn;
            return new PolicyDecision(action, updatedState, nextOffTime, $"Encendido de ventana activa ({config.OnTime:HH\\:mm} - {config.OffTime:HH\\:mm})");
        }

        // 6. Ventana OFF: [OffTime, 23:59:59.999]
        if (currentTime >= config.OffTime)
        {
            DateTime midnightTomorrow = today.AddDays(1).ToDateTime(TimeOnly.MinValue);

            // Reacción a RadioStateChanged en ventana OFF
            if (trigger == TriggerSource.RadioStateChanged)
            {
                if (config.ForceOff && currentRadioState == BluetoothRadioState.On)
                {
                    var updatedState = state with { LastOffDate = today };
                    return new PolicyDecision(ScheduleAction.TurnOff, updatedState, midnightTomorrow, "ForceOff activo: usuario encendió Bluetooth en ventana OFF, forzando apagado");
                }
                return new PolicyDecision(ScheduleAction.None, state, midnightTomorrow, "Cambio de radio en ventana OFF (ForceOff inactivo o ya apagado)");
            }

            // Transición normal, inicio, desbloqueo o reanudación en ventana OFF
            // Si ya se ejecutó el apagado hoy y no está ForceOff, no necesitamos hacer nada a menos que esté On y no se haya apagado aún
            if (state.LastOffDate == today && !config.ForceOff)
            {
                return new PolicyDecision(ScheduleAction.None, state, midnightTomorrow, $"Ventana OFF ya procesada hoy ({today:yyyy-MM-dd})");
            }

            if (currentRadioState == BluetoothRadioState.Off)
            {
                var updatedState = state with { LastOffDate = today };
                return new PolicyDecision(ScheduleAction.None, updatedState, midnightTomorrow, "Radio ya está apagado en ventana OFF");
            }

            var newState = state with { LastOffDate = today };
            return new PolicyDecision(ScheduleAction.TurnOff, newState, midnightTomorrow, $"Apagado de ventana OFF (a partir de las {config.OffTime:HH\\:mm})");
        }

        // Fallback
        DateTime fallbackNext = now.AddMinutes(30);
        return new PolicyDecision(ScheduleAction.None, state, fallbackNext, "Evaluación por defecto");
    }

    public static DateTime CalculateNextScheduledTime(ScheduleConfig config, ScheduleState state, DateTime now)
    {
        DateOnly today = DateOnly.FromDateTime(now);
        TimeOnly currentTime = TimeOnly.FromDateTime(now);

        if (state.IsPaused(today))
        {
            return today.AddDays(1).ToDateTime(TimeOnly.MinValue);
        }

        if (!config.ActiveDays.Contains(now.DayOfWeek))
        {
            // Siguiente medianoche
            return today.AddDays(1).ToDateTime(TimeOnly.MinValue);
        }

        if (currentTime < config.OnTime)
        {
            return today.ToDateTime(config.OnTime);
        }

        if (currentTime < config.OffTime)
        {
            return today.ToDateTime(config.OffTime);
        }

        return today.AddDays(1).ToDateTime(TimeOnly.MinValue);
    }
}
