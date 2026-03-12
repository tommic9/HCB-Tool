using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.ColorUniqueSystems.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ColorUniqueSystems;

[Transaction(TransactionMode.Manual)]
public sealed class ColorUniqueSystemsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        ColorUniqueSystemsService service = new();

        var allSystems = service.GetAvailableSystems()
            .Select(item => new ColorSelectionItem(item, item.DisplayLabel, item.Red, item.Green, item.Blue, item.FilterGroup))
            .ToList();

        string sourceLabel = service.UsesFallbackPreset()
            ? "Zrodlo kolorow: preset w kodzie (fallback)."
            : $"Zrodlo kolorow: CSV ({service.GetConfigurationSourceLabel()}).";

        ColorSelectionWindow window = new(
            "Color Unique Systems",
            "Systemy wentylacyjne i rurowe",
            allSystems,
            [],
            false,
            "Koloruj systemy",
            sourceLabel + " Wybierz systemy do pokolorowania i filtruj je po grupach zdefiniowanych w konfiguracji.");

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        List<SystemColorOption> selected = window.SelectedValues.Cast<SystemColorOption>().ToList();
        ColorUniqueSystemsResult result = service.Apply(document, document.ActiveView, selected, window.OverrideDisplayLines);
        string fallbackText = result.UsedFallbackPreset ? "Tak" : "Nie";

        TaskDialog.Show(
            "Color Unique Systems",
            $"Zastosowane systemy: {result.AppliedCount}\nFallback preset: {fallbackText}\nZrodlo: {result.ConfigurationSource}\n\n{string.Join("\n", result.Messages.Take(16))}");
        return Result.Succeeded;
    }
}
