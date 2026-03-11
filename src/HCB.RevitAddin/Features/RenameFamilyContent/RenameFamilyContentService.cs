using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.RenameFamilyContent.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.RenameFamilyContent;

public sealed class RenameFamilyContentService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileClassifier _fileClassifier = new();
    private readonly WithoutOpenDocumentService _documentService = new();

    public RenameFamilyContentResult Rename(Application application, IEnumerable<string> familyPaths, RenameFamilyContentOptions options)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(familyPaths);
        if (options.SaveAsCopy)
        {
            Directory.CreateDirectory(options.OutputFolderPath);
        }

        List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);
        foreach (string familyPath in normalizedPaths)
        {
            entries.Add(RenameSingle(application, familyPath, options));
        }

        return new RenameFamilyContentResult
        {
            Entries = entries
        };
    }

    private WithoutOpenOperationLogEntry RenameSingle(Application application, string familyPath, RenameFamilyContentOptions options)
    {
        DateTime startedAt = DateTime.UtcNow;

        try
        {
            if (_fileClassifier.GetFileKind(familyPath) != WithoutOpenFileKind.Family)
            {
                return CreateLog(familyPath, WithoutOpenOperationStatus.Skipped, "Pominieto plik, bo nie jest rodzina .rfa.", startedAt);
            }

            Document? document = null;
            try
            {
                document = _documentService.OpenDocument(application, familyPath);
                if (!document.IsFamilyDocument)
                {
                    return CreateLog(familyPath, WithoutOpenOperationStatus.Skipped, "Pominieto plik, bo nie jest dokumentem rodziny.", startedAt);
                }

                FamilyManager familyManager = document.FamilyManager;
                int renamedParameters = 0;
                int renamedTypes = 0;

                using Transaction transaction = new(document, "Rename Family Content");
                transaction.Start();

                HashSet<string> parameterNames = familyManager.Parameters
                    .Cast<FamilyParameter>()
                    .Select(parameter => parameter.Definition?.Name ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (FamilyParameter parameter in familyManager.Parameters)
                {
                    if (parameter.IsShared)
                    {
                        continue;
                    }

                    string currentName = parameter.Definition?.Name ?? string.Empty;
                    string targetName = ApplyRenameRule(currentName, options);
                    if (string.IsNullOrWhiteSpace(targetName) || string.Equals(currentName, targetName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (parameterNames.Contains(targetName))
                    {
                        continue;
                    }

                    familyManager.RenameParameter(parameter, targetName);
                    parameterNames.Remove(currentName);
                    parameterNames.Add(targetName);
                    renamedParameters++;
                }

                HashSet<string> typeNames = familyManager.Types
                    .Cast<FamilyType>()
                    .Select(type => type.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (FamilyType familyType in familyManager.Types)
                {
                    string currentName = familyType.Name;
                    string targetName = ApplyRenameRule(currentName, options);
                    if (string.IsNullOrWhiteSpace(targetName) || string.Equals(currentName, targetName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (typeNames.Contains(targetName))
                    {
                        continue;
                    }

                    familyManager.CurrentType = familyType;
                    familyManager.RenameCurrentType(targetName);
                    typeNames.Remove(currentName);
                    typeNames.Add(targetName);
                    renamedTypes++;
                }

                transaction.Commit();

                if (renamedParameters == 0 && renamedTypes == 0)
                {
                    return CreateLog(familyPath, WithoutOpenOperationStatus.Skipped, "Brak zmian do zapisania.", startedAt);
                }

                string savedPath = SaveFamily(document, familyPath, options);
                return CreateLog(familyPath, WithoutOpenOperationStatus.Success, $"Zmieniono parametry: {renamedParameters}, typy: {renamedTypes}. Zapis: {savedPath}", startedAt);
            }
            finally
            {
                if (document != null)
                {
                    _documentService.CloseWithoutSave(document);
                }
            }
        }
        catch (Exception exception)
        {
            return CreateLog(familyPath, WithoutOpenOperationStatus.Failed, exception.Message, startedAt);
        }
    }

    private static string SaveFamily(Document document, string sourcePath, RenameFamilyContentOptions options)
    {
        if (!options.SaveAsCopy)
        {
            document.Save();
            return sourcePath;
        }

        string targetPath = Path.Combine(options.OutputFolderPath, Path.GetFileName(sourcePath));
        SaveAsOptions saveAsOptions = new()
        {
            OverwriteExistingFile = true
        };

        document.SaveAs(targetPath, saveAsOptions);
        return targetPath;
    }

    private static string ApplyRenameRule(string value, RenameFamilyContentOptions options)
    {
        string renamed = value;

        if (!string.IsNullOrEmpty(options.Find))
        {
            renamed = renamed.Replace(options.Find, options.Replace ?? string.Empty);
        }

        renamed = $"{options.Prefix}{renamed}{options.Suffix}";
        return renamed.Trim();
    }

    private static WithoutOpenOperationLogEntry CreateLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt)
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Rename Family Content",
            Status = status,
            Message = message,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}
