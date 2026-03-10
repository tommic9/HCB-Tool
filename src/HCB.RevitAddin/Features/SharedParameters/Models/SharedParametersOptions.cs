using System.Collections.Generic;

namespace HCB.RevitAddin.Features.SharedParameters.Models;

public sealed class SharedParametersOptions
{
    public IReadOnlyList<string> SelectedParameterNames { get; init; } = [];
}
