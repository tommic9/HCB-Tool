using System.Collections.Generic;

namespace HCB.RevitAddin.Features.HCWireSize.Models;

public sealed class HCWireSizeResult
{
    public int UpdatedCount { get; set; }

    public List<string> Messages { get; } = new();
}
