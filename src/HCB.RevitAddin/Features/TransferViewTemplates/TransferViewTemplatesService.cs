using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.TransferViewTemplates.Models;

namespace HCB.RevitAddin.Features.TransferViewTemplates;

public sealed class TransferViewTemplatesService
{
    public IReadOnlyList<Document> GetAvailableProjectDocuments(Application application)
    {
        return application.Documents
            .Cast<Document>()
            .Where(document => !document.IsFamilyDocument && !document.IsLinked)
            .OrderBy(document => document.Title)
            .ToList();
    }

    public TransferViewTemplatesResult Transfer(Document sourceDocument, Document targetDocument, IEnumerable<View> templates, bool overwrite)
    {
        List<View> selectedTemplates = templates.ToList();
        List<string> selectedNames = selectedTemplates.Select(view => view.Name).ToList();
        TransferViewTemplatesResult result = new();

        Dictionary<string, List<ElementId>> reassignmentMap = [];

        using TransactionGroup transactionGroup = new(targetDocument, "Transfer View Templates");
        transactionGroup.Start();

        if (overwrite)
        {
            using Transaction removeTransaction = new(targetDocument, "Remove Existing View Templates");
            removeTransaction.Start();
            reassignmentMap = RemoveExistingTemplates(targetDocument, selectedNames);
            removeTransaction.Commit();
        }

        using Transaction copyTransaction = new(targetDocument, "Copy View Templates");
        copyTransaction.Start();
        ElementTransformUtils.CopyElements(
            sourceDocument,
            selectedTemplates.Select(view => view.Id).ToList(),
            targetDocument,
            Transform.Identity,
            new CopyPasteOptions());
        copyTransaction.Commit();

        if (overwrite && reassignmentMap.Count > 0)
        {
            using Transaction assignTransaction = new(targetDocument, "Reassign View Templates");
            assignTransaction.Start();
            ReassignTemplates(targetDocument, reassignmentMap);
            assignTransaction.Commit();
        }

        transactionGroup.Assimilate();

        result.CopiedCount = selectedTemplates.Count;
        foreach (string name in selectedNames)
        {
            result.Messages.Add(overwrite ? $"Zaktualizowano: {name}" : $"Dodano: {name}");
        }

        return result;
    }

    private static Dictionary<string, List<ElementId>> RemoveExistingTemplates(Document document, IReadOnlyCollection<string> selectedNames)
    {
        Dictionary<string, List<ElementId>> reassignmentMap = [];

        List<View> templates = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(view => view.IsTemplate && selectedNames.Contains(view.Name))
            .ToList();

        foreach (View template in templates)
        {
            reassignmentMap[template.Name] = new FilteredElementCollector(document)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(view => !view.IsTemplate && view.ViewTemplateId == template.Id)
                .Select(view => view.Id)
                .ToList();

            document.Delete(template.Id);
        }

        return reassignmentMap;
    }

    private static void ReassignTemplates(Document document, IReadOnlyDictionary<string, List<ElementId>> reassignmentMap)
    {
        Dictionary<string, View> templatesByName = new FilteredElementCollector(document)
            .OfClass(typeof(View))
            .Cast<View>()
            .Where(view => view.IsTemplate)
            .ToDictionary(view => view.Name, view => view);

        foreach ((string templateName, List<ElementId> viewIds) in reassignmentMap)
        {
            if (!templatesByName.TryGetValue(templateName, out View? template))
            {
                continue;
            }

            foreach (ElementId viewId in viewIds)
            {
                if (document.GetElement(viewId) is View view)
                {
                    view.ViewTemplateId = template.Id;
                }
            }
        }
    }
}
