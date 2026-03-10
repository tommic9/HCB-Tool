using System.Collections.Generic;

namespace HCB.RevitAddin.Features.PurgeAnnotations.Models;

public sealed class PurgeAnnotationsResult
{
    public int DeletedCount { get; set; }

    public int FailedCount { get; set; }

    public List<string> Messages { get; } = new();
}
