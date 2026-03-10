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

        var allSystems = service.GetVentilationSystems()
            .Select(item => new ColorSelectionItem(item, item.DisplayName, item.Red, item.Green, item.Blue, "Wentylacja"))
            .Concat(service.GetPipingSystems()
                .Select(item => new ColorSelectionItem(item, item.DisplayName, item.Red, item.Green, item.Blue, "Rurowe")))
            .ToList();

        ColorSelectionWindow window = new(
            "Color Unique Systems",
            "Systemy wentylacyjne i rurowe",
            allSystems,
            [],
            false,
            "Koloruj systemy",
            "Wybierz systemy wentylacyjne i rurowe do pokolorowania. Mozesz filtrowac po grupie.");

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        List<SystemColorOption> selected = window.SelectedValues.Cast<SystemColorOption>().ToList();
        ColorUniqueSystemsResult result = service.Apply(document, document.ActiveView, selected, window.OverrideDisplayLines);
        TaskDialog.Show(
            "Color Unique Systems",
            $"Zastosowane systemy: {result.AppliedCount}\n\n{string.Join("\n", result.Messages.Take(16))}");
        return Result.Succeeded;
    }
}
