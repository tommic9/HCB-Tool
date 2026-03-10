using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ColorVentSystems.Models;

public sealed class ColorVentSystemsResult
{
    public int ProcessedSystemsCount { get; set; }

    public List<string> Messages { get; } = new();
}
