using Autodesk.Revit.DB;

namespace HCB.RevitAddin.Features._Template;

public sealed class FeatureService
{
    private readonly Document _document;

    public FeatureService(Document document)
    {
        _document = document;
    }

    public string GetPlaceholderMessage()
    {
        return $"Replace template code for document '{_document.Title}'.";
    }
}
