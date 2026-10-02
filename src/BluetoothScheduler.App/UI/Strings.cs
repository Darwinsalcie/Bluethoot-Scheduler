namespace BluetoothScheduler.App.UI;

public static class Strings
{
    public const string AppTitle = "Bluetooth Scheduler";
    public const string TrayTooltip = "Bluetooth Scheduler";
    
    // Tray Menu
    public const string MenuOpen = "Abrir configuración";
    public const string MenuPauseToday = "Pausar por hoy";
    public const string MenuUnpause = "Reanudar scheduler";
    public const string MenuStartWithWindows = "Iniciar con Windows";
    public const string MenuExit = "Salir";

    // Form
    public const string HeaderTitle = "Bluetooth Scheduler";
    public const string StatusBluetoothOn = "Bluetooth encendido";
    public const string StatusBluetoothOff = "Bluetooth apagado";
    public const string StatusBluetoothUnknown = "Bluetooth no disponible";
    public const string StatusTurnsOffAt = "se apagará a las {0}";
    public const string StatusTurnsOnAt = "se encenderá a las {0}";
    public const string StatusPaused = "Pausado por hoy";
    public const string StatusInactiveDay = "Día no activo";

    // Sections
    public const string SectionActiveDays = "DÍAS ACTIVOS";
    public const string SectionSchedule = "HORARIO";
    public const string SectionOptions = "OPCIONES";

    // Fields
    public const string LabelTurnOnAt = "Encender a las";
    public const string LabelTurnOffAt = "Apagar a las";
    public const string ErrorTimeValidation = "La hora de encendido debe ser anterior a la de apagado";
    public const string ErrorNoDaysSelected = "Debes seleccionar al menos un día activo";

    // Toggles
    public const string OptionForceOffTitle = "Forzar apagado tras la hora límite";
    public const string OptionForceOffDesc = "Vuelve a apagar el Bluetooth si se enciende dentro de la ventana OFF.";

    public const string OptionStartWithWindowsTitle = "Iniciar con Windows";
    public const string OptionStartWithWindowsDesc = "Ejecutar en segundo plano al iniciar sesión del usuario.";

    public const string OptionNotificationsTitle = "Mostrar notificaciones";
    public const string OptionNotificationsDesc = "Avisar en la bandeja del sistema cuando se cambie el estado.";

    // Footer
    public const string BtnRestoreDefaults = "Restaurar valores por defecto";
    public const string StatusSaved = "Guardado";

    // Balloon notifications
    public const string NotifTitle = "Bluetooth Scheduler";
    public const string NotifTurnedOn = "Bluetooth se ha encendido automáticamente según el horario programado.";
    public const string NotifTurnedOff = "Bluetooth se ha apagado automáticamente según el horario programado.";
    public const string NotifForceOff = "Bluetooth se ha apagado automáticamente (ventana OFF activa).";
}
