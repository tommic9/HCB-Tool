using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.DuctFittingsArea.Models;
using HCB.RevitAddin.Features.DuctFittingsArea.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.DuctFittingsArea;

[Transaction(TransactionMode.Manual)]
public sealed class DuctFittingsAreaLinearCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        DuctFittingsAreaOptionsWindow optionsWindow = new();
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        DuctFittingsAreaService service = new();
        DuctFittingsAreaResult result = service.CalculateLinear(commandData.Application.ActiveUIDocument.Document, optionsWindow.Options);
        ShowReport("Duct Fittings Area LINEAR", "duct-fittings-area-linear.csv", result);
        return Result.Succeeded;
    }

    internal static void ShowReport(string title, string suggestedFileName, DuctFittingsAreaResult result)
    {
        List<ReportPreviewColumn> columns =
        [
            new() { Key = "ElementId", Header = "ElementId" },
            new() { Key = "Category", Header = "Kategoria" },
            new() { Key = "FamilyName", Header = "Rodzina" },
            new() { Key = "TypeName", Header = "Typ" },
            new() { Key = "Size", Header = "Size" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "Area", Header = "HC_Area [m2]" },
            new() { Key = "Source", Header = "Zrodlo" },
            new() { Key = "Reason", Header = "Komunikat" }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = result.Rows
            .Select(ToRow)
            .ToList();

        string summary =
            $"Przetworzone: {result.ProcessedCount} | Zaktualizowane: {result.UpdatedCount} | Pominiete: {result.SkippedCount}";

        string footerSummary =
            $"Podsumowanie: HC_Area razem {result.TotalAreaSquareMeters.ToString("0.###", CultureInfo.InvariantCulture)} m2 | zaktualizowane {result.UpdatedCount} | pominiete {result.SkippedCount}";

        ReportPreviewWindow previewWindow = new(
            title,
            summary,
            columns,
            rows,
            suggestedFileName,
            outputPath => ExportCsv(result.Rows, outputPath),
            footerSummary: footerSummary);

        previewWindow.ShowDialog();
    }

    private static IReadOnlyDictionary<string, string> ToRow(DuctFittingsAreaRow row)
    {
        return new Dictionary<string, string>
        {
            ["ElementId"] = row.ElementId.ToString(CultureInfo.InvariantCulture),
            ["Category"] = row.Category,
            ["FamilyName"] = row.FamilyName,
            ["TypeName"] = row.TypeName,
            ["Size"] = row.Size,
            ["Status"] = row.Status,
            ["Area"] = row.AreaSquareMeters?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty,
            ["Source"] = row.Source,
            ["Reason"] = row.Reason
        };
    }

    private static void ExportCsv(IEnumerable<DuctFittingsAreaRow> rows, string outputPath)
    {
        StringBuilder builder = new();
        builder.AppendLine("ElementId,Category,FamilyName,TypeName,Size,Status,AreaSquareMeters,Source,Reason");
        foreach (DuctFittingsAreaRow row in rows)
        {
            builder.AppendLine(string.Join(",",
                Escape(row.ElementId.ToString(CultureInfo.InvariantCulture)),
                Escape(row.Category),
                Escape(row.FamilyName),
                Escape(row.TypeName),
                Escape(row.Size),
                Escape(row.Status),
                Escape(row.AreaSquareMeters?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty),
                Escape(row.Source),
                Escape(row.Reason)));
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value)
    {
        return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}

