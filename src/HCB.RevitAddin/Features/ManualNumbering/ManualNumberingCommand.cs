using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HCB.RevitAddin.Features.ManualNumbering.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.ManualNumbering;

[Transaction(TransactionMode.Manual)]
public sealed class ManualNumberingCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        UIDocument uiDocument = commandData.Application.ActiveUIDocument;
        Document document = uiDocument.Document;

        IList<Reference> references;
        try
        {
            references = uiDocument.Selection.PickObjects(ObjectType.Element, new AnyElementSelectionFilter(), "Wybierz elementy do numeracji manualnej");
        }
        catch
        {
            return Result.Cancelled;
        }

        List<Element> selectedElements = references
            .Select(reference => document.GetElement(reference))
            .Where(element => element != null)
            .DistinctBy(element => element!.Id.Value)
            .Cast<Element>()
            .ToList();

        if (selectedElements.Count == 0)
        {
            TaskDialog.Show("Manual Numbering", "Nie wybrano zadnych elementow.");
            return Result.Succeeded;
        }

        ManualNumberingService service = new();
        IReadOnlyList<string> parameterNames = service.GetWritableStringParameterNames(selectedElements);
        if (parameterNames.Count == 0)
        {
            TaskDialog.Show("Manual Numbering", "Wybrane elementy nie maja zapisywalnych parametrow tekstowych.");
            return Result.Succeeded;
        }

        NumberingOptionsWindow optionsWindow = new("Manual Numbering", parameterNames);
        if (optionsWindow.ShowDialog() != true)
        {
            return Result.Cancelled;
        }

        ManualNumberingOptions options = new()
        {
            ParameterName = optionsWindow.SelectedParameterName,
            StartNumber = optionsWindow.StartNumber,
            Prefix = optionsWindow.Prefix,
            Suffix = optionsWindow.Suffix
        };

        List<Element> orderedElements = [];
        HashSet<long> remainingIds = selectedElements.Select(element => element.Id.Value).ToHashSet();

        while (remainingIds.Count > 0)
        {
            Reference reference;
            try
            {
                int currentIndex = orderedElements.Count + 1;
                string previewValue = $"{options.Prefix}{options.StartNumber + orderedElements.Count}{options.Suffix}";
                reference = uiDocument.Selection.PickObject(
                    ObjectType.Element,
                    new RemainingSelectionFilter(remainingIds),
                    $"Wskaz element nr {currentIndex} z {selectedElements.Count} | nastepna wartosc: {previewValue}");
            }
            catch
            {
                return Result.Cancelled;
            }

            Element pickedElement = document.GetElement(reference);
            if (!remainingIds.Remove(pickedElement.Id.Value))
            {
                continue;
            }

            orderedElements.Add(pickedElement);
            uiDocument.Selection.SetElementIds([.. orderedElements.Select(element => element.Id)]);
        }

        ManualNumberingResult result = service.Apply(document, orderedElements, options);
        List<ReportPreviewColumn> columns =
        [
            new() { Key = "Order", Header = "#" },
            new() { Key = "ElementId", Header = "ElementId" },
            new() { Key = "Category", Header = "Kategoria" },
            new() { Key = "Name", Header = "Nazwa" },
            new() { Key = "Value", Header = options.ParameterName }
        ];

        IReadOnlyList<IReadOnlyDictionary<string, string>> rows = orderedElements
            .Select((element, index) => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
            {
                ["Order"] = (index + 1).ToString(),
                ["ElementId"] = element.Id.Value.ToString(),
                ["Category"] = element.Category?.Name ?? string.Empty,
                ["Name"] = element.Name ?? string.Empty,
                ["Value"] = $"{options.Prefix}{options.StartNumber + index}{options.Suffix}"
            })
            .ToList();

        string summary = $"Wybrane: {selectedElements.Count}\nZaktualizowane: {result.UpdatedCount}\nPominiete: {result.SkippedCount}";
        if (result.Messages.Count > 0)
        {
            summary += $"\n{string.Join("\n", result.Messages.Take(6))}";
        }

        ReportPreviewWindow reportWindow = new(
            "Manual Numbering",
            summary,
            columns,
            rows,
            "manual-numbering.csv",
            null,
            "Pokaż element",
            row =>
            {
                if (!row.TryGetValue("ElementId", out string? elementIdText) ||
                    !long.TryParse(elementIdText, out long elementId))
                {
                    return;
                }

                ElementId targetId = new(elementId);
                uiDocument.Selection.SetElementIds([targetId]);
                uiDocument.ShowElements(targetId);
            });

        reportWindow.ShowDialog();

        return Result.Succeeded;
    }

    private sealed class AnyElementSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return element.Category != null;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    private sealed class RemainingSelectionFilter(HashSet<long> remainingIds) : ISelectionFilter
    {
        public bool AllowElement(Element element)
        {
            return remainingIds.Contains(element.Id.Value);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
