# -*- coding: utf-8 -*-
__title__ = "Shared parameters"
__doc__ = """Date = 06.2025
_____________________________________________________________________
Komentarz:
Dodawanie do projektu parametrów współdzielonych z ustawionymi opcjami i kategoriami.

Należy wskazać ścieżkę pliku z parametrami współdzielonymi oraz wybrać grupę parametrów do załadowania, a następnie wskazać konkretne parametry.
"""

from Autodesk.Revit.DB import *
from pyrevit import forms, script
import clr
import sys
import os

# ENV
uidoc = __revit__.ActiveUIDocument
doc = uidoc.Document
app = __revit__.Application
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
    names = []
    it = doc.ParameterBindings.ForwardIterator()
    it.Reset()
    while it.MoveNext():
        definition = it.Key
        if definition:
            names.append(definition.Name)
    return names

def check_missing_params(required_names):
    return [p for p in required_names if p not in get_loaded_param_names()]

def load_params(param_map, sp_file, bind_mode_map):
    if not sp_file:
        forms.alert("Shared parameter file not selected.", title=__title__, exitscript=True)

    for group in sp_file.Groups:
        for p_def in group.Definitions:
            if p_def.Name in param_map:
                logger.info("Attempting to load: {}".format(p_def.Name))
                data = param_map[p_def.Name]

                cat_set = CategorySet()
                for bic in data['bic-cats']:
                    cat = doc.Settings.Categories.get_Item(bic)
                    if cat:
                        cat_set.Insert(cat)

                mode = bind_mode_map.get(p_def.Name, 'instance')
                binding = TypeBinding(cat_set) if mode == 'type' else InstanceBinding(cat_set)

                try:
                    doc.ParameterBindings.Insert(p_def, binding, data['p_group'])
                    logger.info("Loaded parameter: {}".format(p_def.Name))
                except Exception as e:
                    logger.warning("Failed to insert parameter: {}\n{}".format(p_def.Name, str(e)))

def guess_group_by_prefix(name):
    if name.startswith("HC_"):
        return ParamGroup.PG_MECHANICAL if hasattr(ParamGroup, 'PG_MECHANICAL') else ParamGroup.Mechanical
    return ParamGroup.PG_TEXT if hasattr(ParamGroup, 'PG_TEXT') else ParamGroup.Text

def pick_parameter_file():
    return forms.pick_file(file_ext='txt', title="Wskaż plik parametrów współdzielonych")

def pick_parameter_group(sp_file):
    group_names = [g.Name for g in sp_file.Groups]
    selected_group = forms.SelectFromList.show(sorted(group_names), title="Wybierz grupę parametrów")
    if not selected_group:
        forms.alert("Nie wybrano grupy parametrów.", title=__title__, exitscript=True)
    return selected_group

def build_param_map(p_defs):
    param_map = {}
    for p_def in p_defs:
        name = p_def.Name
        param_map[name] = {
            'bic-cats': [
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PlumbingFixtures
            ],
            'p_group': guess_group_by_prefix(name),
            'bind_mode': 'instance'
        }
    return param_map

def select_parameter_definitions(sp_file, selected_group):
    definitions = [p_def for group in sp_file.Groups if group.Name == selected_group for p_def in group.Definitions]
    choices = [p.Name for p in definitions]
    selected_names = forms.SelectFromList.show(sorted(choices), multiselect=True, title="Wybierz parametry do załadowania")
    return [p for p in definitions if p.Name in selected_names] if selected_names else []

def configure_parameters(param_map, missing):
    available_groups = [attr for attr in dir(ParamGroup) if not attr.startswith('__') and not attr.startswith('op_')]
    group_map = {g: getattr(ParamGroup, g) for g in available_groups}

    to_configure = [p for p in missing if not param_map[p].get('p_group') or not param_map[p].get('bic-cats') or not param_map[p].get('bind_mode')]

    if to_configure:
        confirm = forms.alert("Następujące parametry wymagają dodatkowej konfiguracji :\n\n{}\n\nCzy chcesz kontynuować?".format("\n".join(to_configure)), yes=True, no=True)
        if not confirm:
            for pname in to_configure:
                del param_map[pname]
            return

    for pname in to_configure:
        config = param_map[pname]

        default_group_name = next((k for k, v in group_map.items() if v == config.get('p_group')), None)

        selected_group_name = forms.SelectFromList.show(
            available_groups,
            title="Wybierz grupę dla parametru: {}".format(pname),
            default=default_group_name
        )
        if selected_group_name:
            config['p_group'] = group_map[selected_group_name]

        new_binding = forms.SelectFromList.show(['instance', 'type'], title="Typ powiązania dla {}".format(pname), default=config.get('bind_mode', 'instance'))
        config['bind_mode'] = new_binding if new_binding else config.get('bind_mode', 'instance')

# --- MAIN ---

sp_path = pick_parameter_file()
if not sp_path:
    forms.alert("Nie wybrano pliku parametrów współdzielonych.", title=__title__, exitscript=True)

app.SharedParametersFilename = sp_path
sp_file = app.OpenSharedParameterFile()

if not sp_file:
    forms.alert("Nie udało się otworzyć pliku parametrów współdzielonych.", title=__title__, exitscript=True)

selected_group = pick_parameter_group(sp_file)
selected_defs = select_parameter_definitions(sp_file, selected_group)
if not selected_defs:
    forms.alert("Nie wybrano parametrów.", title=__title__, exitscript=True)

param_map = build_param_map(selected_defs)
missing = check_missing_params(list(param_map.keys()))

if not missing:
    forms.alert("Wybrane parametry są już dodane.", title=__title__)
else:
    configure_parameters(param_map, missing)
    bind_mode_map = {k: v.get('bind_mode', 'instance') for k, v in param_map.items()}
    confirmed = forms.alert(
        "Brakuje następujących parametrów:\n\n{}\n\nDodać je?".format("\n".join(missing)),
        title=__title__, yes=True, no=True)
    if confirmed:
        t = Transaction(doc, "Dodawanie parametrów współdzielonych")
        t.Start()
        try:
            filtered_map = {k: v for k, v in param_map.items() if k in missing}
            load_params(filtered_map, sp_file, bind_mode_map)
            logger.info("Dodano brakujące parametry.")
        except Exception as e:
            logger.error("Błąd przy dodawaniu parametrów: {}".format(str(e)))
            t.RollBack()
        else:
            t.Commit()
