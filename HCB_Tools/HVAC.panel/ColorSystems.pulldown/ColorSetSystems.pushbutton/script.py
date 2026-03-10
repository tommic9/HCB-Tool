# -*- coding: utf-8 -*-
__title__ = "Color Set Systems"
__doc__ = """Date  = 06.2025 
_____________________________________________________________________
Komentarz:
Skrypt umożliwia dodanie pakietów filtrów dla systemów kanałów i rur oraz nadpisywanie ich kolorem wypełnienia wg schematu lub pakietów.

Jak korzystać:
-Wybierz pakiety które chcesz dodać do widoku

Uwaga
Filtry które nie istnieją w projekcie zostaną dodane. istniejące zostaną nadpisane

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
from pyrevit import forms
import clr
clr.AddReference("System")
from System.Collections.Generic import List

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ==================================================
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

ventilation_system_colors = {
    'V_Supply air (Nawiew)':        (0, 128, 255),
    'V_Outdoor air (Czerpny)':      (64, 192, 0),
    'V_Extract air (Wywiew)':       (255, 255, 0),
    'V_Exhaust air (Wyrzut)':       (192, 128, 0),
}

piping_system_colors = {
    'CHW_Supply':                   (0, 92, 255),
    'CHW_Return':                   (0, 255, 255),
    'HP_Supply':                    (128, 64, 128),
    'HP_Return':                    (255, 64, 128),
    'HW_Supply':                    (255, 0, 0),
    'HW_Return':                    (0, 0, 128),
    'HWX_Supply':                   (192, 0, 0),
    'HWX_Return':                   (0, 64, 192),
    'WU_Circulation':               (128, 0, 255),
    'WU_Cold Water':                (0, 255, 0),
    'WU_Hot Water':                 (255, 64, 0),
    'FP_Fire Protection':           (255, 164, 8),
    'PH_Supply':                    (240, 0, 150),
    'PH_Return':                    (180, 0, 192),
    'SW_Sewage Water':              (128, 92, 64),
    'SWC_Sewage Water Condensat':   (128, 128, 92),
    'SWV_Sewage Water Ventilation': (164, 128, 64),
    'RWP_Rainwater Pressure':       (0, 128, 164),
    'RWG_Rainwater Gravity':        (128, 164, 255),
    'RWE_Rainwater Emergency':      (192, 128, 192),
    'VRV_Return':                   (0, 92, 192),
    'VRV_Supply':                   (0, 192, 192),
    'REF_Supply':                   (24, 128, 200),
    'REF_Return':                   (24, 192, 222),
    'SPL_Supply':                   (255, 164, 164),
    'SPL_Return':                   (255, 128, 164),
}

system_packages = {
    "WENTYLACJA ALL": list(ventilation_system_colors.keys()),
    "Chłodzenie (CHW+VRV+REF+HP)": ['REF_Supply', 'REF_Return', 'VRV_Supply', 'VRV_Return', 'CHW_Supply', 'CHW_Return', 'HP_Supply', 'HP_Return'],
    "Grzanie (HW+HWX+HP)": ['HW_Supply', 'HW_Return', 'HWX_Supply', 'HWX_Return', 'HP_Supply', 'HP_Return'],
    "Podłogówka (PH)": ['PH_Supply', 'PH_Return'],
    "Grzanie/chłodzenie (CHW+VRV+REF+HP+HW+HWX)": ['HW_Supply', 'HW_Return', 'HWX_Supply', 'HWX_Return', 'HP_Supply', 'HP_Return', 'CHW_Supply', 'CHW_Return', 'REF_Supply', 'REF_Return', 'VRV_Supply', 'VRV_Return'],
    "Kanaliza (SW)": ['SW_Sewage Water', 'SWC_Sewage Water Condensat', 'SWV_Sewage Water Ventilation'],
    "Woda użytkowa (WU)": ['WU_Circulation', 'WU_Cold Water', 'WU_Hot Water'],
    "Deszczóka (RW)": ['RWP_Rainwater Pressure', 'RWG_Rainwater Gravity', 'RWE_Rainwater Emergency'],
    "Ppoż (FP)": ['FP_Fire Protection'],
    "Sanitarne (WU+SW)": ['WU_Circulation', 'WU_Cold Water', 'WU_Hot Water', 'SW_Sewage Water', 'SWC_Sewage Water Condensat', 'SWV_Sewage Water Ventilation'],
    "RUROWE ALL": list(piping_system_colors.keys()),
    "WSZYSTKIE ALL": list(ventilation_system_colors.keys()) + list(piping_system_colors.keys()),
}

# MAIN
# ==================================================

def create_view_filters_and_set_overrides(doc, view, selected_systems, system_colors, is_ventilation):
    if not view.AreGraphicsOverridesAllowed():
        return

    solid_fill_pattern_id = next(
        (fp.Id for fp in FilteredElementCollector(doc).OfClass(FillPatternElement)
         if fp.Name == "<Solid fill>"),
        ElementId.InvalidElementId
    )

    applied_filter_ids = view.GetFilters()

    for system_name in sorted(selected_systems):
        color_rgb = system_colors.get(system_name)
        if not color_rgb:
            continue

        color = Color(*color_rgb)
        ogs = OverrideGraphicSettings()
        ogs.SetSurfaceForegroundPatternColor(color)
        ogs.SetSurfaceForegroundPatternId(solid_fill_pattern_id)

        if is_ventilation:
            param_id = ElementId(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM)
            categories = List[ElementId]([
                ElementId(BuiltInCategory.OST_DuctCurves),
                ElementId(BuiltInCategory.OST_DuctFitting),
                ElementId(BuiltInCategory.OST_DuctAccessory),
                ElementId(BuiltInCategory.OST_DuctTerminal),
                ElementId(BuiltInCategory.OST_FlexDuctCurves),
            ])
            rule_value = system_name.split("(")[0].strip()
        else:
            param_id = ElementId(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM)
            categories = List[ElementId]([
                ElementId(BuiltInCategory.OST_PipeCurves),
                ElementId(BuiltInCategory.OST_PipeFitting),
                ElementId(BuiltInCategory.OST_PipeAccessory),
                ElementId(BuiltInCategory.OST_PlumbingFixtures),
            ])
            rule_value = system_name

        rule = ParameterFilterRuleFactory.CreateContainsRule(param_id, rule_value, False)
        element_filter = ElementParameterFilter(rule)

        existing_filter = next(
            (f for f in FilteredElementCollector(doc).OfClass(ParameterFilterElement) if f.Name == system_name),
            None
        )

        if existing_filter:
            filter_elem = existing_filter
            filter_elem.SetElementFilter(element_filter)
        else:
            filter_elem = ParameterFilterElement.Create(doc, system_name, categories, element_filter)

        if filter_elem.Id not in applied_filter_ids:
            view.AddFilter(filter_elem.Id)

        view.SetFilterOverrides(filter_elem.Id, ogs)

view = doc.ActiveView

selected_packages = forms.SelectFromList.show(
    sorted(system_packages.keys()),
    title="Wybierz pakiety systemów do pokolorowania",
    multiselect=True,
    button_name="Zastosuj"
)

if not selected_packages:
    forms.alert("Nie wybrano pakietów.", title="Anulowano")
else:
    selected_systems = []
    for package in selected_packages:
        selected_systems.extend(system_packages[package])

    t = Transaction(doc, "Nadpisanie systemów")
    t.Start()
    try:
        vent_systems = [s for s in selected_systems if s in ventilation_system_colors]
        pipe_systems = [s for s in selected_systems if s in piping_system_colors]

        if vent_systems:
            create_view_filters_and_set_overrides(doc, view, vent_systems, ventilation_system_colors, True)
        if pipe_systems:
            create_view_filters_and_set_overrides(doc, view, pipe_systems, piping_system_colors, False)

        t.Commit()
    except:
        t.RollBack()
        raise
