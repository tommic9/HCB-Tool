using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ViewsDuplicate.Models;

namespace HCB.RevitAddin.Features.ViewsDuplicate;

public sealed class ViewsDuplicateService
{
    public IReadOnlyList<View> GetAvailableViews(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(view => view is not ViewSheet && !view.IsTemplate && view.CanViewBeDuplicated(ViewDuplicateOption.Duplicate))
            .OrderBy(view => view.Name)
            .ToList();
    }

    public ViewsDuplicateResult Duplicate(Document document, IEnumerable<View> views, ViewsDuplicateOptions options)
    {
        List<View> selectedViews = views.ToList();
        ViewsDuplicateResult result = new()
        {
            SourceCount = selectedViews.Count
        };

        ViewDuplicateOption duplicateOption = options.DuplicateMode switch
        {
            "WithDetailing" => ViewDuplicateOption.WithDetailing,
            "AsDependent" => ViewDuplicateOption.AsDependent,
            _ => ViewDuplicateOption.Duplicate
        };

        using Transaction transaction = new(document, "Duplicate Views");
        transaction.Start();

        foreach (View view in selectedViews)
        {
            for (int index = 0; index < options.CopiesCount; index++)
            {
                ViewDuplicateOption actualOption = GetSupportedOption(view, duplicateOption);
                ElementId newViewId = view.Duplicate(actualOption);
                if (newViewId != ElementId.InvalidElementId && document.GetElement(newViewId) is View newView)
                {
                    result.CreatedCount++;
                    result.Messages.Add($"{view.Name} -> {newView.Name}");
                }
            }
        }

        transaction.Commit();
        return result;
    }

    private static ViewDuplicateOption GetSupportedOption(View view, ViewDuplicateOption requested)
    {
        if (view.ViewType == ViewType.Schedule)
        {
            return ViewDuplicateOption.Duplicate;
        }

        if (view.ViewType == ViewType.Legend)
        {
            return ViewDuplicateOption.WithDetailing;
        }

        if (!view.CanViewBeDuplicated(requested))
        {
            return ViewDuplicateOption.Duplicate;
        }

        return requested;
    }
}
