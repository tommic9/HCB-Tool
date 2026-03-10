using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.RenameViews.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameViews;

public sealed class RenameViewsService
{
    public RenameViewsResult Rename(Document document, IEnumerable<View> views, RenameOptions options)
    {
        RenameViewsResult result = new();

        using Transaction transaction = new(document, "Rename Views");
        transaction.Start();

        foreach (View view in views)
        {
            result.ProcessedCount++;
            string currentName = view.Name;
            string newName = BuildNewName(currentName, options);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    view.Name = newName;
                    result.RenamedCount++;
                    result.Messages.Add($"{currentName} -> {newName}");
                    break;
                }
                catch (Exception)
                {
                    newName += "*";
                }
            }
        }

        transaction.Commit();
        return result;
    }

    private static string BuildNewName(string currentName, RenameOptions options)
    {
        string replaced = string.IsNullOrEmpty(options.Find)
            ? currentName
            : currentName.Replace(options.Find, options.Replace ?? string.Empty);

        return $"{options.Prefix}{replaced}{options.Suffix}";
    }
}
