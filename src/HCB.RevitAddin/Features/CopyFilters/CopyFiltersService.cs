using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.CopyFilters.Models;

namespace HCB.RevitAddin.Features.CopyFilters
{
    internal enum ExistingFilterAction
    {
        Overwrite,
        Skip
    }

    internal sealed class CopyFiltersService
    {
        public IList<View> GetSupportedViews(Document document)
        {
            return new FilteredElementCollector(document)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(IsSupportedView)
                .OrderByDescending(view => view.IsTemplate)
                .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public IList<ParameterFilterElement> GetFiltersForView(Document document, View sourceView)
        {
            return sourceView
                .GetFilters()
                .Select(document.GetElement)
                .OfType<ParameterFilterElement>()
                .OrderBy(filter => filter.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public bool HasConflicts(IEnumerable<View> destinationViews, IEnumerable<ElementId> filterIds)
        {
            HashSet<long> selectedIds = new HashSet<long>(filterIds.Select(id => id.Value));

            foreach (View destinationView in destinationViews)
            {
                foreach (ElementId filterId in destinationView.GetFilters())
                {
                    if (selectedIds.Contains(filterId.Value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public CopyFiltersResult CopyFilters(
            Document document,
            View sourceView,
            IList<ParameterFilterElement> selectedFilters,
            IList<View> destinationViews,
            ExistingFilterAction existingFilterAction)
        {
            CopyFiltersResult result = new CopyFiltersResult
            {
                ProcessedViewsCount = destinationViews.Count,
                SelectedFiltersCount = selectedFilters.Count
            };

            List<ElementId> sourceOrder = sourceView.GetFilters().ToList();
            Dictionary<long, int> sourcePositions = sourceOrder
                .Select((id, index) => new { IdValue = id.Value, Index = index })
                .ToDictionary(item => item.IdValue, item => item.Index);

            using (Transaction transaction = new Transaction(document, "Copy Filters"))
            {
                transaction.Start();

                foreach (View destinationView in destinationViews)
                {
                    ProcessDestinationView(
                        sourceView,
                        destinationView,
                        selectedFilters,
                        sourcePositions,
                        existingFilterAction,
                        result);
                }

                transaction.Commit();
            }

            return result;
        }

        private static void ProcessDestinationView(
            View sourceView,
            View destinationView,
            IList<ParameterFilterElement> selectedFilters,
            IDictionary<long, int> sourcePositions,
            ExistingFilterAction existingFilterAction,
            CopyFiltersResult result)
        {
            List<ElementId> destinationFilterIds = destinationView.GetFilters().ToList();
            HashSet<long> destinationFilterSet = new HashSet<long>(destinationFilterIds.Select(id => id.Value));

            foreach (ParameterFilterElement filter in selectedFilters.OrderBy(f => sourcePositions[f.Id.Value]))
            {
                bool alreadyApplied = destinationFilterSet.Contains(filter.Id.Value);

                if (alreadyApplied && existingFilterAction == ExistingFilterAction.Skip)
                {
                    result.SkippedFiltersCount++;
                    result.Messages.Add($"{destinationView.Name}: skipped '{filter.Name}'.");
                    continue;
                }

                OverrideGraphicSettings overrides = sourceView.GetFilterOverrides(filter.Id);
                bool isEnabled = sourceView.GetIsFilterEnabled(filter.Id);
                bool visibility = sourceView.GetFilterVisibility(filter.Id);

                if (alreadyApplied)
                {
                    destinationView.RemoveFilter(filter.Id);
                    result.UpdatedFiltersCount++;
                }
                else
                {
                    result.AddedFiltersCount++;
                }

                destinationView.AddFilter(filter.Id);
                destinationView.SetFilterOverrides(filter.Id, overrides);
                destinationView.SetIsFilterEnabled(filter.Id, isEnabled);
                destinationView.SetFilterVisibility(filter.Id, visibility);
            }

            ReorderFilters(destinationView, sourcePositions);
        }

        private static void ReorderFilters(View destinationView, IDictionary<long, int> sourcePositions)
        {
            List<ElementId> orderedIds = destinationView.GetFilters()
                .OrderBy(id => sourcePositions.TryGetValue(id.Value, out int order) ? order : int.MaxValue)
                .ToList();

            foreach (ElementId filterId in orderedIds)
            {
                OverrideGraphicSettings overrides = destinationView.GetFilterOverrides(filterId);
                bool isEnabled = destinationView.GetIsFilterEnabled(filterId);
                bool visibility = destinationView.GetFilterVisibility(filterId);

                destinationView.RemoveFilter(filterId);
                destinationView.AddFilter(filterId);
                destinationView.SetFilterOverrides(filterId, overrides);
                destinationView.SetIsFilterEnabled(filterId, isEnabled);
                destinationView.SetFilterVisibility(filterId, visibility);
            }
        }

        private static bool IsSupportedView(View view)
        {
            if (view == null || view.IsAssemblyView)
            {
                return false;
            }

            if (!view.CanBePrinted && !view.IsTemplate)
            {
                return false;
            }

            try
            {
                view.GetFilters();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
