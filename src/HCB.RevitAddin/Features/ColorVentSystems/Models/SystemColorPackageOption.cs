using System.Collections.Generic;

namespace HCB.RevitAddin.Features.ColorVentSystems.Models;

public sealed record SystemColorPackageOption(string Name, IReadOnlyList<string> SystemNames)
{
    public string DisplayName => Name;
}
