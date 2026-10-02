using System.Text.Json;
using System.Text.Json.Serialization;
using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Core.Models;

namespace BluetoothScheduler.Infrastructure.Persistence;

public class JsonStateStore : IStateStore
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonStateStore(string directoryPath, ILogger logger)
    {
        _logger = logger;
        Directory.CreateDirectory(directoryPath);
        _filePath = Path.Combine(directoryPath, "state.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<ScheduleState> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaultState = new ScheduleState();
                await SaveInternalAsync(defaultState, cancellationToken);
                return defaultState;
            }

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var state = JsonSerializer.Deserialize<ScheduleState>(json, _jsonOptions);
            return state ?? new ScheduleState();
        }
        catch (Exception ex)
        {
            _logger.Error("Error leyendo state.json. Usando valores por defecto.", ex);
            return new ScheduleState();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(ScheduleState state, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await SaveInternalAsync(state, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Error guardando state.json.", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveInternalAsync(ScheduleState state, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(state, _jsonOptions);
        string tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
