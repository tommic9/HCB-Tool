using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.UnloadLinks.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.UnloadLinks;

public sealed class UnloadLinksService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileClassifier _fileClassifier = new();
    private readonly WithoutOpenTransmissionDataService _transmissionDataService = new();
    private readonly WithoutOpenDocumentService _documentService = new();

    public UnloadLinksResult UnloadAllLinks(Application application, IEnumerable<string> filePaths)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(filePaths);
        List<WithoutOpenOperationLogEntry> entries = new(normalizedPaths.Count);

        foreach (string filePath in normalizedPaths)
        {
            if (_fileClassifier.GetFileKind(filePath) != WithoutOpenFileKind.Project)
            {
                entries.Add(new WithoutOpenOperationLogEntry
                {
                    FilePath = filePath,
                    OperationName = "Unload Links",
                    Status = WithoutOpenOperationStatus.Skipped,
                    Message = "Pominieto plik, bo nie jest projektem .rvt."
                });
                continue;
            }

            entries.Add(UnloadProjectReferences(application, filePath));
        }

        return new UnloadLinksResult
        {
            Entries = entries
        };
    }

    private WithoutOpenOperationLogEntry UnloadProjectReferences(Application application, string filePath)
    {
        DateTime startedAt = DateTime.UtcNow;

        WithoutOpenOperationLogEntry pointCloudEntry = RemovePointClouds(application, filePath);
        WithoutOpenOperationLogEntry linkEntry = _transmissionDataService.UnloadAllReferences(filePath);

        WithoutOpenOperationStatus status = MergeStatus(linkEntry.Status, pointCloudEntry.Status);
        string message = $"Linki: {linkEntry.Message} Chmury punktow: {pointCloudEntry.Message}";

        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Unload Links",
            Status = status,
            Message = message,
            Duration = DateTime.UtcNow - startedAt
        };
    }

    private WithoutOpenOperationLogEntry RemovePointClouds(Application application, string filePath)
    {
        DateTime startedAt = DateTime.UtcNow;

        try
        {
            if (_fileClassifier.IsCloudPath(filePath))
            {
                return CreatePointCloudLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto model chmurowy.", startedAt);
            }

            if (!_fileClassifier.IsLocalOrUncPath(filePath))
            {
                return CreatePointCloudLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto sciezke nielokalna.", startedAt);
            }

            if (!File.Exists(filePath))
            {
                return CreatePointCloudLog(filePath, WithoutOpenOperationStatus.Failed, "Plik nie istnieje.", startedAt);
            }

            Document? document = null;
            try
            {
                document = _documentService.OpenDocument(application, filePath);
                List<ElementId> pointCloudIds = new FilteredElementCollector(document)
                    .WhereElementIsNotElementType()
                    .OfClass(typeof(PointCloudInstance))
                    .Select(element => element.Id)
                    .ToList();

                List<ElementId> pointCloudTypeIds = new FilteredElementCollector(document)
                    .WhereElementIsElementType()
                    .OfClass(typeof(PointCloudType))
                    .Select(element => element.Id)
                    .ToList();

                if (pointCloudIds.Count == 0 && pointCloudTypeIds.Count == 0)
                {
                    return CreatePointCloudLog(filePath, WithoutOpenOperationStatus.Skipped, "Nie znaleziono chmur punktow do odpiecia.", startedAt);
                }

                using Transaction transaction = new(document, "Remove Point Clouds");
                transaction.Start();

                if (pointCloudIds.Count > 0)
                {
                    document.Delete(pointCloudIds);
                }

                if (pointCloudTypeIds.Count > 0)
                {
                    document.Delete(pointCloudTypeIds);
                }

                transaction.Commit();
                document.Save();

                return CreatePointCloudLog(
                    filePath,
                    WithoutOpenOperationStatus.Success,
                    $"Usunieto chmury punktow: instancje {pointCloudIds.Count}, typy {pointCloudTypeIds.Count}.",
                    startedAt);
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
            return CreatePointCloudLog(filePath, WithoutOpenOperationStatus.Failed, exception.Message, startedAt);
        }
    }

    private static WithoutOpenOperationStatus MergeStatus(WithoutOpenOperationStatus first, WithoutOpenOperationStatus second)
    {
        if (first == WithoutOpenOperationStatus.Failed || second == WithoutOpenOperationStatus.Failed)
        {
            return WithoutOpenOperationStatus.Failed;
        }

        if (first == WithoutOpenOperationStatus.Success || second == WithoutOpenOperationStatus.Success)
        {
            return WithoutOpenOperationStatus.Success;
        }

        return WithoutOpenOperationStatus.Skipped;
    }

    private static WithoutOpenOperationLogEntry CreatePointCloudLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt)
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Unload Point Clouds",
            Status = status,
            Message = message,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}
