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

        HashSet<string> selectedTargets = options.TargetParameterKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool restrictToTargets = selectedTargets.Count > 0;

        List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);
        foreach (string familyPath in normalizedPaths)
        {
            entries.Add(RenameSingle(application, familyPath, options, selectedTargets, restrictToTargets));
        }

        return new RenameFamilyContentResult
        {
            Entries = entries
        };
    }

    private WithoutOpenOperationLogEntry RenameSingle(
        Application application,
        string familyPath,
        RenameFamilyContentOptions options,
        IReadOnlySet<string> selectedTargets,
        bool restrictToTargets)
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
                int protectedParameters = 0;
                int skippedBySelection = 0;

                using Transaction transaction = new(document, "Rename Family Parameters");
                transaction.Start();

                HashSet<string> parameterNames = familyManager.Parameters
                    .Cast<FamilyParameter>()
                    .Select(parameter => parameter.Definition?.Name ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (FamilyParameter parameter in familyManager.Parameters)
                {
                    string currentName = parameter.Definition?.Name ?? string.Empty;
                    if (restrictToTargets && !selectedTargets.Contains(BuildTargetKey(familyPath, currentName)))
                    {
                        skippedBySelection++;
                        continue;
                    }

                    if (!CanRenameParameter(parameter))
                    {
                        protectedParameters++;
                        continue;
                    }

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

                transaction.Commit();

                if (renamedParameters == 0)
                {
                    string message = protectedParameters > 0
                        ? $"Brak zmian do zapisania. Pominieto parametry chronione: {protectedParameters}."
                        : "Brak zmian do zapisania.";

                    if (restrictToTargets)
                    {
                        message += $" Poza zakresem zaznaczenia: {skippedBySelection}.";
                    }

                    return CreateLog(familyPath, WithoutOpenOperationStatus.Skipped, message, startedAt);
                }

                string savedPath = SaveFamily(document, familyPath, options);
                string successMessage = $"Zmieniono parametry: {renamedParameters}. Pominieto chronione: {protectedParameters}.";
                if (restrictToTargets)
                {
                    successMessage += $" Poza zakresem zaznaczenia: {skippedBySelection}.";
                }

                successMessage += $" Zapis: {savedPath}";
                return CreateLog(familyPath, WithoutOpenOperationStatus.Success, successMessage, startedAt, savedPath);
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

    public static string BuildTargetKey(string filePath, string parameterName)
    {
        return $"{filePath}|{parameterName}";
    }

    private static bool CanRenameParameter(FamilyParameter parameter)
    {
        return !parameter.IsShared && !IsBuiltInParameter(parameter);
    }

    private static bool IsBuiltInParameter(FamilyParameter parameter)
    {
        return parameter.Definition is InternalDefinition internalDefinition &&
               internalDefinition.BuiltInParameter != BuiltInParameter.INVALID;
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

    private static WithoutOpenOperationLogEntry CreateLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt, string outputPath = "")
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Rename Family Parameters",
            Status = status,
            Message = message,
            OutputPath = outputPath,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}
