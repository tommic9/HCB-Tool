using System.Windows;
using System.Windows.Controls;
using HCB.RevitAddin.Features.ViewsDuplicate.Models;

namespace HCB.RevitAddin.Features.ViewsDuplicate.UI;

public partial class ViewsDuplicateWindow : Window
{
    public ViewsDuplicateWindow()
    {
        InitializeComponent();
        FooterBar.StatusText = "Wybierz liczbe kopii i tryb duplikacji.";
    }

    public ViewsDuplicateOptions Options => new()
    {
        CopiesCount = int.TryParse(CopiesCountTextBox.Text, out int value) ? value : 0,
        DuplicateMode = (DuplicateModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "WithDetailing"
    };

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CopiesCountTextBox.Text, out int copies) || copies <= 0)
        {
            FooterBar.StatusText = "Podaj dodatnia liczbe kopii.";
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
