using HCB.RevitAddin.Features.UnloadLinks.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.UnloadLinks;

public sealed class UnloadLinksService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileClassifier _fileClassifier = new();
    private readonly WithoutOpenTransmissionDataService _transmissionDataService = new();

    public UnloadLinksResult UnloadAllLinks(IEnumerable<string> filePaths)
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

            entries.Add(_transmissionDataService.UnloadAllReferences(filePath));
        }

        return new UnloadLinksResult
        {
            Entries = entries
        };
    }
}

