using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using HCB.RevitAddin.Features.RenameMaterials.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameMaterials;

public sealed class RenameMaterialsService
{
    public IReadOnlyList<RenameMaterialsPreviewItem> BuildPreview(Document document, IReadOnlyList<Material> materials, RenameOptions options)
    {
        HashSet<string> existingNames = new(new FilteredElementCollector(document)
            .OfClass(typeof(Material))
            .Cast<Material>()
            .Select(material => material.Name));

        List<RenameMaterialsPreviewItem> previewItems = [];
        foreach (Material material in materials)
        {
            string newName = BuildNewName(material.Name, options);
            string status = newName == material.Name
                ? "Bez zmian"
                : existingNames.Contains(newName) && newName != material.Name
                    ? "Konflikt"
                    : "OK";

            previewItems.Add(new RenameMaterialsPreviewItem(material.Name, newName, status));
        }

        return previewItems;
    }

    public RenameMaterialsResult Apply(Document document, IReadOnlyList<Material> materials, RenameOptions options)
    {
        RenameMaterialsResult result = new();

        using Transaction transaction = new(document, "Rename Materials");
        transaction.Start();

        foreach (Material material in materials)
        {
            string newName = BuildNewName(material.Name, options);
            if (newName == material.Name)
            {
                result.UnchangedCount++;
                continue;
            }

            try
            {
                material.Name = newName;
                result.RenamedCount++;
            }
            catch (ArgumentException)
            {
                result.SkippedCount++;
            }
        }

        transaction.Commit();
        return result;
    }

    private static string BuildNewName(string currentName, RenameOptions options)
    {
        return $"{options.Prefix}{currentName.Replace(options.Find, options.Replace)}{options.Suffix}";
    }
}
