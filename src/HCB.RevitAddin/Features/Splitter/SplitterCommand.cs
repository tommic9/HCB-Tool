using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HCB.RevitAddin.Features.Splitter.Models;
using HCB.RevitAddin.Features.Splitter.UI;

namespace HCB.RevitAddin.Features.Splitter;

[Transaction(TransactionMode.Manual)]
public sealed class SplitterCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument? uiDocument = commandData.Application.ActiveUIDocument;
        if (uiDocument?.Document == null)
        {
            TaskDialog.Show("Splitter", "To narzedzie wymaga otwartego projektu.");
            return Result.Succeeded;
        }

        SplitterWindow window = new();
        if (window.ShowDialog() != true || window.SelectedOptions == null)
        {
            return Result.Cancelled;
        }

        Document document = uiDocument.Document;
        SplitterOptions options = window.SelectedOptions;
        SplitterService service = new();

        int pickedCount = 0;
        int changedCount = 0;
        int totalSplits = 0;
        int totalFittings = 0;
        List<string> messages = [];

        while (true)
        {
            Reference reference;
            try
            {
                reference = uiDocument.Selection.PickObject(
                    ObjectType.PointOnElement,
                    new SplitterSelectionFilter(service, options),
                    "Klikaj ducts i pipes blizej konca startowego. ESC konczy.");
            }
            catch
            {
                break;
            }

            pickedCount++;

            using Transaction transaction = new(document, "Splitter");
            transaction.Start();

            try
            {
                SplitterResult result = service.SplitSelectedElement(document, reference, options);
                if (result.HasChanges)
                {
                    transaction.Commit();
                    changedCount++;
                    totalSplits += result.SplitCount;
                    totalFittings += result.FittingCount;
                }
                else
                {
                    transaction.RollBack();
                }

                messages.Add(result.Message);
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                Element? element = document.GetElement(reference);
                long elementId = element?.Id.Value ?? reference.ElementId.Value;
                messages.Add($"Element {elementId}: blad podzialu. {ex.Message}");
            }
        }

        if (pickedCount == 0)
        {
            return Result.Cancelled;
        }

        string summary = $"Klikniete elementy: {pickedCount}\nZmodyfikowane: {changedCount}\nPodzialy: {totalSplits}\nLaczniki: {totalFittings}";
        if (messages.Count > 0)
        {
            summary += $"\n\nSzczegoly:\n{string.Join("\n", messages.Take(12))}";
        }

        TaskDialog.Show("Splitter", summary);
        return Result.Succeeded;
    }

    private sealed class SplitterSelectionFilter(SplitterService service, SplitterOptions options) : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return service.IsSupported(element, options);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}

