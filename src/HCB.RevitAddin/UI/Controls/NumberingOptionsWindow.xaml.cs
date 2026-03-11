using System.Collections.Generic;
using System.Windows;

namespace HCB.RevitAddin.UI.Controls;

public partial class NumberingOptionsWindow : Window
{
    public NumberingOptionsWindow(string title, IReadOnlyList<string> parameterNames)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        ParameterComboBox.ItemsSource = parameterNames;
        ParameterComboBox.SelectedIndex = parameterNames.Count > 0 ? 0 : -1;
        FooterBar.StatusText = $"Dostepne parametry tekstowe: {parameterNames.Count}";
        Loaded += (_, _) => StartNumberTextBox.Focus();
    }

    public string SelectedParameterName => ParameterComboBox.SelectedItem as string ?? string.Empty;

    public int StartNumber => int.TryParse(StartNumberTextBox.Text, out int value) ? value : 0;

    public string Prefix => PrefixTextBox.Text ?? string.Empty;

    public string Suffix => SuffixTextBox.Text ?? string.Empty;

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedParameterName))
        {
            FooterBar.StatusText = "Wybierz parametr docelowy.";
            return;
        }

        if (StartNumber <= 0)
        {
            FooterBar.StatusText = "Podaj dodatni numer startowy.";
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
