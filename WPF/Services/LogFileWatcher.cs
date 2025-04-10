using System.Globalization;
using System.IO;
using WPF.Models;

namespace WPF.Services
{
    public class LogFileWatcher
    {
        private readonly string _logsFolder;
        private readonly string _archiveFilePath;
        private FileSystemWatcher _watcher;

        public event EventHandler<Log> NewLogReceived;

        public LogFileWatcher(string logsFolder, string archiveFilePath)
        {
            _logsFolder = logsFolder;
            _archiveFilePath = archiveFilePath;
        }

        public void StartWatching()
        {
            if (!Directory.Exists(_logsFolder))
                Directory.CreateDirectory(_logsFolder);

            _watcher = new FileSystemWatcher(_logsFolder)
            {
                Filter = "*.log",
                EnableRaisingEvents = true
            };
            _watcher.Created += OnFileCreated;
        }

        public void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private async void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(1000);

            try
            {
                var lines = File.ReadAllLines(e.FullPath);
                foreach (var line in lines)
                {
                    if (line.Length < 21)
                        continue;
            
                    string timePart = line.Substring(0, 19);
                    string rest = line.Substring(20);

                    var tokens = rest.Split(new char[] { ' ' }, 2);
                    if (tokens.Length < 2)
                        continue;

                    string status = tokens[0];
                    string message = tokens[1];

                    if (DateTime.TryParseExact(timePart, "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                    {
                        var log = new Log
                        {
                            Date = date,
                            Status = status,
                            Message = message
                        };

                        NewLogReceived?.Invoke(this, log);
                        AppendToArchive(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        private void AppendToArchive(string line)
        {
            try
            {
                File.AppendAllText(_archiveFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
    }
}
