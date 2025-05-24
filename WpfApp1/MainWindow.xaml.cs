using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using Path = System.Windows.Shapes.Path;
using WinForms = System.Windows.Forms;


namespace WpfApp1;

public partial class MainWindow : Window
{
    private CancellationTokenSource _cts;
    private ManualResetEventSlim _pauseEvent = new(true);
    private long _totalBytes;
    private long _copiedBytes;
    private DateTime _startTime;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            sourcePathTextBox.Text = openFileDialog.FileName;
        }
    }

    private void BrowseDestination_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Оберіть цільову папку",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            destinationPathTextBox.Text = dialog.FolderName;
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;

        var fileInfo = new FileInfo(sourcePathTextBox.Text);
        string destinationFile = System.IO.Path.Combine(destinationPathTextBox.Text, fileInfo.Name);

        if (File.Exists(destinationFile) && !ConfirmOverwrite())
        {
            return;
        }

        InitializeCopyProcess(fileInfo);

        try
        {
            int threadCount = GetSelectedThreadCount();
            await CopyFileWithProgressAsync(sourcePathTextBox.Text, destinationFile, threadCount, _cts.Token);
            LogMessage("Копіювання завершено успішно!");
        }
        catch (OperationCanceledException)
        {
            LogMessage("Копіювання скасовано користувачем");
            TryDeleteIncompleteFile(destinationFile);
        }
        catch (Exception ex)
        {
            LogMessage($"Помилка: {ex.Message}");
            TryDeleteIncompleteFile(destinationFile);
        }
        finally
        {
            CompleteCopyProcess();
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(sourcePathTextBox.Text))
        {
            MessageBox.Show("Будь ласка, оберіть вихідний файл");
            return false;
        }

        if (string.IsNullOrWhiteSpace(destinationPathTextBox.Text))
        {
            MessageBox.Show("Будь ласка, оберіть цільову папку");
            return false;
        }

        if (!File.Exists(sourcePathTextBox.Text))
        {
            MessageBox.Show("Вихідний файл не існує");
            return false;
        }

        return true;
    }

    private bool ConfirmOverwrite()
    {
        return MessageBox.Show("Файл в цільовій папці вже існує. Перезаписати?",
            "Попередження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
    }

    private void InitializeCopyProcess(FileInfo fileInfo)
    {
        _cts = new CancellationTokenSource();
        _pauseEvent.Set(); // не на паузі
        _totalBytes = fileInfo.Length;
        _copiedBytes = 0;
        _startTime = DateTime.Now;

        pauseButton.IsEnabled = true;
        resumeButton.IsEnabled = false;
        stopButton.IsEnabled = true;
        cancelButton.IsEnabled = true;
        startButton.IsEnabled = false;
        browseDestinationButton.IsEnabled = false;

        progressBar.Value = 0;
        progressText.Text = "0%";
        speedText.Text = string.Empty;

        LogMessage($"Початок копіювання {fileInfo.Name} ({FormatFileSize(_totalBytes)})...");
    }

    private int GetSelectedThreadCount()
    {
        return int.Parse(((System.Windows.Controls.ComboBoxItem)threadCountComboBox.SelectedItem).Content.ToString());
    }

    private async Task CopyFileWithProgressAsync(string sourcePath, string destinationPath, int threadCount, CancellationToken token)
    {
        const int bufferSize = 81920;
        var buffer = new byte[bufferSize];

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true);
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true);

        int bytesRead;
        while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
        {
            token.ThrowIfCancellationRequested();

            _pauseEvent.Wait(token); // Ось тут пауза

            await destinationStream.WriteAsync(buffer, 0, bytesRead, token);
            Interlocked.Add(ref _copiedBytes, bytesRead);

            UpdateProgressUI();
        }
    }

    private void UpdateProgressUI()
    {
        Dispatcher.Invoke(() =>
        {
            double progress = (double)_copiedBytes / _totalBytes * 100;
            progressBar.Value = progress;
            progressText.Text = $"{progress:F1}%";

            var elapsed = DateTime.Now - _startTime;
            var bytesPerSecond = _copiedBytes / elapsed.TotalSeconds;
            speedText.Text = $"{FormatFileSize(bytesPerSecond)}/сек";
        });
    }

    private void LogMessage(string message)
    {
        Dispatcher.Invoke(() =>
        {
            logTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
            logTextBox.ScrollToEnd();
        });
    }

    private void TryDeleteIncompleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void CompleteCopyProcess()
    {
        _cts?.Dispose();
        _cts = null;

        pauseButton.IsEnabled = false;
        resumeButton.IsEnabled = false;
        stopButton.IsEnabled = false;
        cancelButton.IsEnabled = false;
        startButton.IsEnabled = true;
        browseDestinationButton.IsEnabled = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pauseEvent.IsSet)
        {
            _pauseEvent.Reset(); // Поставити на паузу
            pauseButton.IsEnabled = false;
            resumeButton.IsEnabled = true;
            LogMessage("Копіювання призупинено");
        }
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_pauseEvent.IsSet)
        {
            _pauseEvent.Set(); // Продовжити копіювання
            pauseButton.IsEnabled = true;
            resumeButton.IsEnabled = false;
            LogMessage("Копіювання відновлено");
        }
    }


    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        _pauseEvent.Set();

        LogMessage("Копіювання зупинено");

        CompleteCopyProcess();
    }

    private string FormatFileSize(double bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (bytes >= 1024 && order < sizes.Length - 1)
        {
            order++;
            bytes /= 1024;
        }
        return $"{bytes:0.##} {sizes[order]}";
    }
}