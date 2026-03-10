using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.RenameMaterials.UI;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameMaterials;

[Transaction(TransactionMode.Manual)]
public sealed class RenameMaterialsCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;

        var materials = new FilteredElementCollector(document)
            .OfClass(typeof(Material))
            .Cast<Material>()
            .OrderBy(material => material.Name)
            .ToList();

        if (materials.Count == 0)
        {
            TaskDialog.Show("Rename Materials", "Brak materialow w projekcie.");
            return Result.Succeeded;
        }

        SelectionListWindow materialsWindow = new(
            "Rename Materials",
            "Wybierz materialy",
            materials.Select(material => new SelectionListItem(material, material.Name)),
            [],
            "Dalej",
            "Wybierz materialy do zmiany nazw.");

        if (materialsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        RenameOptionsWindow optionsWindow = new("Rename Materials", RenameOptionsMode.Views);
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        RenameMaterialsService service = new();
        var previewItems = service.BuildPreview(document, materialsWindow.SelectedValues.Cast<Material>().ToList(), optionsWindow.Options);
        RenameMaterialsPreviewWindow previewWindow = new(previewItems);
        if (previewWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        var result = service.Apply(document, materialsWindow.SelectedValues.Cast<Material>().ToList(), optionsWindow.Options);

        TaskDialog.Show(
            "Rename Materials",
            $"Zmienione: {result.RenamedCount}\nBez zmian: {result.UnchangedCount}\nPominiete konflikty nazw: {result.SkippedCount}");

        return Result.Succeeded;
    }
}
