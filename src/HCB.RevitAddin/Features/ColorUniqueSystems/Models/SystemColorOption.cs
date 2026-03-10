namespace HCB.RevitAddin.Features.ColorUniqueSystems.Models;

public sealed record SystemColorOption(string SystemName, byte Red, byte Green, byte Blue, bool IsVentilation)
{
    public string DisplayName => SystemName;
}
