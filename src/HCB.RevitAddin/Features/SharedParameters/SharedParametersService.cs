using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using HCB.RevitAddin.Features.SharedParameters.Models;

namespace HCB.RevitAddin.Features.SharedParameters;

public sealed class SharedParametersService
{
    private static readonly IReadOnlyDictionary<string, SharedParameterSpec> RequiredParameters = new Dictionary<string, SharedParameterSpec>
    {
        ["HC_Area"] = new([BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctCurves], GroupTypeId.Construction),
        ["HC_System"] = new([BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves, BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_FlexPipeCurves, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_PlumbingEquipment], GroupTypeId.Mechanical),
        ["HC_Duct_System"] = new([BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves, BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_MechanicalEquipment], GroupTypeId.Mechanical),
        ["HC_Piping_System"] = new([BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_FlexPipeCurves, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_PlumbingEquipment, BuiltInCategory.OST_MechanicalEquipment], GroupTypeId.Mechanical),
        ["HC_Do_Zam\u00F3wienia"] = new([BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves, BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_FlexPipeCurves, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_PlumbingEquipment, BuiltInCategory.OST_ElectricalFixtures], GroupTypeId.IdentityData),
        ["HC_Zam\u00F3wione"] = new([BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_FlexDuctCurves, BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctTerminal, BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_FlexPipeCurves, BuiltInCategory.OST_PlumbingFixtures, BuiltInCategory.OST_PlumbingEquipment, BuiltInCategory.OST_ElectricalFixtures], GroupTypeId.IdentityData),
        ["HC_Etap"] = new([BuiltInCategory.OST_ProjectInformation], GroupTypeId.Text),
        ["HC_Bran\u017Ca"] = new([BuiltInCategory.OST_ProjectInformation], GroupTypeId.Text)
    };

    public IReadOnlyList<string> GetMissingParameters(Document document)
    {
        HashSet<string> loadedNames = new(GetLoadedDefinitions(document).Select(definition => definition.Name));
        return RequiredParameters.Keys.Where(parameter => !loadedNames.Contains(parameter)).ToList();
    }

    public string? GetSharedParameterFilePath(Application application)
    {
        return application.OpenSharedParameterFile()?.Filename;
    }

    public SharedParametersResult LoadMissingParameters(Document document, Application application, IEnumerable<string> missingParameters)
    {
        DefinitionFile sharedParameterFile = application.OpenSharedParameterFile();
        SharedParametersResult result = new();

        if (sharedParameterFile == null)
        {
            result.Messages.Add("Nie znaleziono pliku Shared Parameters ustawionego w Revit.");
            return result;
        }

        List<string> missing = missingParameters
            .Where(parameter => RequiredParameters.ContainsKey(parameter))
            .Distinct()
            .ToList();

        result.MissingCount = missing.Count;

        using Transaction transaction = new(document, "Add Shared Parameters");
        transaction.Start();

        foreach (DefinitionGroup group in sharedParameterFile.Groups)
        {
            foreach (Definition definition in group.Definitions)
            {
                if (!missing.Contains(definition.Name))
                {
                    continue;
                }

                SharedParameterSpec spec = RequiredParameters[definition.Name];
                CategorySet categorySet = application.Create.NewCategorySet();
                foreach (BuiltInCategory category in spec.Categories)
                {
                    categorySet.Insert(document.Settings.Categories.get_Item(category));
                }

                Binding binding = application.Create.NewInstanceBinding(categorySet);
                if (document.ParameterBindings.Insert(definition, binding, spec.ParameterGroup))
                {
                    result.LoadedCount++;
                    result.Messages.Add($"Dodano parametr: {definition.Name}");
                }
                else
                {
                    result.Messages.Add($"Parametr juz istnieje lub nie mogl zostac dodany: {definition.Name}");
                }
            }
        }

        transaction.Commit();
        return result;
    }

    private static IEnumerable<Definition> GetLoadedDefinitions(Document document)
    {
        BindingMap map = document.ParameterBindings;
        DefinitionBindingMapIterator iterator = map.ForwardIterator();
        iterator.Reset();

        while (iterator.MoveNext())
        {
            if (iterator.Key != null)
            {
                yield return iterator.Key;
            }
        }
    }
}

public sealed record SharedParameterSpec(IReadOnlyList<BuiltInCategory> Categories, ForgeTypeId ParameterGroup);
