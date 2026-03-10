using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HCB.RevitAddin.Features.SharedParameters.Models;

namespace HCB.RevitAddin.Features.SharedParameters.UI;

public partial class SharedParametersWindow : Window
{
    public SharedParametersWindow(IReadOnlyList<string> missingParameters, string? sharedParameterFilePath)
    {
        InitializeComponent();

        ParametersListBox.ItemsSource = missingParameters;
        ParametersListBox.SelectAll();
        SharedParameterFileTextBox.Text = string.IsNullOrWhiteSpace(sharedParameterFilePath)
            ? "Brak ustawionego pliku Shared Parameters w Revit."
            : sharedParameterFilePath;

        FooterBar.StatusText = string.IsNullOrWhiteSpace(sharedParameterFilePath)
            ? "Ustaw plik Shared Parameters w Revit, zanim uruchomisz dodawanie."
            : $"Do wyboru: {missingParameters.Count} parametrow.";
    }

    public SharedParametersOptions Options => new()
    {
        SelectedParameterNames = ParametersListBox.SelectedItems.Cast<string>().ToList()
    };

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        ParametersListBox.SelectAll();
    }

    private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
    {
        ParametersListBox.UnselectAll();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SharedParameterFileTextBox.Text) ||
            SharedParameterFileTextBox.Text == "Brak ustawionego pliku Shared Parameters w Revit.")
        {
            FooterBar.StatusText = "Najpierw ustaw plik Shared Parameters w Revit.";
            return;
        }

        if (ParametersListBox.SelectedItems.Count == 0)
        {
            FooterBar.StatusText = "Wybierz co najmniej jeden parametr.";
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
