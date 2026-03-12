using System.Collections.Generic;
using System.Linq;
using HCB.RevitAddin.Infrastructure.WithoutOpen.Models;

namespace HCB.RevitAddin.Features.BatchFileScan.Models;

public sealed class BatchFileScanResult
{
    public IReadOnlyList<WithoutOpenFileScanItem> Items { get; init; } = [];

    public int SuccessCount => Items.Count(item => item.Status == WithoutOpenOperationStatus.Success);

    public int SkippedCount => Items.Count(item => item.Status == WithoutOpenOperationStatus.Skipped);

    public int FailedCount => Items.Count(item => item.Status == WithoutOpenOperationStatus.Failed);
}

