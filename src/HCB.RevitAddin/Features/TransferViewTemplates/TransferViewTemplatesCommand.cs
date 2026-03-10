using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.TransferViewTemplates.Models;
using HCB.RevitAddin.Features.TransferViewTemplates.UI;

namespace HCB.RevitAddin.Features.TransferViewTemplates;

[Transaction(TransactionMode.Manual)]
public sealed class TransferViewTemplatesCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        TransferViewTemplatesService service = new();
        var documents = service.GetAvailableProjectDocuments(commandData.Application.Application);

        if (documents.Count < 2)
        {
            TaskDialog.Show("Transfer View Templates", "Potrzebne sa co najmniej dwa otwarte projekty.");
            return Result.Succeeded;
        }

        TransferViewTemplatesWindow window = new(documents);
        if (window.ShowDialog() != true || window.SourceDocument == null || window.TargetDocument == null)
        {
            return Result.Cancelled;
        }

        TransferViewTemplatesResult result = service.Transfer(
            window.SourceDocument,
            window.TargetDocument,
            window.SelectedTemplates,
            window.OverrideExisting);

        TaskDialog.Show(
            "Transfer View Templates",
            $"Skopiowane szablony: {result.CopiedCount}\n\n{string.Join("\n", result.Messages)}");

        return Result.Succeeded;
    }
}
