namespace HCB.RevitAddin.UI.Controls;

public sealed class SelectionListItem
{
    public SelectionListItem(
        object value,
        string displayName,
        string? filterGroup = null,
        string? secondaryFilterGroup = null)
    {
        Value = value;
        DisplayName = displayName;
        FilterGroup = filterGroup ?? string.Empty;
        SecondaryFilterGroup = secondaryFilterGroup ?? string.Empty;
    }

    public object Value { get; }

    public string DisplayName { get; }

    public string FilterGroup { get; }

    public string SecondaryFilterGroup { get; }
}
