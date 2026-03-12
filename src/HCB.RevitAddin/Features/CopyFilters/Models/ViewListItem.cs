using Autodesk.Revit.DB;

namespace HCB.RevitAddin.Features.CopyFilters.Models
{
    internal sealed class ViewListItem
    {
        public ViewListItem(View view)
        {
            View = view;
        }

        public View View { get; }

        public ElementId Id => View.Id;

        public string ViewTypeName => View.ViewType.ToString();

        public string ItemTypeName => View.IsTemplate ? "Template" : "View";

        public string DisplayName => View.IsTemplate
            ? $"{View.Name} [Template]"
            : View.Name;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
