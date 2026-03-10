using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace HCB.RevitAddin.Features.TransferViewTemplates.UI;

public partial class TransferViewTemplatesWindow : Window
{
    private List<View> _allTemplates = new();

    public TransferViewTemplatesWindow(IReadOnlyList<Document> documents)
    {
        InitializeComponent();

        SourceDocumentComboBox.ItemsSource = documents;
        SourceDocumentComboBox.DisplayMemberPath = "Title";
        TargetDocumentComboBox.ItemsSource = documents;
        TargetDocumentComboBox.DisplayMemberPath = "Title";
        FooterBar.StatusText = "Wybierz projekt zrodlowy, docelowy i szablony do transferu.";
    }

    public Document? SourceDocument => SourceDocumentComboBox.SelectedItem as Document;

    public Document? TargetDocument => TargetDocumentComboBox.SelectedItem as Document;

    public bool OverrideExisting => OverrideExistingCheckBox.IsChecked == true;

    public IReadOnlyList<View> SelectedTemplates => TemplatesListBox.SelectedItems.Cast<View>().ToList();

    private void DocumentComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SourceDocument == null || TargetDocument == null || ReferenceEquals(SourceDocument, TargetDocument))
        {
            _allTemplates = [];
            TemplatesListBox.ItemsSource = _allTemplates;
            return;
        }

        _allTemplates = new FilteredElementCollector(SourceDocument)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(view => view.IsTemplate)
            .OrderBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        TemplatesListBox.ItemsSource = _allTemplates;
    }

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        string search = SearchTextBox.Text.Trim();
        TemplatesListBox.ItemsSource = string.IsNullOrWhiteSpace(search)
            ? _allTemplates
            : _allTemplates.Where(view => view.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
    }

    private void SelectAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        TemplatesListBox.SelectAll();
    }

    private void SelectNoneButton_OnClick(object sender, RoutedEventArgs e)
    {
        TemplatesListBox.UnselectAll();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (SourceDocument == null || TargetDocument == null)
        {
            FooterBar.StatusText = "Wybierz oba projekty.";
            return;
        }

        if (ReferenceEquals(SourceDocument, TargetDocument))
        {
            FooterBar.StatusText = "Projekt zrodlowy i docelowy musza byc rozne.";
            return;
        }

        if (TemplatesListBox.SelectedItems.Count == 0)
        {
            FooterBar.StatusText = "Wybierz co najmniej jeden szablon.";
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
