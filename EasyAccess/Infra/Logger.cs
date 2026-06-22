using global::System;
using global::System.IO;
using global::System.Text;

namespace EasyAccess.Infra
{
    internal enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    internal sealed class Logger : IDisposable
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minLevel;
        private readonly object _lock = new();
        private StreamWriter? _writer;
        private string? _currentLogFile;
        private bool _disposed;

        public Logger(string logDirectory, LogLevel minLevel = LogLevel.Info)
        {
            _logDirectory = logDirectory;
            _minLevel = minLevel;
            Directory.CreateDirectory(_logDirectory);
            RotateIfNeeded();
        }

        public void Debug(string message) => Log(LogLevel.Debug, message);
        public void Info(string message) => Log(LogLevel.Info, message);
        public void Warn(string message) => Log(LogLevel.Warn, message);
        public void Error(string message) => Log(LogLevel.Error, message);
        public void Error(string message, Exception ex) => Log(LogLevel.Error, $"{message}: {ex}");

        private void Log(LogLevel level, string message)
        {
            if (level < _minLevel || _disposed)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var line = $"[{timestamp}] [{level,-5}] {message}";

            lock (_lock)
            {
                try
                {
                    RotateIfNeeded();
                    _writer?.WriteLine(line);
                    _writer?.Flush();
                }
                catch
                {
                    // Silently ignore logging failures
                }
            }
        }

        private void RotateIfNeeded()
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            var logFile = Path.Combine(_logDirectory, $"EasyAccess_{today}.log");

            if (_currentLogFile == logFile && _writer != null)
                return;

            _writer?.Dispose();

            try
            {
                var files = Directory.GetFiles(_logDirectory, "EasyAccess_*.log");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < DateTime.Now.AddDays(-7))
                        fileInfo.Delete();
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            _currentLogFile = logFile;
            _writer = new StreamWriter(logFile, append: true, encoding: Encoding.UTF8)
            {
                AutoFlush = false
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            lock (_lock)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }
}
