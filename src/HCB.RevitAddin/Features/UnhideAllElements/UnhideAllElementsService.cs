using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.UnhideAllElements.Models;

namespace HCB.RevitAddin.Features.UnhideAllElements;

public sealed class UnhideAllElementsService
{
    public UnhideAllElementsResult Execute(Document document, View view)
    {
        List<ElementId> candidates = new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .ToElementIds()
            .Where(id => id != ElementId.InvalidElementId)
            .ToList();

        UnhideAllElementsResult result = new()
        {
            CandidateCount = candidates.Count
        };

        if (candidates.Count == 0)
        {
            return result;
        }

        using Transaction transaction = new(document, "Unhide All Elements");
        transaction.Start();
        view.UnhideElements(candidates);
        transaction.Commit();

        result.UnhiddenCount = candidates.Count;
        return result;
    }
}
