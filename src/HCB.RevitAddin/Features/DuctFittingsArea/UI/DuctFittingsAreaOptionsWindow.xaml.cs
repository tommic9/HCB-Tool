using System.Windows;
using HCB.RevitAddin.Features.DuctFittingsArea.Models;

namespace HCB.RevitAddin.Features.DuctFittingsArea.UI;

public partial class DuctFittingsAreaOptionsWindow : Window
{
    public DuctFittingsAreaOptionsWindow()
    {
        InitializeComponent();
    }

    public DuctFittingsAreaOptions Options { get; private set; } = new();

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        Options = new DuctFittingsAreaOptions
        {
            ClampValuesBelowOneToOne = ClampToOneRadioButton.IsChecked == true
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
