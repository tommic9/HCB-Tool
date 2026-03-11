using System.Windows;
using HCB.RevitAddin.Features.RenameFamilyContent.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.Features.RenameFamilyContent.UI;

public partial class RenameFamilyContentWindow : Window
{
    public RenameFamilyContentWindow()
    {
        InitializeComponent();
        FooterBar.StatusText = "Reguly beda zastosowane jednoczesnie do parametrow rodzinnych i typow.";
        UpdateOutputFolderState();
    }

    public RenameFamilyContentOptions Options => new()
    {
        Prefix = PrefixTextBox.Text,
        Find = FindTextBox.Text,
        Replace = ReplaceTextBox.Text,
        Suffix = SuffixTextBox.Text,
        SaveAsCopy = SaveAsCopyCheckBox.IsChecked == true,
        OutputFolderPath = OutputFolderTextBox.Text
    };

    private void BrowseOutputFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        WithoutOpenDialogService dialogService = new();
        string? folderPath = dialogService.PickFolderPath("Wybierz folder docelowy dla kopii rodzin po zmianie nazw.");
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            OutputFolderTextBox.Text = folderPath;
        }
    }

    private void SaveAsCopyCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        UpdateOutputFolderState();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SaveAsCopyCheckBox.IsChecked == true && string.IsNullOrWhiteSpace(OutputFolderTextBox.Text))
        {
            FooterBar.StatusText = "Wybierz folder docelowy dla kopii rodzin.";
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

    private void UpdateOutputFolderState()
    {
        if (OutputFolderTextBox == null || SaveAsCopyCheckBox == null)
        {
            return;
        }

        OutputFolderTextBox.IsEnabled = SaveAsCopyCheckBox.IsChecked == true;
    }
}
