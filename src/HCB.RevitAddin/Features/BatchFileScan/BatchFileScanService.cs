using System.Collections.Generic;
using HCB.RevitAddin.Features.BatchFileScan.Models;
using HCB.RevitAddin.Infrastructure.WithoutOpen;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.BatchFileScan;

public sealed class BatchFileScanService
{
    private readonly WithoutOpenFileDiscoveryService _fileDiscoveryService = new();
    private readonly WithoutOpenFileMetadataService _fileMetadataService = new();

    public BatchFileScanResult Scan(IEnumerable<string> filePaths)
    {
        IReadOnlyList<string> normalizedPaths = _fileDiscoveryService.Normalize(filePaths);
        List<WithoutOpenFileScanItem> items = new(normalizedPaths.Count);

        foreach (string filePath in normalizedPaths)
        {
            items.Add(_fileMetadataService.Scan(filePath));
        }

        return new BatchFileScanResult
        {
            Items = items
        };
    }
}

