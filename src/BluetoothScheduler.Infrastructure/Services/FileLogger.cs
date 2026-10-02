using BluetoothScheduler.Core.Interfaces;

namespace BluetoothScheduler.Infrastructure.Services;

public class FileLogger : ILogger
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    public FileLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, "app.log");
    }

    public void Info(string message) => Log("INFO", message);
    public void Warn(string message) => Log("WARN", message);
    public void Error(string message, Exception? ex = null)
    {
        string fullMsg = ex != null ? $"{message} | Ex: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : message;
        Log("ERROR", fullMsg);
    }

    private void Log(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                RotateIfNeeded();
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, line);
            }
            catch
            {
                // Silently swallow logging errors to prevent crashes
            }
        }
    }

    private void RotateIfNeeded()
    {
        if (File.Exists(_logFilePath))
        {
            var info = new FileInfo(_logFilePath);
            if (info.Length > MaxFileSizeBytes)
            {
                string oldLog = Path.Combine(Path.GetDirectoryName(_logFilePath)!, "app.old.log");
                if (File.Exists(oldLog))
                {
                    File.Delete(oldLog);
                }
                File.Move(_logFilePath, oldLog);
            }
        }
    }
}
