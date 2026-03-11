using System.Collections.Generic;
using System.Windows;

namespace HCB.RevitAddin.Features.DuctFittingNumbering.UI;

public partial class DuctFittingNumberingWindow : Window
{
    public DuctFittingNumberingWindow(IReadOnlyList<string> availableTargetParameters, IReadOnlyList<string> availableLengthParameters)
    {
        InitializeComponent();
        TargetParameterComboBox.ItemsSource = availableTargetParameters;
        TargetParameterComboBox.SelectedIndex = availableTargetParameters.Count > 0 ? 0 : -1;
        LengthParameterComboBox.ItemsSource = availableLengthParameters;
        LengthParameterComboBox.SelectedIndex = availableLengthParameters.Count > 0 ? 0 : -1;
        FooterBar.StatusText = "Wybierz parametr docelowy i parametr dlugosci do grupowania kanalow.";
    }

    public string SelectedTargetParameter => TargetParameterComboBox.SelectedItem as string ?? string.Empty;

    public string SelectedLengthParameter => LengthParameterComboBox.SelectedItem as string ?? string.Empty;

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedTargetParameter))
        {
            FooterBar.StatusText = "Wybierz parametr docelowy.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedLengthParameter))
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
