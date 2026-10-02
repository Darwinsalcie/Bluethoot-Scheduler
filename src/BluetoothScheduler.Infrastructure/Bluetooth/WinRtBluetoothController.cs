using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Radios;
using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Core.Models;

namespace BluetoothScheduler.Infrastructure.Bluetooth;

public class WinRtBluetoothController : IBluetoothController, IDisposable
{
    private readonly ILogger _logger;
    private Radio? _radio;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isDisposed;
    private bool _isInternalChange;

    public event EventHandler<BluetoothRadioState>? StateChanged;

    public bool IsAvailable => _radio != null;

    public WinRtBluetoothController(ILogger logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureRadioInitializedInternalAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<bool> EnsureRadioInitializedInternalAsync()
    {
        if (_radio != null) return true;

        try
        {
            var access = await Radio.RequestAccessAsync();
            if (access != RadioAccessStatus.Allowed)
            {
                _logger.Warn($"Acceso a radios denegado por Windows: {access}");
                return false;
            }

            var radios = await Radio.GetRadiosAsync();
            var btRadio = radios.FirstOrDefault(r => r.Kind == RadioKind.Bluetooth);
            if (btRadio == null)
            {
                _logger.Warn("No se encontró ningún adaptador Bluetooth en el sistema.");
                return false;
            }

            _radio = btRadio;
            _radio.StateChanged += OnRadioStateChanged;
            _logger.Info($"Adaptador Bluetooth vinculado: {_radio.Name} (Estado actual: {_radio.State})");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Excepción inicializando Radio Bluetooth de WinRT.", ex);
            return false;
        }
    }

    private void OnRadioStateChanged(Radio sender, object args)
    {
        if (_isInternalChange)
        {
            // Ignorar eventos generados por nuestra propia llamada a SetRadioStateAsync
            return;
        }

        var state = ConvertToCoreState(sender.State);
        _logger.Info($"Evento de hardware detectado: Radio.StateChanged -> {state}");
        StateChanged?.Invoke(this, state);
    }

    public async Task<BluetoothRadioState> GetRadioStateAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!await EnsureRadioInitializedInternalAsync() || _radio == null)
            {
                return BluetoothRadioState.Unknown;
            }

            return ConvertToCoreState(_radio.State);
        }
        catch (Exception ex)
        {
            _logger.Error("Error obteniendo estado del radio Bluetooth.", ex);
            return BluetoothRadioState.Unknown;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> SetRadioStateAsync(BluetoothRadioState state, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!await EnsureRadioInitializedInternalAsync() || _radio == null)
            {
                _logger.Warn("No se puede cambiar el estado: radio Bluetooth no disponible.");
                return false;
            }

            var targetState = state switch
            {
                BluetoothRadioState.On => RadioState.On,
                BluetoothRadioState.Off => RadioState.Off,
                _ => RadioState.Unknown
            };

            if (targetState == RadioState.Unknown || _radio.State == targetState)
            {
                return true;
            }

            _isInternalChange = true;
            try
            {
                _logger.Info($"Cambiando estado de Bluetooth a: {targetState}...");
                var result = await _radio.SetStateAsync(targetState);
                if (result == RadioAccessStatus.Allowed)
                {
                    _logger.Info($"Estado de Bluetooth cambiado exitosamente a: {targetState}");
                    return true;
                }
                else
                {
                    _logger.Warn($"Fallo al cambiar estado de Bluetooth: {result}");
                    return false;
                }
            }
            finally
            {
                // Dejamos un breve margen antes de rearmar la detección de eventos para evitar ecos
                await Task.Delay(200, cancellationToken);
                _isInternalChange = false;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Excepción cambiando estado del radio a {state}.", ex);
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static BluetoothRadioState ConvertToCoreState(RadioState state) => state switch
    {
        RadioState.On => BluetoothRadioState.On,
        RadioState.Off => BluetoothRadioState.Off,
        RadioState.Disabled => BluetoothRadioState.Disabled,
        _ => BluetoothRadioState.Unknown
    };

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_radio != null)
        {
            _radio.StateChanged -= OnRadioStateChanged;
            _radio = null;
        }
        _lock.Dispose();
    }
}
