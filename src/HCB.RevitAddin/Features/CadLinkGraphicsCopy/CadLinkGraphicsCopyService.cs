using System;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.CadLinkGraphicsCopy.Models;

namespace HCB.RevitAddin.Features.CadLinkGraphicsCopy;

public sealed class CadLinkGraphicsCopyService
{
    public IReadOnlyList<View> GetSupportedViews(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(IsSupportedView)
            .OrderByDescending(view => view.IsTemplate)
            .ThenBy(view => view.ViewType.ToString(), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(view => view.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ImportInstance> GetSelectableCadInstances(Document document, View contextView)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(ImportInstance))
            .Cast<ImportInstance>()
            .Where(instance => instance.Category != null)
            .Where(instance => IsInstanceAvailableInContext(instance, contextView))
            .GroupBy(instance => instance.Category!.Id.Value)
            .Select(group => group.OrderBy(instance => GetCadSourceName(instance), StringComparer.CurrentCultureIgnoreCase).First())
            .OrderBy(instance => GetDisplayName(instance), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public CadLinkGraphicsCopyResult CopyViewOverrides(
        Document document,
        View sourceView,
        View targetView,
        ImportInstance sourceInstance,
        ImportInstance targetInstance)
    {
        Category sourceCategory = sourceInstance.Category
            ?? throw new InvalidOperationException("Source CAD category is not available.");
        Category targetCategory = targetInstance.Category
            ?? throw new InvalidOperationException("Target CAD category is not available.");

        CadLinkGraphicsCopyResult result = new();

        Dictionary<string, Category> targetLayers = GetLayerLookup(targetCategory);
        IReadOnlyList<Category> sourceLayers = GetLayers(sourceCategory);

        result.SourceLayerCount = sourceLayers.Count;

        using Transaction transaction = new(document, "Copy CAD Link Graphics");
        transaction.Start();

        CopyCategoryVisibility(sourceView, targetView, sourceCategory, targetCategory, result);

        foreach (Category sourceLayer in sourceLayers)
        {
            string normalizedLayerName = NormalizeLayerName(sourceLayer.Name);
            if (!targetLayers.TryGetValue(normalizedLayerName, out Category? targetLayer))
            {
                result.MissingLayerCount++;
                result.Messages.Add($"Brak warstwy w linku docelowym: {sourceLayer.Name}");
                continue;
            }

            CopyCategorySettings(sourceView, targetView, sourceLayer, targetLayer, result);
            result.MatchedLayerCount++;
        }

        transaction.Commit();
        return result;
    }

    public string GetDisplayName(ImportInstance instance)
    {
        string mode = instance.IsLinked ? "Link" : "Import";
        string cadName = GetCadSourceName(instance);
        string viewInfo = GetViewContextLabel(instance);
        return $"{cadName} [{mode}] | {viewInfo}";
    }

    public string GetViewContextLabel(ImportInstance instance)
    {
        if (!instance.ViewSpecific)
        {
            return "Model-wide";
        }

        ElementId ownerViewId = instance.OwnerViewId;
        if (ownerViewId == ElementId.InvalidElementId)
        {
            return "Widok: nieznany";
        }

        View? ownerView = instance.Document.GetElement(ownerViewId) as View;
        return ownerView == null ? "Widok: nieznany" : $"Widok: {ownerView.Name}";
    }

    public string GetViewDisplayName(View view)
    {
        string scope = view.IsTemplate ? "Template" : view.ViewType.ToString();
        return $"{view.Name} [{scope}]";
    }

    private string GetCadSourceName(ImportInstance instance)
    {
        Element? typeElement = instance.Document.GetElement(instance.GetTypeId());
        if (typeElement != null && !string.IsNullOrWhiteSpace(typeElement.Name))
        {
            return typeElement.Name;
        }

        string? categoryName = instance.Category?.Name;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            return categoryName;
        }

        return instance.Name;
    }

    private static bool IsInstanceAvailableInContext(ImportInstance instance, View contextView)
    {
        if (!instance.ViewSpecific)
        {
            return true;
        }

        View? ownerView = instance.Document.GetElement(instance.OwnerViewId) as View;
        if (ownerView == null)
        {
            return false;
        }

        if (contextView.IsTemplate)
        {
            return ownerView.ViewTemplateId == contextView.Id;
        }

        return ownerView.Id == contextView.Id;
    }

    private static void CopyCategoryVisibility(
        View sourceView,
        View targetView,
        Category sourceCategory,
        Category targetCategory,
        CadLinkGraphicsCopyResult result)
    {
        try
        {
            bool isHidden = sourceView.GetCategoryHidden(sourceCategory.Id);
            targetView.SetCategoryHidden(targetCategory.Id, isHidden);
            result.UpdatedCategoryCount++;
        }
        catch (Exception exception)
        {
            result.Messages.Add($"Nie udalo sie skopiowac widocznosci kategorii {sourceCategory.Name}: {exception.Message}");
        }
    }

    private static void CopyCategorySettings(
        View sourceView,
        View targetView,
        Category sourceCategory,
        Category targetCategory,
        CadLinkGraphicsCopyResult result)
    {
        try
        {
            OverrideGraphicSettings overrides = sourceView.GetCategoryOverrides(sourceCategory.Id);
            bool isHidden = sourceView.GetCategoryHidden(sourceCategory.Id);

            targetView.SetCategoryOverrides(targetCategory.Id, overrides);
            targetView.SetCategoryHidden(targetCategory.Id, isHidden);
            result.UpdatedCategoryCount++;
        }
        catch (Exception exception)
        {
            result.Messages.Add($"Nie udalo sie skopiowac warstwy {sourceCategory.Name}: {exception.Message}");
        }
    }

    private static IReadOnlyList<Category> GetLayers(Category category)
    {
        if (category.SubCategories == null || category.SubCategories.IsEmpty)
        {
            return [];
        }

        return category.SubCategories
            .Cast<Category>()
            .OrderBy(subCategory => subCategory.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, Category> GetLayerLookup(Category category)
    {
        return GetLayers(category)
            .GroupBy(layer => NormalizeLayerName(layer.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static string NormalizeLayerName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }

    private static bool IsSupportedView(View view)
    {
        if (view.IsAssemblyView)
        {
            return false;
        }

        if (!view.CanBePrinted && !view.IsTemplate)
        {
            return false;
        }

        try
        {
            _ = view.ViewType;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
