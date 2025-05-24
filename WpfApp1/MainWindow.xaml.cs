using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
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
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        resultTextBlock.Text = "Розрахунок...";
        progressBar.Visibility = Visibility.Visible;

        if (!double.TryParse(baseTextBox.Text, out double baseNumber))
        {
            MessageBox.Show("Некоректне число.");
            progressBar.Visibility = Visibility.Collapsed;
            return;
        }

        if (!int.TryParse(exponentTextBox.Text, out int exponent))
        {
            MessageBox.Show("Некоректний степінь (має бути цілим числом).");
            progressBar.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            double result = await Task.Run(() => Math.Pow(baseNumber, exponent));
            resultTextBlock.Text = $"Результат: {result}";
        }
        catch (Exception ex)
        {
            resultTextBlock.Text = $"Помилка: {ex.Message}";
        }
        finally
        {
            progressBar.Visibility = Visibility.Collapsed;
        }
    }
}