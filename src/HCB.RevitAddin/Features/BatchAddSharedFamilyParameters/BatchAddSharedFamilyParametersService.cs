using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.BatchAddSharedFamilyParameters.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.BatchAddSharedFamilyParameters;

public sealed class BatchAddSharedFamilyParametersService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileClassifier _fileClassifier = new();
    private readonly WithoutOpenDocumentService _documentService = new();

    public IReadOnlyList<SharedParameterDefinitionItem> LoadDefinitions(Application application, string sharedParameterFilePath)
    {
        string originalPath = application.SharedParametersFilename;
        application.SharedParametersFilename = sharedParameterFilePath;

        try
        {
            DefinitionFile? definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                return [];
            }

            List<SharedParameterDefinitionItem> items = [];
            foreach (DefinitionGroup group in definitionFile.Groups)
            {
                foreach (Definition definition in group.Definitions)
                {
                    items.Add(new SharedParameterDefinitionItem
                    {
                        Name = definition.Name,
                        GroupName = group.Name
                    });
                }
            }

            return items
                .OrderBy(item => item.GroupName)
                .ThenBy(item => item.Name)
                .ToList();
        }
        finally
        {
            application.SharedParametersFilename = originalPath;
        }
    }

    public IReadOnlyList<FamilyParameterGroupOption> GetGroupOptions()
    {
        return
        [
            new FamilyParameterGroupOption { Key = "Data", Label = "Data", GroupTypeId = GroupTypeId.Data },
            new FamilyParameterGroupOption { Key = "Text", Label = "Text", GroupTypeId = GroupTypeId.Text },
            new FamilyParameterGroupOption { Key = "IdentityData", Label = "Identity Data", GroupTypeId = GroupTypeId.IdentityData },
            new FamilyParameterGroupOption { Key = "Construction", Label = "Construction", GroupTypeId = GroupTypeId.Construction },
            new FamilyParameterGroupOption { Key = "Dimensions", Label = "Dimensions", GroupTypeId = GroupTypeId.Geometry },
            new FamilyParameterGroupOption { Key = "Materials", Label = "Materials", GroupTypeId = GroupTypeId.Materials },
            new FamilyParameterGroupOption { Key = "Mechanical", Label = "Mechanical", GroupTypeId = GroupTypeId.Mechanical },
            new FamilyParameterGroupOption { Key = "Electrical", Label = "Electrical", GroupTypeId = GroupTypeId.Electrical },
            new FamilyParameterGroupOption { Key = "Plumbing", Label = "Plumbing", GroupTypeId = GroupTypeId.Plumbing }
        ];
    }

    public BatchAddSharedFamilyParametersResult AddParameters(Application application, IEnumerable<string> familyPaths, string sharedParameterFilePath, BatchAddSharedFamilyParametersOptions options)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(familyPaths);
        IReadOnlyList<FamilyParameterGroupOption> groupOptions = GetGroupOptions();
        FamilyParameterGroupOption groupOption = groupOptions.FirstOrDefault(option => option.Key == options.GroupKey) ?? groupOptions[0];

        string originalPath = application.SharedParametersFilename;
        application.SharedParametersFilename = sharedParameterFilePath;

        try
        {
            DefinitionFile? definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                return new BatchAddSharedFamilyParametersResult
                {
                    Entries =
                    [
                        new WithoutOpenOperationLogEntry
                        {
                            FilePath = sharedParameterFilePath,
                            OperationName = "Add Shared Parameters",
                            Status = WithoutOpenOperationStatus.Failed,
                            Message = "Nie udalo sie otworzyc wskazanego pliku Shared Parameters."
                        }
                    ]
                };
            }

            Dictionary<string, ExternalDefinition> selectedDefinitions = GetDefinitions(definitionFile, options.SelectedParameterNames);
            List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);
            foreach (string familyPath in normalizedPaths)
            {
                entries.Add(AddToSingleFamily(application, familyPath, selectedDefinitions, groupOption.GroupTypeId, options.IsInstance));
            }

            return new BatchAddSharedFamilyParametersResult
            {
                Entries = entries
            };
        }
        finally
        {
            application.SharedParametersFilename = originalPath;
        }
    }

    private WithoutOpenOperationLogEntry AddToSingleFamily(Application application, string familyPath, IReadOnlyDictionary<string, ExternalDefinition> selectedDefinitions, ForgeTypeId groupTypeId, bool isInstance)
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
                HashSet<string> existingNames = familyManager.Parameters
                    .Cast<FamilyParameter>()
                    .Select(parameter => parameter.Definition?.Name ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                int addedCount = 0;
                using Transaction transaction = new(document, "Add Shared Family Parameters");
                transaction.Start();

                foreach ((string parameterName, ExternalDefinition definition) in selectedDefinitions)
                {
                    if (existingNames.Contains(parameterName))
                    {
                        continue;
                    }

                    familyManager.AddParameter(definition, groupTypeId, isInstance);
                    existingNames.Add(parameterName);
                    addedCount++;
                }

                transaction.Commit();

                if (addedCount == 0)
                {
                    return CreateLog(familyPath, WithoutOpenOperationStatus.Skipped, "Brak nowych parametrow do dodania.", startedAt);
                }

                document.Save();
                return CreateLog(familyPath, WithoutOpenOperationStatus.Success, $"Dodano parametry: {addedCount}.", startedAt);
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

    private static Dictionary<string, ExternalDefinition> GetDefinitions(DefinitionFile definitionFile, IEnumerable<string> selectedNames)
    {
        HashSet<string> names = selectedNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, ExternalDefinition> definitions = new(StringComparer.OrdinalIgnoreCase);

        foreach (DefinitionGroup group in definitionFile.Groups)
        {
            foreach (Definition definition in group.Definitions)
            {
                if (definition is ExternalDefinition externalDefinition && names.Contains(definition.Name))
                {
                    definitions[definition.Name] = externalDefinition;
                }
            }
        }

        return definitions;
    }

    private static WithoutOpenOperationLogEntry CreateLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt)
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Add Shared Parameters",
            Status = status,
            Message = message,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}

