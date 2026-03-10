using System.IO;
using System.Windows;
using HCB.RevitAddin.Features.Estimate.Models;
using Microsoft.Win32;

namespace HCB.RevitAddin.Features.Estimate.UI;

public partial class EstimateOptionsWindow : Window
{
    public EstimateOptionsWindow(string initialCatalogPath)
    {
        InitializeComponent();
        CatalogPathTextBox.Text = initialCatalogPath;
        FooterBar.StatusText = "Wybierz plik CSV z cennikiem i opcje dodatkowe.";
        Loaded += (_, _) => CatalogPathTextBox.Focus();
    }

    public EstimateOptions Options { get; private set; } = new();

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Wybierz plik cennika (CSV)",
            Filter = "Pliki CSV (*.csv)|*.csv|Wszystkie pliki (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(CatalogPathTextBox.Text))
        {
            try
            {
                dialog.InitialDirectory = Path.GetDirectoryName(CatalogPathTextBox.Text);
            }
            catch
            {
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            CatalogPathTextBox.Text = dialog.FileName;
        }
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        string path = CatalogPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            FooterBar.StatusText = "Wybierz plik cennika CSV.";
            return;
        }

        if (!File.Exists(path))
        {
            FooterBar.StatusText = "Wskazany plik nie istnieje.";
            return;
        }

        Options = new EstimateOptions
        {
            CatalogPath = path,
            AddFoil = AddFoilCheckBox.IsChecked == true
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
