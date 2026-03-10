using System.Collections.Generic;

namespace HCB.RevitAddin.Features.DuctFittingsArea.Models;

public sealed class DuctFittingsAreaResult
{
    public int ProcessedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public List<string> Messages { get; } = new();
}
