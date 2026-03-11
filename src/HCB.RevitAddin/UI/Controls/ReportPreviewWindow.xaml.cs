using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using HCB.RevitAddin.Infrastructure.WithoutOpen;

namespace HCB.RevitAddin.UI.Controls;

public partial class ReportPreviewWindow : Window
{
    private readonly Action<string>? _exportAction;
    private readonly string _suggestedFileName;

    public ReportPreviewWindow(
        string title,
        string summary,
        IReadOnlyList<ReportPreviewColumn> columns,
        IEnumerable<IReadOnlyDictionary<string, string>> rows,
        string suggestedFileName,
        Action<string>? exportAction)
    {
        InitializeComponent();

        Title = title;
        TitleTextBlock.Text = title;
        SummaryTextBlock.Text = summary;
        _suggestedFileName = suggestedFileName;
        _exportAction = exportAction;

        DataTable table = BuildTable(columns, rows);
        ReportDataGrid.ItemsSource = table.DefaultView;
        RowsCountTextBlock.Text = $"Wiersze: {table.Rows.Count}";
        ExportButton.IsEnabled = exportAction != null;
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

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private static DataTable BuildTable(IReadOnlyList<ReportPreviewColumn> columns, IEnumerable<IReadOnlyDictionary<string, string>> rows)
    {
        DataTable table = new();
        foreach (ReportPreviewColumn column in columns)
        {
            table.Columns.Add(column.Header, typeof(string));
        }

        foreach (IReadOnlyDictionary<string, string> row in rows)
        {
            DataRow dataRow = table.NewRow();
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
