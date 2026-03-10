using System.Windows;

namespace HCB.RevitAddin.Features.ColorVentSystems.UI;

public partial class ColorSetSystemsOptionsWindow : Window
{
    public ColorSetSystemsOptionsWindow()
    {
        InitializeComponent();
        FooterBar.StatusText = "Ustaw dodatkowe nadpisania linii i zatwierdz.";
    }

    public bool OverrideDisplayLines => BlackLinesCheckBox.IsChecked == true;

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
}
