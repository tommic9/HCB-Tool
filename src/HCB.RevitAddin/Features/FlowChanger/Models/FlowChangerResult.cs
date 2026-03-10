using System.Collections.Generic;

namespace HCB.RevitAddin.Features.FlowChanger.Models;

public sealed class FlowChangerResult
{
    public int ProcessedCount { get; set; }

    public int UpdatedCount { get; set; }

    public List<string> Messages { get; } = new();
}
