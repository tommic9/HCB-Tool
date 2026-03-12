using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ColorUniqueSystems.Models;

public sealed class ColorUniqueSystemsResult
{
    public int AppliedCount { get; set; }

    public string ConfigurationSource { get; set; } = string.Empty;

    public bool UsedFallbackPreset { get; set; }

    public List<string> Messages { get; } = new();
}
