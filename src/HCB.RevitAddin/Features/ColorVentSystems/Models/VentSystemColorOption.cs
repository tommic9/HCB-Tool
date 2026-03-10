namespace HCB.RevitAddin.Features.ColorVentSystems.Models;

public sealed record VentSystemColorOption(string SystemName, byte Red, byte Green, byte Blue)
{
    public string DisplayName => SystemName;
}
