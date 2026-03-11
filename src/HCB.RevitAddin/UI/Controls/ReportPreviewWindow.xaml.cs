using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.UI.Controls;

public partial class ReportPreviewWindow : Window
{
    private readonly Action<string>? _exportAction;
    private readonly Action<IReadOnlyDictionary<string, string>>? _rowAction;
    private readonly List<IReadOnlyDictionary<string, string>> _rows;
    private readonly string _suggestedFileName;
    private readonly DataView _tableView;
    private readonly IReadOnlyList<ReportPreviewColumn> _columns;

    public ReportPreviewWindow(
        string title,
        string summary,
        IReadOnlyList<ReportPreviewColumn> columns,
        IEnumerable<IReadOnlyDictionary<string, string>> rows,
        string suggestedFileName,
        Action<string>? exportAction,
        string? rowActionButtonText = null,
        Action<IReadOnlyDictionary<string, string>>? rowAction = null)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        SummaryTextBlock.Text = summary;
        _suggestedFileName = suggestedFileName;
        _exportAction = exportAction;
        _rowAction = rowAction;
        _rows = rows.ToList();
        _columns = columns;

        DataTable table = BuildTable(columns, _rows);
        _tableView = table.DefaultView;
        ReportDataGrid.ItemsSource = _tableView;
        UpdateRowsCount();
        ExportButton.IsEnabled = exportAction != null;
        RowActionButton.Visibility = rowAction == null ? Visibility.Collapsed : Visibility.Visible;
        RowActionButton.Content = string.IsNullOrWhiteSpace(rowActionButtonText) ? "Otworz" : rowActionButtonText;
        RowActionButton.IsEnabled = false;
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

    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        string filterText = SearchTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(filterText))
        {
            _tableView.RowFilter = string.Empty;
            UpdateRowsCount();
            return;
        }

        string escaped = filterText.Replace("'", "''");
        string filterExpression = string.Join(
            " OR ",
            _columns.Select(column => $"[{column.Header}] LIKE '%{escaped}%'").ToArray());

        _tableView.RowFilter = filterExpression;
        UpdateRowsCount();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void ReportDataGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RowActionButton.IsEnabled = _rowAction != null && TryGetSelectedRow(out _);
    }

    private void ReportDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_rowAction == null)
        {
            return;
        }

        ExecuteRowAction();
    }

    private void RowActionButton_OnClick(object sender, RoutedEventArgs e)
    {
        ExecuteRowAction();
    }

    private void ExecuteRowAction()
    {
        if (_rowAction == null || !TryGetSelectedRow(out IReadOnlyDictionary<string, string>? row))
        {
            return;
        }

        _rowAction.Invoke(row!);
    }

    private bool TryGetSelectedRow(out IReadOnlyDictionary<string, string>? row)
    {
        row = null;
        if (ReportDataGrid.SelectedItem is not DataRowView dataRowView)
        {
            return false;
        }

        if (!dataRowView.Row.Table.Columns.Contains("__RowIndex"))
        {
            return false;
        }

        int rowIndex = Convert.ToInt32(dataRowView.Row["__RowIndex"]);
        if (rowIndex < 0 || rowIndex >= _rows.Count)
        {
            return false;
        }

        row = _rows[rowIndex];
        return true;
    }

    private void UpdateRowsCount()
    {
        RowsCountTextBlock.Text = $"Wiersze: {_tableView.Count}";
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
