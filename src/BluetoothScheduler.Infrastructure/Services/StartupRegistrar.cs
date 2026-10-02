using System.Diagnostics;
using Microsoft.Win32;
using BluetoothScheduler.Core.Interfaces;

namespace BluetoothScheduler.Infrastructure.Services;

public class StartupRegistrar : IStartupRegistrar
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "BluetoothScheduler";
    private const string BootTaskName = "BluetoothScheduler_Boot";
    private readonly ILogger _logger;

    public StartupRegistrar(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppName) != null;
        }
        catch (Exception ex)
        {
            _logger.Error("Error comprobando el registro de inicio con Windows.", ex);
            return false;
        }
    }

    public void SetRegistered(bool enable)
    {
        try
        {
            // 1. Registro en HKCU\Run para la sesión del usuario (Bandeja / UI interactiva)
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key != null)
            {
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        _logger.Info($"Inicio con Windows (sesión de usuario) activado: {exePath}");
                    }
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                        _logger.Info("Inicio con Windows (sesión de usuario) desactivado.");
                    }
                }
            }

            // 2. Gestión de la Tarea Programada al arrancar el sistema (Pre-Login / Pantalla de bloqueo)
            ConfigureBootTask(enable);
        }
        catch (Exception ex)
        {
            _logger.Error($"Error cambiando el estado de inicio con Windows a {enable}.", ex);
        }
    }

    private void ConfigureBootTask(bool enable)
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath) && enable) return;

        try
        {
            if (enable)
            {
                // Crear tarea programada para ejecutarse al iniciar el sistema con --boot
                string args = $"/create /tn \"{BootTaskName}\" /tr \"\\\"{exePath}\\\" --boot\" /sc onstart /ru \"SYSTEM\" /rl highest /f";
                RunSchtasks(args);
            }
            else
            {
                // Eliminar tarea programada
                string args = $"/delete /tn \"{BootTaskName}\" /f";
                RunSchtasks(args);
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Error configurando la tarea programada de arranque con schtasks.", ex);
        }
    }

    private void RunSchtasks(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(5000);
                string output = proc.StandardOutput.ReadToEnd().Trim();
                string error = proc.StandardError.ReadToEnd().Trim();

                if (proc.ExitCode == 0)
                {
                    _logger.Info($"schtasks completado con éxito. {output}");
                }
                else
                {
                    bool isDelete = arguments.Contains("/delete");
                    bool isAccessDenied = error.Contains("denegado", StringComparison.OrdinalIgnoreCase) || 
                                          error.Contains("denied", StringComparison.OrdinalIgnoreCase);

                    // Si estamos eliminando y la tarea no existe, no es un error
                    if (isDelete && !isAccessDenied)
                    {
                        _logger.Info("La tarea programada no existía previamente.");
                        return;
                    }

                    _logger.Warn($"schtasks salió con código {proc.ExitCode}: {output} {error}");

                    // Solo intentar elevación UAC si fue por falta de permisos
                    if (isAccessDenied)
                    {
                        try
                        {
                            var elevatedPsi = new ProcessStartInfo
                            {
                                FileName = "schtasks.exe",
                                Arguments = arguments,
                                Verb = "runas",
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true
                            };
                            using var elevatedProc = Process.Start(elevatedPsi);
                            elevatedProc?.WaitForExit(5000);
                            _logger.Info($"schtasks ejecutado con elevación. Código de salida: {elevatedProc?.ExitCode}");
                        }
                        catch (Exception uacEx)
                        {
                            _logger.Warn($"No se pudo elevar schtasks (UAC cancelado o denegado): {uacEx.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Excepción al invocar schtasks.exe.", ex);
        }
    }
}

