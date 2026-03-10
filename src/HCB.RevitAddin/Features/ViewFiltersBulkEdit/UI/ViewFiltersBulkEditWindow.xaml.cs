using System.Windows;
using System.Windows.Controls;
using HCB.RevitAddin.Features.ViewFiltersBulkEdit.Models;

namespace HCB.RevitAddin.Features.ViewFiltersBulkEdit.UI;

public partial class ViewFiltersBulkEditWindow : Window
{
    public ViewFiltersBulkEditWindow()
    {
        InitializeComponent();
        FooterBar.StatusText = "Wybierz akcje, ktore maja zostac wykonane dla zaznaczonych filtrow.";
    }

    public ViewFiltersBulkEditOptions Options => new()
    {
        EnableMode = (EnableModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "NoChange",
        VisibilityMode = (VisibilityModeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "NoChange"
    };

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Options.EnableMode == "NoChange" && Options.VisibilityMode == "NoChange")
        {
            FooterBar.StatusText = "Wybierz co najmniej jedna akcje.";
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
