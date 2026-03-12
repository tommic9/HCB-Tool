using System.Linq;
using System.Windows;
using HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.UI;

public partial class BatchAddSharedFamilyParametersWindow : Window
{
    public BatchAddSharedFamilyParametersWindow(IReadOnlyList<SharedParameterDefinitionItem> definitions, IReadOnlyList<FamilyParameterGroupOption> groupOptions, string sharedParameterFilePath)
    {
        InitializeComponent();

        ParametersListBox.ItemsSource = definitions;
        ParametersListBox.SelectAll();
        GroupComboBox.ItemsSource = groupOptions;
        GroupComboBox.SelectedIndex = 0;
        SharedParameterFileTextBox.Text = sharedParameterFilePath;
        FooterBar.StatusText = $"Do wyboru: {definitions.Count} definicji.";
        UpdateOutputFolderState();
    }

    public BatchAddSharedFamilyParametersOptions Options => new()
    {
        SelectedParameterNames = ParametersListBox.SelectedItems.Cast<SharedParameterDefinitionItem>().Select(item => item.Name).ToList(),
        IsInstance = InstanceCheckBox.IsChecked == true,
        GroupKey = GroupComboBox.SelectedValue?.ToString() ?? string.Empty,
        SaveAsCopy = SaveAsCopyCheckBox.IsChecked == true,
        OutputFolderPath = OutputFolderTextBox.Text
    };

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        ParametersListBox.SelectAll();
    }

    private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
    {
        ParametersListBox.UnselectAll();
    }

    private void BrowseOutputFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        WithoutOpenDialogService dialogService = new();
        string? folderPath = dialogService.PickFolderPath("Wybierz folder docelowy dla kopii rodzin.");
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
        if (ParametersListBox.SelectedItems.Count == 0)
        {
            FooterBar.StatusText = "Wybierz co najmniej jedna definicje.";
            return;
        }

        if (GroupComboBox.SelectedItem == null)
        {
            FooterBar.StatusText = "Wybierz grupe parametru.";
            return;
        }

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
        OutputFolderTextBox.IsEnabled = SaveAsCopyCheckBox.IsChecked == true;
    }
}
