using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin;

public sealed class AlwaysAvailableCommandAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
        return true;
    }
}
