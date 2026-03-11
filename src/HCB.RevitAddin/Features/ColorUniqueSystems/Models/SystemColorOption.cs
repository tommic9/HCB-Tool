namespace HCB.RevitAddin.Features.ColorUniqueSystems.Models;

public sealed record SystemColorOption(
    string SystemName,
    byte Red,
    byte Green,
    byte Blue,
    bool IsVentilation,
    string DisplayName = "",
    string GroupName = "",
    string FilterValue = "")
{
    public string DisplayLabel => string.IsNullOrWhiteSpace(DisplayName) ? SystemName : DisplayName;

    public string FilterGroup => string.IsNullOrWhiteSpace(GroupName) ? "Inne" : GroupName;

    public string EffectiveFilterValue => string.IsNullOrWhiteSpace(FilterValue) ? SystemName : FilterValue;
}
