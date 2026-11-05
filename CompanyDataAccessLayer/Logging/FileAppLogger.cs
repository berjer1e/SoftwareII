/*
 * Файл: FileAppLogger.cs
 * Призначення: Потокобезпечна реалізація інтерфейсу IAppLogger для запису подій і помилок у текстовий лог-файл.
 */

using System;
using System.IO;

namespace CompanyDataAccessLayer.Logging
{
    public class FileAppLogger : IAppLogger
    {
        public static FileAppLogger Instance { get; } = new();

        private readonly string _logFilePath;
        private readonly object _syncRoot = new();

        /*
         * Ініціалізує файловий логер із вказаним шляхом до файлу або стандартним шляхом у папці logs.
         */
        public FileAppLogger(string? logFilePath = null)
        {
            _logFilePath = string.IsNullOrWhiteSpace(logFilePath)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "app.log")
                : logFilePath;
        }

        /*
         * Реєструє інформаційне повідомлення з рівнем INFO.
         */
        public void LogInfo(string message) => WriteEntry("INFO", message);

        /*
         * Реєструє попереджувальне повідомлення з рівнем WARN.
         */
        public void LogWarning(string message) => WriteEntry("WARN", message);

        /*
         * Форматує інформацію про виняток (включно з внутрішнім винятком) і записує її з рівнем ERROR.
         */
        public void LogError(Exception exception, string contextMessage)
        {
            var details = exception.InnerException != null
                ? $"{contextMessage} | {exception.GetType().Name}: {exception.Message} | Inner: {exception.InnerException.Message}"
                : $"{contextMessage} | {exception.GetType().Name}: {exception.Message}";

            WriteEntry("ERROR", details);
        }

        /*
         * Синхронізовано дописує сформований рядок журналу з поточною міткою часу у цільовий файл на диску.
         */
        private void WriteEntry(string level, string message)
        {
            try
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                lock (_syncRoot)
                {
                    var dir = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.AppendAllText(_logFilePath, line);
                }
            }
            catch
            {
            }
        }
    }
}
