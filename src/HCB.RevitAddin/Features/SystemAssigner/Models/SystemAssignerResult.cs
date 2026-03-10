using System.Collections.Generic;

namespace HCB.RevitAddin.Features.SystemAssigner.Models;

public sealed class SystemAssignerResult
{
    public int ProcessedEquipmentCount { get; set; }

    public int ProcessedSystemCount { get; set; }

    public int ChangedCount { get; set; }

    public List<string> Messages { get; } = new();
}
