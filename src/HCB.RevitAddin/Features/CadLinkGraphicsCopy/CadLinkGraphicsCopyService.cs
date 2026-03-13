using System;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.CadLinkGraphicsCopy.Models;

namespace HCB.RevitAddin.Features.CadLinkGraphicsCopy;

public sealed class CadLinkGraphicsCopyService
{
    public IReadOnlyList<ImportInstance> GetAvailableCadInstances(Document document)
    {
        return new FilteredElementCollector(document)
            .OfClass(typeof(ImportInstance))
            .Cast<ImportInstance>()
            .Where(instance => instance.Category != null)
            .OrderBy(instance => GetDisplayName(instance), StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public CadLinkGraphicsCopyResult CopyViewOverrides(
        Document document,
        View view,
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

        CopyCategorySettings(view, sourceCategory, targetCategory);
        result.UpdatedCategoryCount++;

        foreach (Category sourceLayer in sourceLayers)
        {
            string normalizedLayerName = NormalizeLayerName(sourceLayer.Name);
            if (!targetLayers.TryGetValue(normalizedLayerName, out Category? targetLayer))
            {
                result.MissingLayerCount++;
                result.Messages.Add($"Brak warstwy w linku docelowym: {sourceLayer.Name}");
                continue;
            }

            CopyCategorySettings(view, sourceLayer, targetLayer);
            result.MatchedLayerCount++;
            result.UpdatedCategoryCount++;
        }

        transaction.Commit();
        return result;
    }

    public string GetDisplayName(ImportInstance instance)
    {
        string mode = instance.IsLinked ? "Link" : "Import";
        string categoryName = instance.Category?.Name ?? "Brak kategorii";
        return $"{instance.Name} [{mode}] - {categoryName}";
    }

    private static void CopyCategorySettings(View view, Category sourceCategory, Category targetCategory)
    {
        OverrideGraphicSettings overrides = view.GetCategoryOverrides(sourceCategory.Id);
        bool isHidden = view.GetCategoryHidden(sourceCategory.Id);

        view.SetCategoryOverrides(targetCategory.Id, overrides);
        view.SetCategoryHidden(targetCategory.Id, isHidden);
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
}
