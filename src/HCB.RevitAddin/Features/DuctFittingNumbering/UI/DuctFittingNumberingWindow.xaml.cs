using System.Collections.Generic;
using System.Windows;

namespace HCB.RevitAddin.Features.DuctFittingNumbering.UI;

public partial class DuctFittingNumberingWindow : Window
{
    private const string DefaultTargetParameterName = "LIN_POSITION_NUMBER_A";
    private readonly bool _requiresLengthParameter;

    public DuctFittingNumberingWindow(
        IReadOnlyList<string> availableTargetParameters,
        IReadOnlyList<string> availableLengthParameters,
        string windowTitle,
        string heading,
        bool requiresLengthParameter)
    {
        InitializeComponent();

        _requiresLengthParameter = requiresLengthParameter;
        Title = windowTitle;
        WindowTitleTextBlock.Text = heading;
        OptionsGroupBox.Header = requiresLengthParameter ? "Opcje numeracji" : "Parametr docelowy";

        TargetParameterComboBox.ItemsSource = availableTargetParameters;
        TargetParameterComboBox.SelectedItem = availableTargetParameters.Contains(DefaultTargetParameterName)
            ? DefaultTargetParameterName
            : availableTargetParameters.Count > 0 ? availableTargetParameters[0] : null;

        LengthParameterComboBox.ItemsSource = availableLengthParameters;
        LengthParameterComboBox.SelectedIndex = availableLengthParameters.Count > 0 ? 0 : -1;
        IncludeSystemParameterCheckBox.IsChecked = false;

        if (!requiresLengthParameter)
        {
            LengthParameterLabel.Visibility = Visibility.Collapsed;
            LengthParameterComboBox.Visibility = Visibility.Collapsed;
            LengthParameterSpacerRow.Height = new GridLength(0);
            LengthParameterRow.Height = new GridLength(0);
            FooterBar.StatusText = "Wybierz parametr docelowy. Numeracja grupuje ksztaltki i akcesoria po parametrach LIN.";
            return;
        }

        FooterBar.StatusText = "Wybierz parametr docelowy, parametr dlugosci dla kanalow i opcjonalnie dolacz HC_System.";
    }

    public string SelectedTargetParameter => TargetParameterComboBox.SelectedItem as string ?? string.Empty;

    public string SelectedLengthParameter => _requiresLengthParameter
        ? LengthParameterComboBox.SelectedItem as string ?? string.Empty
        : string.Empty;

    public bool IncludeSystemParameter => IncludeSystemParameterCheckBox.IsChecked == true;

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedTargetParameter))
        {
            FooterBar.StatusText = "Wybierz parametr docelowy.";
            return;
        }

        if (_requiresLengthParameter && string.IsNullOrWhiteSpace(SelectedLengthParameter))
        {
            FooterBar.StatusText = "Wybierz parametr dlugosci.";
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
