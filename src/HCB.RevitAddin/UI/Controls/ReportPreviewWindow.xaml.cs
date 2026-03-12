using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.UI.Controls;

public partial class ReportPreviewWindow : Window
{
    private const string AllColumnsFilterKey = "__ALL__";
    private const string AllValuesOption = "(wszystkie wartosci)";

    private readonly Action<string>? _exportAction;
    private readonly Action<IReadOnlyDictionary<string, string>>? _rowAction;
    private readonly Action<IReadOnlyList<IReadOnlyDictionary<string, string>>>? _batchAction;
    private readonly List<IReadOnlyDictionary<string, string>> _rows;
    private readonly string _suggestedFileName;
    private readonly DataView _tableView;
    private readonly IReadOnlyList<ReportPreviewColumn> _columns;
    private bool _isUpdatingFilterOptions;

    public ReportPreviewWindow(
        string title,
        string summary,
        IReadOnlyList<ReportPreviewColumn> columns,
        IEnumerable<IReadOnlyDictionary<string, string>> rows,
        string suggestedFileName,
        Action<string>? exportAction,
        string? rowActionButtonText = null,
        Action<IReadOnlyDictionary<string, string>>? rowAction = null,
        string? batchActionButtonText = null,
        Action<IReadOnlyList<IReadOnlyDictionary<string, string>>>? batchAction = null,
        string? footerSummary = null)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        SummaryTextBlock.Text = summary;
        FooterSummaryTextBlock.Text = footerSummary ?? string.Empty;
        FooterSummaryBorder.Visibility = string.IsNullOrWhiteSpace(footerSummary) ? Visibility.Collapsed : Visibility.Visible;
        _suggestedFileName = suggestedFileName;
        _exportAction = exportAction;
        _rowAction = rowAction;
        _batchAction = batchAction;
        _rows = rows.ToList();
        _columns = columns;

        DataTable table = BuildTable(columns, _rows);
        _tableView = table.DefaultView;
        ReportDataGrid.ItemsSource = _tableView;
        FilterColumnComboBox.ItemsSource = BuildFilterColumnItems(columns);
        FilterColumnComboBox.SelectedValue = AllColumnsFilterKey;
        RebuildFilterValueOptions();
        UpdateRowsCount();
        ExportButton.IsEnabled = exportAction != null;
        RowActionButton.Visibility = rowAction == null ? Visibility.Collapsed : Visibility.Visible;
        RowActionButton.Content = string.IsNullOrWhiteSpace(rowActionButtonText) ? "Otworz" : rowActionButtonText;
        BatchActionButton.Visibility = batchAction == null ? Visibility.Collapsed : Visibility.Visible;
        BatchActionButton.Content = string.IsNullOrWhiteSpace(batchActionButtonText) ? "Akcja zbiorcza" : batchActionButtonText;
        UpdateActionButtons();
    }

    private void ExportButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_exportAction == null)
        {
            return;
        }

        WithoutOpenDialogService dialogService = new();
        string? outputPath = dialogService.PickCsvOutputPath(_suggestedFileName);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return;
        }

        _exportAction(outputPath);
        SummaryTextBlock.Text += $"\nWyeksportowano CSV: {outputPath}";
    }

    private void FilterColumnComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        RebuildFilterValueOptions();
        ApplyFilter();
    }

    private void FilterValueComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingFilterOptions || !IsLoaded)
        {
            return;
        }

        ApplyFilter();
    }

    private void FilterValueComboBox_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (_isUpdatingFilterOptions)
        {
            return;
        }

        ApplyFilter();
    }

    private void FilterValueComboBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingFilterOptions)
        {
            return;
        }

        ApplyFilter();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ReportDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionButtons();
    }

    private void ReportDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_rowAction == null)
        {
            return;
        }

        ExecuteRowAction();
    }

    private void ReportDataGrid_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (string.Equals(e.PropertyName, "__RowIndex", StringComparison.Ordinal))
        {
            e.Cancel = true;
            return;
        }

        if (string.Equals(e.Column.Header?.ToString(), "Komunikat", StringComparison.OrdinalIgnoreCase))
        {
            e.Column = new DataGridTextColumn
            {
                Header = e.Column.Header,
                Binding = new Binding(e.PropertyName),
                Width = new DataGridLength(280),
                MaxWidth = 320,
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap),
                        new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center)
                    }
                }
            };
            return;
        }

        if (string.Equals(e.Column.Header?.ToString(), "Sciezka", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Column.Header?.ToString(), "Plik wynikowy", StringComparison.OrdinalIgnoreCase))
        {
            e.Column.Width = new DataGridLength(220);
            e.Column.MaxWidth = 280;
        }
    }

    private void RowActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteRowAction();
    }

    private void BatchActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteBatchAction();
    }

    private void OpenLocationButton_OnClick(object sender, RoutedEventArgs e)
    {
        string? path = GetPrimaryLocationPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        OpenLocation(path);
    }

    private void ExecuteRowAction()
    {
        if (_rowAction == null || !TryGetSelectedRow(out IReadOnlyDictionary<string, string>? row))
        {
            return;
        }

        _rowAction.Invoke(row!);
    }

    private void ExecuteBatchAction()
    {
        if (_batchAction == null)
        {
            return;
        }

        IReadOnlyList<IReadOnlyDictionary<string, string>> selectedRows = GetSelectedRows();
        if (selectedRows.Count == 0)
        {
            return;
        }

        _batchAction.Invoke(selectedRows);
    }

    private bool TryGetSelectedRow(out IReadOnlyDictionary<string, string>? row)
    {
        row = GetSelectedRows().FirstOrDefault();
        return row != null;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, string>> GetSelectedRows()
    {
        List<IReadOnlyDictionary<string, string>> selectedRows = [];
        foreach (object selectedItem in ReportDataGrid.SelectedItems)
        {
            if (selectedItem is not DataRowView dataRowView)
            {
                continue;
            }

            if (!dataRowView.Row.Table.Columns.Contains("__RowIndex"))
            {
                continue;
            }

            int rowIndex = Convert.ToInt32(dataRowView.Row["__RowIndex"]);
            if (rowIndex < 0 || rowIndex >= _rows.Count)
            {
                continue;
            }

            selectedRows.Add(_rows[rowIndex]);
        }

        return selectedRows;
    }

    private void UpdateActionButtons()
    {
        int selectedCount = GetSelectedRows().Count;
        RowActionButton.IsEnabled = _rowAction != null && selectedCount >= 1;
        BatchActionButton.IsEnabled = _batchAction != null && selectedCount >= 1;
        OpenLocationButton.IsEnabled = selectedCount >= 1 && !string.IsNullOrWhiteSpace(GetPrimaryLocationPath());
    }

    private void UpdateRowsCount()
    {
        RowsCountTextBlock.Text = $"Wiersze: {_tableView.Count}";
    }

    private void RebuildFilterValueOptions()
    {
        string currentText = GetCurrentFilterText();
        List<string> values = GetAvailableFilterValues();

        _isUpdatingFilterOptions = true;
        FilterValueComboBox.ItemsSource = values;
        FilterValueComboBox.Text = currentText;
        _isUpdatingFilterOptions = false;
    }

    private List<string> GetAvailableFilterValues()
    {
        string? selectedKey = FilterColumnComboBox.SelectedValue as string;
        IEnumerable<string> values;

        if (string.IsNullOrWhiteSpace(selectedKey) || string.Equals(selectedKey, AllColumnsFilterKey, StringComparison.Ordinal))
        {
            values = _rows
                .SelectMany(row => _columns.Select(column => row.TryGetValue(column.Key, out string? value) ? value : string.Empty));
        }
        else
        {
            values = _rows
                .Select(row => row.TryGetValue(selectedKey, out string? value) ? value : string.Empty);
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .Prepend(AllValuesOption)
            .ToList();
    }

    private void ApplyFilter()
    {
        string filterText = GetCurrentFilterText();
        if (string.IsNullOrWhiteSpace(filterText) || string.Equals(filterText, AllValuesOption, StringComparison.Ordinal))
        {
            _tableView.RowFilter = string.Empty;
            UpdateRowsCount();
            return;
        }

        string escaped = filterText.Replace("'", "''");
        string? selectedKey = FilterColumnComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedKey) || string.Equals(selectedKey, AllColumnsFilterKey, StringComparison.Ordinal))
        {
            string filterExpression = string.Join(
                " OR ",
                _columns.Select(column => $"[{column.Header}] LIKE '%{escaped}%'"));
            _tableView.RowFilter = filterExpression;
        }
        else
        {
            ReportPreviewColumn? column = _columns.FirstOrDefault(item => string.Equals(item.Key, selectedKey, StringComparison.Ordinal));
            _tableView.RowFilter = column == null ? string.Empty : $"[{column.Header}] LIKE '%{escaped}%'";
        }

        UpdateRowsCount();
    }

    private string GetCurrentFilterText()
    {
        return FilterValueComboBox.Text?.Trim() ?? string.Empty;
    }

    private IEnumerable<SelectionListItem> BuildFilterColumnItems(IReadOnlyList<ReportPreviewColumn> columns)
    {
        yield return new SelectionListItem(AllColumnsFilterKey, "Wszystkie");
        foreach (ReportPreviewColumn column in columns)
        {
            yield return new SelectionListItem(column.Key, column.Header);
        }
    }

    private string? GetPrimaryLocationPath()
    {
        if (!TryGetSelectedRow(out IReadOnlyDictionary<string, string>? row))
        {
            return null;
        }

        string[] keys = ["OutputPath", "FilePath", "Sciezka", "Plik wynikowy"];
        foreach (string key in keys)
        {
            if (row!.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static void OpenLocation(string path)
    {
        string targetPath = path;
        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
            return;
        }

        if (!Directory.Exists(targetPath))
        {
            targetPath = Path.GetDirectoryName(path) ?? string.Empty;
        }

        if (Directory.Exists(targetPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = targetPath,
                UseShellExecute = true
            });
        }
    }

    private static DataTable BuildTable(IReadOnlyList<ReportPreviewColumn> columns, IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        DataTable table = new();
        DataColumn rowIndexColumn = table.Columns.Add("__RowIndex", typeof(int));
        rowIndexColumn.ColumnMapping = MappingType.Hidden;

        foreach (ReportPreviewColumn column in columns)
        {
            table.Columns.Add(column.Header, typeof(string));
        }

        int rowIndex = 0;
        foreach (IReadOnlyDictionary<string, string> row in rows)
        {
            DataRow dataRow = table.NewRow();
            dataRow["__RowIndex"] = rowIndex++;
            foreach (ReportPreviewColumn column in columns)
            {
                dataRow[column.Header] = row.TryGetValue(column.Key, out string? value)
                    ? value ?? string.Empty
                    : string.Empty;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }
}
