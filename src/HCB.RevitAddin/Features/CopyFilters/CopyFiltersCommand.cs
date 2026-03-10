using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HCB.RevitAddin.Features.CopyFilters.UI;

namespace HCB.RevitAddin.Features.CopyFilters
{
    [Transaction(TransactionMode.Manual)]
    public class CopyFiltersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            CopyFiltersWindow window = new CopyFiltersWindow(commandData.Application);
            bool? dialogResult = window.ShowDialog();

            if (dialogResult != true)
            {
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }
    }
}
