using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HCB.RevitAddin.Features.FlowChanger.UI;

public partial class FlowChangerOptionsWindow : Window
{
    public FlowChangerOptionsWindow(IReadOnlyList<string> parameterNames)
    {
        InitializeComponent();

        FooterBar.StatusText = "Wybierz parametr, do ktorego ma trafic nowy przeplyw.";
        ParameterComboBox.ItemsSource = parameterNames;
        ParameterComboBox.SelectedItem = parameterNames.FirstOrDefault(name => name == "Actual Flow");
        ParameterComboBox.SelectedIndex = ParameterComboBox.SelectedIndex >= 0 ? ParameterComboBox.SelectedIndex : 0;
    }

    public string SelectedParameterName => ParameterComboBox.SelectedItem?.ToString() ?? string.Empty;

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedParameterName))
        {
            FooterBar.StatusText = "Wybierz parametr docelowy.";
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
