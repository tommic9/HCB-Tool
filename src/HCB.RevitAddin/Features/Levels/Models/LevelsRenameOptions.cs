namespace HCB.RevitAddin.Features.Levels.Models;

public sealed class LevelsRenameOptions
{
    public int DecimalPlaces { get; init; } = 2;

    public bool ShowPlusForPositiveValues { get; init; } = true;
}
