using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WPF.BazhenovAI;
using WPF.Helpers;
using WPF.Models;
using WPF.Services;

namespace WPF
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<Log> _logs = new ObservableCollection<Log>();
        private LogFileWatcher _logFileWatcher;
        private ICollectionView _logsView;

        private DateTime? _dateFrom;
        private DateTime? _dateTo;
        private string _searchQuery = "";

        private string _selectedStatus = "Все";

        public ObservableCollection<LogFilterOption> MessageFilterOptions { get; set; } = new ObservableCollection<LogFilterOption>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _logsView = CollectionViewSource.GetDefaultView(_logs);
            _logsView.Filter = LogFilter;
            LogsListView.ItemsSource = _logsView;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            string binDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Directory.GetParent(binDir).Parent.Parent.Parent.FullName;
            string logsFolder = System.IO.Path.Combine(projectDir, "Logs");
            string archiveFilePath = System.IO.Path.Combine(projectDir, "Logs\\AllLogs.log");
            _logFileWatcher = new LogFileWatcher(logsFolder, archiveFilePath);
            _logFileWatcher.NewLogReceived += LogFileWatcher_NewLogReceived;
            _logFileWatcher.StartWatching();

            LoadArchivedLogs(archiveFilePath);
            _logsView.Refresh();

        }

        private void LogFileWatcher_NewLogReceived(object sender, Log log)
        {
            Dispatcher.Invoke(() =>
            {
                _logs.Add(log);
                AddMessageFilterOption(log.Message);
                _logsView.Refresh();
            });
        }

        private void LoadArchivedLogs(string archiveFilePath)
        {
            if (File.Exists(archiveFilePath))
            {
                var lines = File.ReadAllLines(archiveFilePath);
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
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime date))
                    {
                        Console.WriteLine("Adding log");
                        _logs.Add(new Log { Date = date, Status = status, Message = message });
                        AddMessageFilterOption(message);
                    }
                }
            }
            else
            {
                MessageBox.Show("File dont exist");
            }
        }

        private void AddMessageFilterOption(string message)
        {
            if (!MessageFilterOptions.Any(x => x.Message == message))
            {
                MessageFilterOptions.Add(new LogFilterOption { Message = message, IsSelected = true });
            }
        }

        private bool LogFilter(object item)
        {
            if (item is Log log)
            {
                DateTime logTruncated = new DateTime(log.Date.Year, log.Date.Month, log.Date.Day, log.Date.Hour, log.Date.Minute, 0);

                if (_dateFrom.HasValue)
                {
                    DateTime fromTruncated = new DateTime(_dateFrom.Value.Year, _dateFrom.Value.Month, _dateFrom.Value.Day,
                                                           _dateFrom.Value.Hour, _dateFrom.Value.Minute, 0);
                    if (logTruncated < fromTruncated)
                        return false;
                }
                if (_dateTo.HasValue)
                {
                    DateTime toTruncated = new DateTime(_dateTo.Value.Year, _dateTo.Value.Month, _dateTo.Value.Day,
                                                         _dateTo.Value.Hour, _dateTo.Value.Minute, 0);
                    if (logTruncated > toTruncated)
                        return false;
                }

                if (_selectedStatus != "Все" && !log.Status.Equals(_selectedStatus, StringComparison.OrdinalIgnoreCase))
                    return false;

                var selectedMessages = MessageFilterOptions.Where(x => x.IsSelected).Select(x => x.Message).ToList();
                if (selectedMessages.Any() && !selectedMessages.Contains(log.Message))
                    return false;

                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    int distance = LevenshteinDistance(log.Message, _searchQuery);
                    if (distance > 35)
                        return false;
                }
                return true;
            }
            return false;
        }


        private int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target))
                return source.Length;

            int[,] matrix = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[source.Length, target.Length];
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchTextBox.Text.Trim();

            if (_logsView is ListCollectionView listView)
            {
                if (!string.IsNullOrEmpty(_searchQuery))
                {
                    listView.CustomSort = new LevenshteinComparer(_searchQuery);
                }
                else
                {
                    listView.CustomSort = null;
                }
            }
            _logsView.Refresh();
        }

        private void DateFromPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDateTimeFrom();
        }

        private void DateToPicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDateTimeTo();
        }

        private void TimeFromTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateDateTimeFrom();
        }

        private void TimeToTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateDateTimeTo();
        }

        private void MessageFilterCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            _logsView.Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnClosed(EventArgs e)
        {
            _logFileWatcher.StopWatching();
            base.OnClosed(e);
        }

        private void UpdateDateTimeFrom()
        {
            if (DateFromPicker.SelectedDate.HasValue && TimeSpan.TryParse(TimeFromTextBox.Text, out var time))
            {
                _dateFrom = DateFromPicker.SelectedDate.Value.Date + time;
            }
            else
            {
                _dateFrom = null;
            }
            _logsView.Refresh();
        }

        private void UpdateDateTimeTo()
        {
            if (DateToPicker.SelectedDate.HasValue && TimeSpan.TryParse(TimeToTextBox.Text, out var time))
            {
                _dateTo = DateToPicker.SelectedDate.Value.Date + time;
            }
            else
            {
                _dateTo = null;
            }
            _logsView.Refresh();
        }

        private void TimeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"[0-9:]");
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedStatus = selectedItem.Content.ToString();
                _logsView?.Refresh();
            }
        }


        private readonly HttpClient _httpClient;
        private const string LogFileNamePrefix = "Logs";
        private const string LogFileExtension = ".log";
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ((Button)sender).IsEnabled = false;
                progressBar.IsIndeterminate = true;
                statusText.Text = "Starting download...";

                string apiUrl = "https://api.example.com/logs/download";

                HttpResponseMessage response = await _httpClient.GetAsync(
                    apiUrl, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                string fileName = response.Content.Headers.ContentDisposition?.FileName
                                 ?? GenerateLogFileName();

                string binDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Directory.GetParent(binDir).Parent.Parent.Parent.FullName;
                string logsFolder = System.IO.Path.Combine(projectDir, "Logs");

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = $"Log files (*{LogFileExtension})|*{LogFileExtension}|All files (*.*)|*.*",
                    DefaultExt = LogFileExtension,
                    InitialDirectory = logsFolder
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    statusText.Text = "Downloading...";

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = File.Create(saveFileDialog.FileName))
                    {
                        await contentStream.CopyToAsync(fileStream);
                    }

                    statusText.Text = $"Log file saved: {Path.GetFileName(saveFileDialog.FileName)}";
                    progressBar.Value = 100;
                }
            }
            catch (HttpRequestException ex)
            {
                statusText.Text = $"HTTP Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                statusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                progressBar.IsIndeterminate = false;
                ((Button)sender).IsEnabled = true;
            }
        }

        private string GenerateLogFileName()
        {
            string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH.mm");
            return $"{LogFileNamePrefix}{timestamp}{LogFileExtension}";
        }


        public ObservableCollection<AnomalyResult> AnomalyResults { get; set; } = new ObservableCollection<AnomalyResult>();


        private void RunAnomalyDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            var aggregatedLogs = GetAggregatedLogs();

            var detector = new AnomalyDetector(aggregatedLogs);

            var predictions = detector.Predict(aggregatedLogs).ToList();

            AnomalyResults.Clear();

            foreach (var prediction in predictions)
            {
                AnomalyResults.Add(new AnomalyResult
                {
                    Time = prediction.log.Time,
                    ErrorCount = prediction.log.ErrorCount,
                    IsAnomaly = prediction.isAnomaly,
                    Score = prediction.score
                });
            }

            if (AnomalyResults.Any(r => r.IsAnomaly))
            {
                MessageBox.Show("Обнаружены аномалии в логах! Проверьте систему.");
            }
            else
            {
                MessageBox.Show("Все в порядке, аномалий не обнаружено.");
            }
        }


        private IEnumerable<LogData> GetAggregatedLogs()
        {
            var aggregated = _logs.GroupBy(l =>
            {
                int minuteGroup = (l.Date.Minute / 5) * 5;
                return new DateTime(l.Date.Year, l.Date.Month, l.Date.Day, l.Date.Hour, minuteGroup, 0);
            })
            .Select(g => new LogData
            {
                Time = g.Key,
                ErrorCount = g.Sum(l => l.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ? 1f : 0f)
            })
            .OrderBy(x => x.Time);

            return aggregated;
        }

    }
}
