using System.Text.Json;
using System.Text.Json.Serialization;
using BluetoothScheduler.Core.Interfaces;
using BluetoothScheduler.Core.Models;

namespace BluetoothScheduler.Infrastructure.Persistence;

public class JsonConfigStore : IConfigStore
{
    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonConfigStore(string directoryPath, ILogger logger)
    {
        _logger = logger;
        Directory.CreateDirectory(directoryPath);
        _filePath = Path.Combine(directoryPath, "config.json");
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<ScheduleConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                var defaultConfig = new ScheduleConfig();
                await SaveInternalAsync(defaultConfig, cancellationToken);
                return defaultConfig;
            }

            string json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var config = JsonSerializer.Deserialize<ScheduleConfig>(json, _jsonOptions);
            return config ?? new ScheduleConfig();
        }
        catch (Exception ex)
        {
            _logger.Error("Error leyendo config.json. Usando valores por defecto.", ex);
            return new ScheduleConfig();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(ScheduleConfig config, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await SaveInternalAsync(config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("Error guardando config.json.", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveInternalAsync(ScheduleConfig config, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(config, _jsonOptions);
        string tempPath = _filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
