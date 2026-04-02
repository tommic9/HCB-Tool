using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HCB.RevitAddin.Features.AccessoryTerminalNumbering.Models;

namespace HCB.RevitAddin.Features.AccessoryTerminalNumbering.UI;

public partial class AccessoryTerminalNumberingWindow : Window
{
    private const string DefaultTargetParameterName = "LIN_POSITION_NUMBER_A";

    public AccessoryTerminalNumberingWindow(
        IReadOnlyList<string> availableTargetParameters,
        IReadOnlyList<string> availableAccessoryTypeParameters,
        bool hasDuctAccessories,
        bool hasPipeAccessories,
        bool hasAirTerminals)
    {
        InitializeComponent();
        TargetParameterComboBox.ItemsSource = availableTargetParameters;
        TargetParameterComboBox.SelectedItem = availableTargetParameters.Contains(DefaultTargetParameterName)
            ? DefaultTargetParameterName
            : availableTargetParameters.Count > 0 ? availableTargetParameters[0] : null;

        List<string> accessoryTypeOptions = ["(brak)"];
        accessoryTypeOptions.AddRange(availableAccessoryTypeParameters);
        AccessoryTypeParameterComboBox.ItemsSource = accessoryTypeOptions;
        AccessoryTypeParameterComboBox.SelectedItem = accessoryTypeOptions.Contains("Type Mark")
            ? "Type Mark"
            : accessoryTypeOptions[0];

        DuctAccessoryPrefixPanel.Visibility = hasDuctAccessories ? Visibility.Visible : Visibility.Collapsed;
        PipeAccessoryPrefixPanel.Visibility = hasPipeAccessories ? Visibility.Visible : Visibility.Collapsed;
        TerminalPrefixPanel.Visibility = hasAirTerminals ? Visibility.Visible : Visibility.Collapsed;
        AccessoryTypeParameterPanel.Visibility = hasDuctAccessories || hasPipeAccessories ? Visibility.Visible : Visibility.Collapsed;

        FooterBar.StatusText = "Wybierz parametr docelowy i ustawienia numeracji.";
        UpdatePreview();
    }

    public AccessoryTerminalNumberingOptions Options => new()
    {
        TargetParameterName = GetSelectedString(TargetParameterComboBox),
        StartNumber = int.TryParse(GetText(StartNumberTextBox), out int value) ? value : 0,
        UseLastProjectNumber = UseLastProjectNumberCheckBox?.IsChecked == true,
        DuctAccessoryPrefix = GetText(DuctAccessoryPrefixTextBox),
        PipeAccessoryPrefix = GetText(PipeAccessoryPrefixTextBox),
        TerminalPrefix = GetText(TerminalPrefixTextBox),
        AccessoryTypeParameterName = NormalizeAccessoryTypeParameterName(GetSelectedString(AccessoryTypeParameterComboBox))
    };

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Options.TargetParameterName))
        {
            FooterBar.StatusText = "Wybierz parametr docelowy.";
            return;
        }

        if (!Options.UseLastProjectNumber && Options.StartNumber <= 0)
        {
            FooterBar.StatusText = "Podaj dodatni numer startowy albo zaznacz pobieranie ostatniego numeru z projektu.";
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

    private void OptionsChanged(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
    }

    private void OptionsChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePreview();
    }

    private static string NormalizeAccessoryTypeParameterName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "(brak)")
        {
            return string.Empty;
        }

        return value.Trim();
    }

    private void UpdatePreview()
    {
        if (PreviewTextBlock == null ||
            DuctAccessoryPrefixPanel == null ||
            PipeAccessoryPrefixPanel == null ||
            TerminalPrefixPanel == null)
        {
            return;
        }

        AccessoryTerminalNumberingOptions options = Options;
        string accessoryTypePart = string.IsNullOrWhiteSpace(options.AccessoryTypeParameterName)
            ? string.Empty
            : $".{options.AccessoryTypeParameterName}";

        List<string> lines = [];
        if (DuctAccessoryPrefixPanel.Visibility == Visibility.Visible)
        {
            lines.Add($"duct accessory: SYS{FormatOptionalPart(options.DuctAccessoryPrefix)}{accessoryTypePart}.{GetPreviewNumber()}");
        }

        if (PipeAccessoryPrefixPanel.Visibility == Visibility.Visible)
        {
            lines.Add($"pipe accessory: SYS{FormatOptionalPart(options.PipeAccessoryPrefix)}{accessoryTypePart}.{GetPreviewNumber()}");
        }

        if (TerminalPrefixPanel.Visibility == Visibility.Visible)
        {
            string terminalPrefix = string.IsNullOrWhiteSpace(options.TerminalPrefix) ? "AT" : options.TerminalPrefix;
            lines.Add($"air terminal: {terminalPrefix}.{GetPreviewNumber()}");
        }

        PreviewTextBlock.Text = lines.Count == 0
            ? "brak aktywnych kategorii w biezacej selekcji"
            : string.Join("\n", lines);
    }

    private static string GetText(TextBox? textBox)
    {
        return textBox?.Text?.Trim() ?? string.Empty;
    }

    private static string GetSelectedString(ComboBox? comboBox)
    {
        return comboBox?.SelectedItem as string ?? string.Empty;
    }

    private string GetPreviewNumber()
    {
        if (Options.UseLastProjectNumber)
        {
            return "ostatni+1";
        }

        return Options.StartNumber > 0 ? Options.StartNumber.ToString() : "1";
    }

    private static string FormatOptionalPart(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : $".{value}";
    }
}
