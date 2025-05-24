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
    private long _totalFiles;
    private long _copiedFiles;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog { Description = "Оберіть початкову директорію" };
        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            sourceTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BrowseDestination_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new WinForms.FolderBrowserDialog { Description = "Оберіть директорію призначення" };
        if (dialog.ShowDialog() == WinForms.DialogResult.OK)
        {
            destinationTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        string sourceDir = sourceTextBox.Text;
        string destDir = destinationTextBox.Text;
        int threadCount = int.Parse(((ComboBoxItem)threadCountComboBox.SelectedItem).Content.ToString());

        if (!Directory.Exists(sourceDir))
        {
            MessageBox.Show("Початкова директорія не існує.");
            return;
        }

        Directory.CreateDirectory(destDir);

        var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        _totalFiles = allFiles.Length;
        _copiedFiles = 0;

        var queue = new ConcurrentQueue<string>(allFiles);
        var tasks = new Task[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                while (queue.TryDequeue(out string file))
                {
                    string relativePath = Path.GetRelativePath(sourceDir, file);
                    string destFile = Path.Combine(destDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    File.Copy(file, destFile, true);

                    Interlocked.Increment(ref _copiedFiles);
                    UpdateProgress();
                }
            });
        }

        await Task.WhenAll(tasks);
        MessageBox.Show("Копіювання завершено!");
    }

    private void UpdateProgress()
    {
        Dispatcher.Invoke(() =>
        { 
            double percent = (double)_copiedFiles / _totalFiles * 100;
            progressBar.Value = percent;
            progressText.Text = $"{percent:F1}%";
        });
    }
}