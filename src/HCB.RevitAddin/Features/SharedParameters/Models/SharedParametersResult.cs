using System.Collections.Generic;

namespace HCB.RevitAddin.Features.SharedParameters.Models;

public sealed class SharedParametersResult
{
    public int MissingCount { get; set; }

    public int LoadedCount { get; set; }

    public List<string> Messages { get; } = new();
}
