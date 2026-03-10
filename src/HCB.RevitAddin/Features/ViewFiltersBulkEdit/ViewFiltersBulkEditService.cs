using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ViewFiltersBulkEdit.Models;

namespace HCB.RevitAddin.Features.ViewFiltersBulkEdit;

public sealed class ViewFiltersBulkEditService
{
    public IReadOnlyList<View> GetViewsWithFilters(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(SupportsGraphicsOverrides)
            .Where(view => GetAppliedFilterIds(view).Count > 0)
            .OrderBy(view => view.Name)
            .ToList();
    }

    public IReadOnlyList<ParameterFilterElement> GetCommonFilters(Document document, IReadOnlyList<View> views)
    {
        Dictionary<long, ParameterFilterElement> common = [];

        foreach (View view in views)
        {
            HashSet<long> currentIds = GetAppliedFilterIds(view).Select(id => id.Value).ToHashSet();

            if (common.Count == 0)
            {
                common = currentIds
                    .Select(id => document.GetElement(new ElementId(id)))
                    .OfType<ParameterFilterElement>()
                    .ToDictionary(filter => filter.Id.Value, filter => filter);
                continue;
            }

            foreach (long id in common.Keys.ToList())
            {
                if (!currentIds.Contains(id))
                {
                    common.Remove(id);
                }
            }
        }

        return common.Values.OrderBy(filter => filter.Name).ToList();
    }

    public ViewFiltersBulkEditResult Apply(Document document, IReadOnlyList<View> views, IReadOnlyList<ParameterFilterElement> filters, ViewFiltersBulkEditOptions options)
    {
        ViewFiltersBulkEditResult result = new();

        using Transaction transaction = new(document, "View Filters Bulk Edit");
        transaction.Start();

        foreach (View view in views)
        {
            if (!SupportsGraphicsOverrides(view))
            {
                result.Messages.Add($"Pominieto widok: {view.Name}. Ten typ widoku nie obsluguje Visibility/Graphics Overrides.");
                continue;
            }

            foreach (ParameterFilterElement filter in filters)
            {
                if (options.EnableMode == "Enable")
                {
                    view.SetIsFilterEnabled(filter.Id, true);
                }
                else if (options.EnableMode == "Disable")
                {
                    view.SetIsFilterEnabled(filter.Id, false);
                }

                if (options.VisibilityMode == "Visible")
                {
                    view.SetFilterVisibility(filter.Id, true);
                }
                else if (options.VisibilityMode == "Hidden")
                {
                    view.SetFilterVisibility(filter.Id, false);
                }

                result.UpdatedFiltersCount++;
            }

            result.UpdatedViewsCount++;
            result.Messages.Add($"Zaktualizowano widok: {view.Name}");
        }

        transaction.Commit();
        return result;
    }

    private static IReadOnlyList<ElementId> GetAppliedFilterIds(View view)
    {
        try
        {
            return view.GetOrderedFilters().ToList();
        }
        catch
        {
            return view.GetFilters().ToList();
        }
    }

    private static bool SupportsGraphicsOverrides(View view)
    {
        try
        {
            return view.AreGraphicsOverridesAllowed();
        }
        catch
        {
            return false;
        }
    }
}
