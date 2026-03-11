using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.ManualNumbering.Models;

namespace HCB.RevitAddin.Features.ManualNumbering;

public sealed class ManualNumberingService
{
    public IReadOnlyList<string> GetWritableStringParameterNames(IEnumerable<Element> elements)
    {
        return elements
            .SelectMany(element => element.Parameters.Cast<Parameter>())
            .Where(parameter => !parameter.IsReadOnly && parameter.StorageType == StorageType.String)
            .Select(parameter => parameter.Definition?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .Cast<string>()
            .ToList();
    }

    public ManualNumberingResult Apply(Document document, IEnumerable<Element> elements, ManualNumberingOptions options)
    {
        ManualNumberingResult result = new();
        int currentNumber = options.StartNumber;

        using Transaction transaction = new(document, "Manual Numbering");
        transaction.Start();

        foreach (Element element in elements)
        {
            Parameter? parameter = element.LookupParameter(options.ParameterName);
            if (parameter == null || parameter.IsReadOnly || parameter.StorageType != StorageType.String)
            {
                result.SkippedCount++;
                result.Messages.Add($"Element {element.Id.Value}: pominiety, brak zapisywalnego parametru '{options.ParameterName}'.");
                continue;
            }

            parameter.Set($"{options.Prefix}{currentNumber}{options.Suffix}");
            currentNumber++;
            result.UpdatedCount++;
        }

        transaction.Commit();

        if (result.Messages.Count == 0)
        {
            result.Messages.Add("Numeracja zakonczona bez bledow.");
        }

        return result;
    }
}
