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

    private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        progressBar.Visibility = Visibility.Visible;

        string text = inputTextBox.Text;

        try
        {
            var result = await Task.Run(() => AnalyzeText(text));

            vowelsTextBlock.Text = $"Голосні: {result.VowelCount}";
            consonantsTextBlock.Text = $"Приголосні: {result.ConsonantCount}";
            symbolsTextBlock.Text = $"Символи: {result.TotalCharacters}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка: {ex.Message}");
        }
        finally
        {
            progressBar.Visibility = Visibility.Collapsed;
        }
    }

    private (int VowelCount, int ConsonantCount, int TotalCharacters) AnalyzeText(string text)
    {
        string vowels = "аеєиіїоуюяaeiouy";
        string consonants = "бвгґджзйклмнпрстфхцчшщbcdfghjklmnpqrstvwxz";

        int vowelCount = 0;
        int consonantCount = 0;
        int totalChars = text.Length;

        foreach (char c in text.ToLower())
        {
            if (vowels.Contains(c))
                vowelCount++;
            else if (consonants.Contains(c))
                consonantCount++;
        }

        return (vowelCount, consonantCount, totalChars);
    }
}