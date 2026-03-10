using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.ColorVentSystems.Models;
using HCB.RevitAddin.Features.ColorVentSystems.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ColorVentSystems;

[Transaction(TransactionMode.Manual)]
public sealed class ColorVentSystemsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        ColorVentSystemsService service = new();
        IReadOnlyList<SystemColorPackageOption> availablePackages = service.GetAvailablePackages();

        SelectionListWindow window = new(
            "Kolorowanie systemow",
            "Pakiety systemow",
            availablePackages.Select(option => new SelectionListItem(option.Name, option.DisplayName)),
            [],
            "Koloruj systemy",
            "Wybierz gotowe pakiety systemow do pokolorowania w aktywnym widoku.");

        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ColorSetSystemsOptionsWindow optionsWindow = new();
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        IReadOnlyList<string> selectedPackageNames = window.SelectedValues.Cast<string>().ToList();
        ColorVentSystemsResult result = service.Apply(document, document.ActiveView, selectedPackageNames, optionsWindow.OverrideDisplayLines);

        TaskDialog.Show(
            "Color Vent Systems",
            $"Przetworzone systemy: {result.ProcessedSystemsCount}\n\n{string.Join("\n", result.Messages.Take(16))}");

        return Result.Succeeded;
    }
}
