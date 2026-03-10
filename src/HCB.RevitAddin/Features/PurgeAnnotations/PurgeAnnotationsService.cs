using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.PurgeAnnotations.Models;

namespace HCB.RevitAddin.Features.PurgeAnnotations;

public sealed class PurgeAnnotationsService
{
    public IReadOnlyList<ElementType> GetUnusedAnnotationTypes(Document document)
    {
        List<ElementType> annotationTypes = new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .ToElements()
            .OfType<ElementType>()
            .Where(type => type.Category?.CategoryType == CategoryType.Annotation)
            .OrderBy(type => type.Category?.Name ?? string.Empty)
            .ThenBy(GetDisplayName)
            .ToList();

        HashSet<long> usedTypeIds = new(
            new FilteredElementCollector(document)
                .WhereElementIsNotElementType()
                .ToElements()
                .Select(element => element.GetTypeId())
                .Where(id => id != ElementId.InvalidElementId)
                .Select(id => id.Value));

        return annotationTypes
            .Where(type => !usedTypeIds.Contains(type.Id.Value))
            .ToList();
    }

    public PurgeAnnotationsResult DeleteTypes(Document document, IEnumerable<ElementType> types)
    {
        PurgeAnnotationsResult result = new();

        using Transaction transaction = new(document, "Purge Unused Annotation Types");
        transaction.Start();

        foreach (ElementType type in types)
        {
            try
            {
                document.Delete(type.Id);
                result.DeletedCount++;
                result.Messages.Add($"Usunieto: {GetDisplayName(type)}");
            }
            catch
            {
                result.FailedCount++;
                result.Messages.Add($"Nie udalo sie usunac: {GetDisplayName(type)}");
            }
        }

        transaction.Commit();
        return result;
    }

    public string GetDisplayName(ElementType type)
    {
        string? typeName = type.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsString();
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            return typeName;
        }

        if (!string.IsNullOrWhiteSpace(type.Name))
        {
            return type.Name;
        }

        return $"{type.FamilyName} [{type.Id.Value}]";
    }

    public string GetFilterableLabel(ElementType type)
    {
        string categoryName = type.Category?.Name ?? "No Category";
        string className = type.GetType().Name;
        return $"[{categoryName}] {GetDisplayName(type)} ({className})";
    }
}
