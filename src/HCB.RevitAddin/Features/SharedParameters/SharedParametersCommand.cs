using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.SharedParameters.Models;
using HCB.RevitAddin.Features.SharedParameters.UI;

namespace HCB.RevitAddin.Features.SharedParameters;

[Transaction(TransactionMode.Manual)]
public sealed class SharedParametersCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document document = commandData.Application.ActiveUIDocument.Document;
        Application application = commandData.Application.Application;
        SharedParametersService service = new();

        var missing = service.GetMissingParameters(document);
        if (missing.Count == 0)
        {
            TaskDialog.Show("Shared Parameters", "Wszystkie wymagane parametry sa juz wczytane.");
            return Result.Succeeded;
        }

        SharedParametersWindow window = new(missing, service.GetSharedParameterFilePath(application));
        if (window.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        SharedParametersResult result = service.LoadMissingParameters(document, application, window.Options.SelectedParameterNames);
        TaskDialog.Show(
            "Shared Parameters",
            $"Brakujace parametry: {result.MissingCount}\nDodane: {result.LoadedCount}\n\n{string.Join("\n", result.Messages.Take(12))}");
        return Result.Succeeded;
    }
}
