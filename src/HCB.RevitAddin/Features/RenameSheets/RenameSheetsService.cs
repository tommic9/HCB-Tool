using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.RenameSheets.Models;
using HCB.RevitAddin.UI.Controls;

namespace HCB.RevitAddin.Features.RenameSheets;

public sealed class RenameSheetsService
{
    public RenameSheetsResult Rename(Document document, IEnumerable<ViewSheet> sheets, RenameOptions options)
    {
        RenameSheetsResult result = new();

        using Transaction transaction = new(document, "Rename Sheets");
        transaction.Start();

        foreach (ViewSheet sheet in sheets)
        {
            result.ProcessedCount++;

            string currentName = sheet.Name;
            string currentNumber = sheet.SheetNumber;
            string newName = BuildName(currentName, options);
            string newNumber = BuildNumber(currentNumber, options);

            for (int attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    sheet.Name = newName;
                    sheet.SheetNumber = newNumber;
                    result.RenamedCount++;
                    result.Messages.Add($"{currentNumber} / {currentName} -> {newNumber} / {newName}");
                    break;
                }
                catch (Exception)
                {
                    newName += "*";
                    newNumber += "*";
                }
            }
        }

        transaction.Commit();
        return result;
    }

    private static string BuildName(string currentName, RenameOptions options)
    {
        string replaced = string.IsNullOrEmpty(options.Find)
            ? currentName
            : currentName.Replace(options.Find, options.Replace ?? string.Empty);

        return $"{options.Prefix}{replaced}{options.Suffix}";
    }

    private static string BuildNumber(string currentNumber, RenameOptions options)
    {
        string replaced = string.IsNullOrEmpty(options.NumberFind)
            ? currentNumber
            : currentNumber.Replace(options.NumberFind, options.NumberReplace ?? string.Empty);

        return $"{options.NumberPrefix}{replaced}";
    }
}
