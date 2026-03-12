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

            int foundSupportedReferences = 0;
            int changedReferences = 0;
            foreach (ElementId referenceId in transmissionData.GetAllExternalFileReferenceIds())
            {
                ExternalFileReference reference = transmissionData.GetLastSavedReferenceData(referenceId);
                if (reference == null || !CanUnloadReference(reference))
                {
                    continue;
                }

                foundSupportedReferences++;
                transmissionData.SetDesiredReferenceData(referenceId, reference.GetPath(), reference.PathType, false);
                changedReferences++;
            }

            if (foundSupportedReferences == 0)
            {
                return CreateLog(filePath, WithoutOpenOperationStatus.Skipped, "Nie znaleziono obslugiwanych linkow do odlaczenia.", startedAt);
            }

            transmissionData.IsTransmitted = true;
            TransmissionData.WriteTransmissionData(modelPath, transmissionData);

            return CreateLog(
                filePath,
                WithoutOpenOperationStatus.Success,
                $"Odlaczono linki: {changedReferences}. Model oznaczono jako transmitted, wiec przy kolejnym otwarciu Revit odczyta nowe ustawienia linkow.",
                startedAt);
        }
        catch (Exception exception)
        {
            return CreateLog(filePath, WithoutOpenOperationStatus.Failed, exception.Message, startedAt);
        }
    }

    private static bool CanUnloadReference(ExternalFileReference reference)
    {
        return reference.ExternalFileReferenceType == ExternalFileReferenceType.RevitLink ||
               reference.ExternalFileReferenceType == ExternalFileReferenceType.CADLink;
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
