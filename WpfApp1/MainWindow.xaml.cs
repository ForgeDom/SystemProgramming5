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

        if (!int.TryParse(numberTextBox.Text, out int number) || number < 0)
        {
            progressBar.Visibility = Visibility.Collapsed;
            MessageBox.Show("Введіть коректне невід’ємне ціле число.");
            return;
        }

        try
        {
            BigInteger result = await Task.Run(() => CalculateFactorial(number));
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

    private BigInteger CalculateFactorial(int n)
    {
        BigInteger result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }
}