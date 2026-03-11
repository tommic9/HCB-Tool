using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.Estimate.Models;
using HCB.RevitAddin.Features.Estimate.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.Estimate;

[Transaction(TransactionMode.Manual)]
public sealed class EstimateCommand : IExternalCommand
{
    private static string _lastCatalogPath = string.Empty;

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        EstimateOptionsWindow window = new(_lastCatalogPath);
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        EstimateOptions options = window.Options;
        _lastCatalogPath = options.CatalogPath;

        EstimateService service = new();
        EstimateResult result = service.Apply(document, options);

        List<ReportPreviewColumn> columns =
        [
            new() { Key = "ElementId", Header = "ElementId" },
            new() { Key = "Category", Header = "Kategoria" },
            new() { Key = "TypeName", Header = "Typ" },
            new() { Key = "Size", Header = "Size" },
            new() { Key = "Angle", Header = "Kat" },
            new() { Key = "Status", Header = "Status" },
            new() { Key = "PricingBasis", Header = "Podstawa" },
            new() { Key = "Quantity", Header = "Ilosc" },
            new() { Key = "QuantityUnit", Header = "Jedn." },
            new() { Key = "UnitPrice", Header = "Cena jedn." },
            new() { Key = "Cost", Header = "Koszt" },
            new() { Key = "Reason", Header = "Komunikat" }
        ];

        List<IReadOnlyDictionary<string, string>> rows = result.AppliedRows
            .Select(ToAppliedRow)
            .Concat(result.UnmatchedRows.Select(ToUnmatchedRow))
            .ToList();

        string summary =
            $"Przetworzone: {result.ProcessedCount} | Zaktualizowane: {result.UpdatedCount} | Brak dopasowania: {result.UnmatchedCount}";

        string footerSummary =
            $"Podsumowanie: koszt razem {result.TotalCost.ToString("0.##", CultureInfo.InvariantCulture)} | powierzchnia razem {result.TotalAreaSquareMeters.ToString("0.###", CultureInfo.InvariantCulture)} m2 | dlugosc razem {result.TotalLengthMeters.ToString("0.###", CultureInfo.InvariantCulture)} m | brak HC_Kat: {result.MissingAngleElementIds.Count}";

        ReportPreviewWindow previewWindow = new(
            "Estimate",
            summary,
            columns,
            rows,
            "estimate-report.csv",
            outputPath => ExportCsv(result, outputPath),
            footerSummary: footerSummary);

        previewWindow.ShowDialog();
        return Result.Succeeded;
    }

    private static IReadOnlyDictionary<string, string> ToAppliedRow(EstimateAppliedRow row)
    {
        return new Dictionary<string, string>
        {
            ["ElementId"] = row.ElementId.ToString(CultureInfo.InvariantCulture),
            ["Category"] = row.Category,
            ["TypeName"] = row.TypeName,
            ["Size"] = row.Size,
            ["Angle"] = row.Angle?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["Status"] = "Success",
            ["PricingBasis"] = row.PricingBasis,
            ["Quantity"] = row.Quantity.ToString("0.###", CultureInfo.InvariantCulture),
            ["QuantityUnit"] = row.QuantityUnit,
            ["UnitPrice"] = row.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture),
            ["Cost"] = row.Cost.ToString("0.##", CultureInfo.InvariantCulture),
            ["Reason"] = string.Empty
        };
    }

    private static IReadOnlyDictionary<string, string> ToUnmatchedRow(EstimateUnmatchedRow row)
    {
        return new Dictionary<string, string>
        {
            ["ElementId"] = row.ElementId.ToString(CultureInfo.InvariantCulture),
            ["Category"] = row.Category,
            ["TypeName"] = row.TypeName,
            ["Size"] = row.Size,
            ["Angle"] = row.Angle?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["Status"] = "Unmatched",
            ["PricingBasis"] = string.Empty,
            ["Quantity"] = string.Empty,
            ["QuantityUnit"] = string.Empty,
            ["UnitPrice"] = string.Empty,
            ["Cost"] = string.Empty,
            ["Reason"] = row.Reason
        };
    }

    private static void ExportCsv(EstimateResult result, string outputPath)
    {
        StringBuilder builder = new();
        builder.AppendLine("ElementId,Category,TypeName,Size,Angle,Status,PricingBasis,Quantity,QuantityUnit,UnitPrice,Cost,Reason");

        foreach (EstimateAppliedRow row in result.AppliedRows)
        {
            builder.AppendLine(string.Join(",",
                Escape(row.ElementId.ToString(CultureInfo.InvariantCulture)),
                Escape(row.Category),
                Escape(row.TypeName),
                Escape(row.Size),
                Escape(row.Angle?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                Escape("Success"),
                Escape(row.PricingBasis),
                Escape(row.Quantity.ToString("0.###", CultureInfo.InvariantCulture)),
                Escape(row.QuantityUnit),
                Escape(row.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture)),
                Escape(row.Cost.ToString("0.##", CultureInfo.InvariantCulture)),
                Escape(string.Empty)));
        }

        foreach (EstimateUnmatchedRow row in result.UnmatchedRows)
        {
            builder.AppendLine(string.Join(",",
                Escape(row.ElementId.ToString(CultureInfo.InvariantCulture)),
                Escape(row.Category),
                Escape(row.TypeName),
                Escape(row.Size),
                Escape(row.Angle?.ToString(CultureInfo.InvariantCulture) ?? string.Empty),
                Escape("Unmatched"),
                Escape(string.Empty),
                Escape(string.Empty),
                Escape(string.Empty),
                Escape(string.Empty),
                Escape(string.Empty),
                Escape(row.Reason)));
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
    }

    private static string Escape(string value)
    {
        return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}

