using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using HCB.RevitAddin.Features.Levels.Models;

namespace HCB.RevitAddin.Features.Levels.UI;

public partial class LevelsOptionsWindow : Window
{
    public LevelsOptionsWindow()
    {
        InitializeComponent();
        FooterBar.StatusText = "Ustaw format dopisku rzędnej i zatwierdz zmiany.";
        DecimalPlacesComboBox.SelectionChanged += OptionsChanged;
        UpdatePreview();
    }

    public LevelsRenameOptions Options => new()
    {
        DecimalPlaces = GetSelectedDecimalPlaces(),
        ShowPlusForPositiveValues = ShowPlusCheckBox.IsChecked == true
    };

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OptionsChanged(object? sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void OptionsChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        int decimalPlaces = GetSelectedDecimalPlaces();
        double sampleValue = 3.2d;
        string format = "0" + (decimalPlaces > 0 ? "." + new string('0', decimalPlaces) : string.Empty);
        string number = sampleValue.ToString(format, CultureInfo.InvariantCulture);

        if (ShowPlusCheckBox.IsChecked == true && sampleValue > 0)
        {
            number = "+" + number;
        }

        PreviewTextBlock.Text = $"Poziom 1 ({number})";
    }

    private int GetSelectedDecimalPlaces()
    {
        if (DecimalPlacesComboBox.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Content?.ToString(), out int value))
        {
            return value;
        }

        return 2;
    }
}
