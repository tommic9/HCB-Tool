using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace HCB.RevitAddin.Infrastructure.WithoutOpen;

public sealed class WithoutOpenDocumentService
{
    public Document OpenDocument(Application application, string filePath)
    {
        return application.OpenDocumentFile(filePath);
    }

    public void CloseWithoutSave(Document document)
    {
        if (document != null && document.IsValidObject)
        {
            document.Close(false);
        }
    }
}
