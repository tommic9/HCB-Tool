using System;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenTransmissionDataService
{
    private readonly WithoutOpenFileClassifier _fileClassifier = new();

    public WithoutOpenOperationLogEntry UnloadAllReferences(string filePath)
    {
        DateTime startedAt = DateTime.UtcNow;

        try
        {
            if (_fileClassifier.IsCloudPath(filePath))
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto model chmurowy.", startedAt);
            }

            if (!_fileClassifier.IsLocalOrUncPath(filePath))
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Pominieto sciezke nielokalna.", startedAt);
            }

            if (!File.Exists(filePath))
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Failed, "Plik nie istnieje.", startedAt);
            }

            ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
            TransmissionData? transmissionData = TransmissionData.ReadTransmissionData(modelPath);
            if (transmissionData == null)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Failed, "Nie udalo sie odczytac TransmissionData.", startedAt);
            }

            int changedReferences = 0;
            foreach (ElementId referenceId in transmissionData.GetAllExternalFileReferenceIds())
            {
                ExternalFileReference reference = transmissionData.GetLastSavedReferenceData(referenceId);
                if (reference == null)
                {
                    continue;
                }

                transmissionData.SetDesiredReferenceData(referenceId, reference.GetPath(), reference.PathType, false);
                changedReferences++;
            }

            if (changedReferences == 0)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Nie znaleziono zewnetrznych referencji do odlaczenia.", startedAt);
            }

            TransmissionData.WriteTransmissionData(modelPath, transmissionData);
            return CreateLog(filePath, WithoutOpenOperationStatus.Success, $"Odlaczono referencje: {changedReferences}.", startedAt);
        }
        catch (Exception exception)
        {
            return CreateLog(filePath, WithoutOpenOperationStatus.Failed, exception.Message, startedAt);
        }
    }

    private static WithoutOpenOperationLogEntry CreateLog(string filePath, WithoutOpenOperationStatus status, string message, DateTime startedAt)
    {
        return new WithoutOpenOperationLogEntry
        {
            FilePath = filePath,
            OperationName = "Unload Links",
            Status = status,
            Message = message,
            Duration = DateTime.UtcNow - startedAt
        };
    }
}

