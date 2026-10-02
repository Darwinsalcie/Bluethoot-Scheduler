using BluetoothScheduler.App.Services;
using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Infrastructure.Bluetooth;
using BluetoothScheduler.Infrastructure.Persistence;
using BluetoothScheduler.Infrastructure.Services;

namespace BluetoothScheduler.App;

internal static class Program
{
    private const string MutexName = @"Global\BluetoothScheduler_SingleInstance_Mutex_39F47A";

    [STAThread]
    static async Task Main(string[] args)
    {
        // Directorio de datos %AppData%\BluetoothScheduler
        string appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BluetoothScheduler"
        );
        ILogger logger = new FileLogger(appDataDir);

        // Modo pre-inicio (Ejecutado por Windows Task Scheduler antes del inicio de sesión)
        if (args.Length > 0 && (args.Contains("--boot") || args.Contains("--enable-now")))
        {
            logger.Info("Ejecutando en modo pre-inicio (--boot). Encendiendo Bluetooth para pantalla de bloqueo...");
            try
            {
                using var quickBtController = new WinRtBluetoothController(logger);
                await quickBtController.InitializeAsync();
                bool ok = await quickBtController.SetRadioStateAsync(Core.Models.BluetoothRadioState.On);
                logger.Info($"Modo --boot completado. Resultado encendido: {ok}");
            }
            catch (Exception ex)
            {
                logger.Error("Excepción en modo --boot al encender Bluetooth.", ex);
            }
            return;
        }

        // 1. Instancia única mediante Mutex para la aplicación de bandeja
        using var mutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Ya hay una instancia ejecutándose, terminar de inmediato
            return;
        }

        // 2. Configuración DPI y WinForms
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        logger.Info("Iniciando BluetoothScheduler v1.0 (Modo Interactivo)");

        // 3. Composición manual de dependencias
        IClock clock = new SystemClock();
        IConfigStore configStore = new JsonConfigStore(appDataDir, logger);
        IStateStore stateStore = new JsonStateStore(appDataDir, logger);
        IStartupRegistrar startupRegistrar = new StartupRegistrar(logger);
        var bluetoothController = new WinRtBluetoothController(logger);

        // Inicializar controlador Bluetooth
        await bluetoothController.InitializeAsync();

        using var scheduler = new AppScheduler(
            clock,
            bluetoothController,
            configStore,
            stateStore,
            startupRegistrar,
            logger
        );

        await scheduler.StartAsync();

        // 4. Iniciar contexto de aplicación en segundo plano (bandeja)
        var appCtx = new TrayApplicationContext(scheduler, startupRegistrar);
        Application.Run(appCtx);

        logger.Info("BluetoothScheduler terminado normalmente.");
    }
}
