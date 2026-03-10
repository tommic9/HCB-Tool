# -*- coding: utf-8 -*-
__title__ = "Shared parameters"
__doc__ = """Date = 06.2025
_____________________________________________________________________
Komentarz:
Dodawanie do projektu parametrów współdzielonych z ustawionymi opcjami i kategoriami.

Należy wskazać ścieżkę pliku z parametrami współdzielonymi

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

from Autodesk.Revit.DB import *
from pyrevit import forms, script
import clr
clr.AddReference("System")
import sys

# ENV
doc   = __revit__.ActiveUIDocument.Document
app   = __revit__.Application
logger = script.get_logger()

# Revit 2024 i starsze vs 2025+
try:
    from Autodesk.Revit.DB import BuiltInParameterGroup as ParamGroup
except ImportError:
    try:
        from Autodesk.Revit.DB import ParameterGroup as ParamGroup
    except ImportError:
        from Autodesk.Revit.DB import GroupTypeId as ParamGroup

# --- FUNCTIONS ---

def get_loaded_param_names():
    binding_map = doc.ParameterBindings
    it = binding_map.ForwardIterator()
    names = []
    while it.MoveNext():
        names.append(it.Key.Name)
    return names

def check_missing_params(required_names):
    loaded_names = get_loaded_param_names()
    return [p for p in required_names if p not in loaded_names]

def load_params(param_map, sp_file, bind_mode='instance'):
    if not sp_file:
        forms.alert("Shared parameter file not selected.", title=__title__, exitscript=True)

    logger.info("Using Shared Parameter File: {}".format(sp_file.Filename))

    for group in sp_file.Groups:
        for p_def in group.Definitions:
            if p_def.Name in param_map:
                logger.info("Attempting to load: {}".format(p_def.Name))
                data = param_map[p_def.Name]

                # Categories
                cat_set = CategorySet()
                for bic in data['bic-cats']:
                    cat = doc.Settings.Categories.get_Item(bic)
                    if cat: cat_set.Insert(cat)

                # Binding
                binding = TypeBinding(cat_set) if bind_mode == 'type' else InstanceBinding(cat_set)
                try:
                    doc.ParameterBindings.Insert(p_def, binding, data['p_group'])
                    logger.info("Loaded parameter: {}".format(p_def.Name))
                except Exception as e:
                    logger.warning("Failed to insert parameter: {}\n{}".format(p_def.Name, str(e)))

    # Set VaryBetweenGroups
    for defn in get_loaded_param_names():
        try:
            defn_obj = next((d for g in sp_file.Groups for d in g.Definitions if d.Name == defn), None)
            if defn_obj:
                defn_obj.SetAllowVaryBetweenGroups(doc, True)
        except: pass

# --- PARAMETER MAP ---

required_params = {
    'HC_Area': {
        'bic-cats': [BuiltInCategory.OST_DuctFitting, BuiltInCategory.OST_DuctCurves],
        'p_group' : ParamGroup.PG_CONSTRUCTION if hasattr(ParamGroup, 'PG_CONSTRUCTION') else ParamGroup.Construction
    },
    'HC_System': {
        'bic-cats' : [
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_FlexDuctCurves,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctTerminal,
            BuiltInCategory.OST_DuctAccessory,
            BuiltInCategory.OST_MechanicalEquipment,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory,
            BuiltInCategory.OST_FlexPipeCurves,
            BuiltInCategory.OST_PlumbingFixtures,
            BuiltInCategory.OST_PlumbingEquipment],
        'p_group'  : ParamGroup.PG_MECHANICAL if hasattr(ParamGroup, 'PG_MECHANICAL') else ParamGroup.Mechanical
    },
    'HC_Duct_System':{
        'bic-cats' :[
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_FlexDuctCurves,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctTerminal,
            BuiltInCategory.OST_DuctAccessory,
            BuiltInCategory.OST_MechanicalEquipment],
        'p_group'  : ParamGroup.PG_MECHANICAL if hasattr(ParamGroup, 'PG_MECHANICAL') else ParamGroup.Mechanical
    },
    'HC_Piping_System':{
        'bic-cats' : [
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory,
            BuiltInCategory.OST_FlexPipeCurves,
            BuiltInCategory.OST_PlumbingFixtures,
            BuiltInCategory.OST_PlumbingEquipment,
            BuiltInCategory.OST_MechanicalEquipment],
        'p_group'  : ParamGroup.PG_MECHANICAL if hasattr(ParamGroup, 'PG_MECHANICAL') else ParamGroup.Mechanical
    },
    'HC_Etap': {
        'bic-cats' : [BuiltInCategory.OST_ProjectInformation, BuiltInCategory.OST_Sheets],
        'p_group'  : ParamGroup.PG_TEXT if hasattr(ParamGroup, 'PG_TEXT') else ParamGroup.Text
    },
    'HC_Branża': {
        'bic-cats' : [BuiltInCategory.OST_ProjectInformation, BuiltInCategory.OST_Sheets],
        'p_group'  : ParamGroup.PG_TEXT if hasattr(ParamGroup, 'PG_TEXT') else ParamGroup.Text
    }
}

# --- MAIN ---

missing = check_missing_params(list(required_params.keys()))

if not missing:
    forms.alert("All required shared parameters already loaded.", title=__title__)
else:
    # Select file
    sp_path = forms.pick_file(file_ext='txt', title="Wskaż plik parametrów współdzielonych")
    if not sp_path:
        forms.alert("Nie wybrano pliku parametrów współdzielonych.", title=__title__, exitscript=True)

    # Ustaw ścieżkę jako string i otwórz ręcznie plik przez aplikację
    appType = clr.GetClrType(app.__class__)
    sp_filename_prop = appType.GetProperty("SharedParametersFilename")
    sp_filename_prop.SetValue(app, sp_path, None)

    sp_file = app.OpenSharedParameterFile()

    confirmed = forms.alert(
        "Brakuje następujących parametrów:\n\n{}\n\nCzy chcesz je załadować?".format("\n".join(missing)),
        title=__title__, yes=True, no=True)

    if confirmed:
        t = Transaction(doc, "Dodawanie parametrów współdzielonych")
        t.Start()
        try:
            filtered_map = {k: v for k, v in required_params.items() if k in missing}
            load_params(filtered_map, sp_file, bind_mode='instance')
            logger.info("Dodano brakujące parametry.")
        except Exception as e:
            logger.error("Błąd przy dodawaniu parametrów: {}".format(str(e)))
            t.RollBack()
        else:
            t.Commit()
