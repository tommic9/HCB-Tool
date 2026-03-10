# -*- coding: utf-8 -*-
__title__ = "System Assigner"
__doc__ = """
Data: 06.2025
Skrypt do Revit 2025 (pyRevit, Python 2.7),
pozwalający na automatyczne nadpisanie wartości parametru współdzielonego 'HC_System'
wszystkim elementom w rozgałęzionym systemie MEP, na podstawie wybranych urządzeń (MechanicalEquipment).

Dla systemów wentylacyjnych pobiera numer z cyfrowej części parametru SystemName i tworzy nazwę w formacie 'AHUXX'.
Dla systemów rurowych pobiera prefix z listy system_names do znaku podkreślenia (np. 'CHW', 'HWX').

Jak korzystać:
- Wybierz urządzenia MEP (MechanicalEquipment), które są początkiem systemu
- Kliknij przycisk uruchamiający skrypt
- Wszystkim połączonym elementom zostanie nadpisany parametr HC_System wg powyższych reguł

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
import re
from pyrevit import script

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ CONFIG & VARIABLES
# ==================================================
system_names = [
    'V_Supply air (Nawiew)',
    'V_Outdoor air (Czerpny)',
    'V_Extract air (Wywiew)',
    'V_Exhaust air (Wyrzut)',
    'CHW_Supply',
    'CHW_Return',
    'HP_Supply',
    'HP_Return',
    'HW_Supply',
    'HW_Return',
    'HWX_Supply (HW Exchange)',
    'HWX_Return (HW Exchange)',
    'WU_Circulation',
    'WU_Cold Water',
    'WU_Hot Water',
    'FP_Fire Protection',
    'PH_Supply',
    'PH_Return',
    'SW_Sewage Water',
    'SWC_Sewage Water Condensat',
    'SWV_Sewage Water Ventilation',
    'RWP_Rainwater Pressure',
    'RWG_Rainwater Gravity',
    'RWE_Rainwater Emergency',
    'VRV_Return',
    'VRV_Supply',
    'REF_Supply',
    'REF_Return',
    'SPL_Supply',
    'SPL_Return',
]

doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ FUNKCJE NARZĘDZIOWE
# ==================================================
def get_system_type_and_name(mep_system):
    """Zwraca tuple: (SystemType, SystemName)"""
    sys_type = ""
    sys_name = ""
    try:
        sys_type = mep_system.SystemType.ToString() if hasattr(mep_system, 'SystemType') else ""
        sys_name = mep_system.Name if hasattr(mep_system, 'Name') else ""
    except Exception:
        pass
    return (sys_type, sys_name)

def get_HC_System_value(sys_type, sys_name):
    """Zwraca wartość, jaką należy ustawić w HC_System dla podanego systemu"""
    ventilation_types = [n for n in system_names if n.startswith("V_")]
    if any([sys_type.startswith(v.split(" ")[0]) for v in ventilation_types]):
        digits = re.findall(r"\d+", sys_name)
        number = digits[0] if digits else ""
        return "AHU" + number
    else:
        prefix_match = re.match(r"([A-Z]+)_", sys_type)
        return prefix_match.group(1) if prefix_match else sys_type

def set_shared_param(element, param_name, value):
    """Ustawia wartość parametru współdzielonego elementu (jeśli istnieje)"""
    for param in element.Parameters:
        if param.Definition.Name == param_name:
            param.Set(value)
            return True
    return False

# ╔═╗╦ ╦╔═╗╦ ╦╦╔═ MAIN + DEBUG
# ==================================================

# Filtr wyboru tylko MechanicalEquipment
class MechanicalEquipmentSelectionFilter(ISelectionFilter):
    def AllowElement(self, element):
        return element.Category and element.Category.Id.IntegerValue == int(BuiltInCategory.OST_MechanicalEquipment)
    def AllowReference(self, ref, point):
        return True

selection = uidoc.Selection
picked_refs = selection.PickObjects(
    ObjectType.Element,
    MechanicalEquipmentSelectionFilter(),
    "Wybierz urządzenia MEP (MechanicalEquipment)"
)

missing_param_ids = []
debug_lines = []

# --- NOWE PODEJŚCIE: pobieraj systemy przypisane do urządzenia (AssignedSystems) ---
def get_assigned_mep_systems(element):
    """Zwraca listę systemów przypisanych do urządzenia (dla MEPModel)."""
    try:
        mep_model = element.MEPModel
        if hasattr(mep_model, "AssignedSystems"):
            return list(mep_model.AssignedSystems)
    except Exception:
        pass
    return []

with Transaction(doc, "Ustaw HC_System") as t:
    t.Start()
    changed_count = 0
    for picked_ref in picked_refs:
        eq = doc.GetElement(picked_ref.ElementId)
        assigned_systems = get_assigned_mep_systems(eq)
        if not assigned_systems:
            debug_lines.append("UWAGA: Brak przypisanych systemów dla urządzenia ID {}".format(eq.Id))
            continue
        for mep_system in assigned_systems:
            sys_type, sys_name = get_system_type_and_name(mep_system)
            hc_value = get_HC_System_value(sys_type, sys_name)
            debug_lines.append(
                u"Urządzenie ID: {} | SystemType: {} | SystemName: {} | Wartość HC_System: {}".format(
                    eq.Id, sys_type, sys_name, hc_value
                )
            )
            # Pobierz wszystkie elementy systemu przez .Elements
            elements = list(mep_system.Elements)
            for elem in elements:
                cat_name = elem.Category.Name if elem.Category else "Brak kategorii"
                result = set_shared_param(elem, "HC_System", hc_value)
                debug_lines.append(
                    u"  Element ID: {} | Kategoria: {} | HC_System: {} | {}".format(
                        elem.Id, cat_name, hc_value, "USTAWIONO" if result else "BRAK PARAMETRU"
                    )
                )
                if result:
                    changed_count += 1
                else:
                    missing_param_ids.append((elem.Id, cat_name))
    t.Commit()

# Wyświetl wynik w oknie Output pyRevit
debug = script.get_output()
debug.print_md("### HC_System – debug log\n")
for line in debug_lines:
    debug.print_md(line)
debug.print_md("\n---")
debug.print_md("Ustawiono HC_System dla **{}** elementów.".format(changed_count))
if missing_param_ids:
    debug.print_md("Elementy **bez parametru HC_System** (ID, kategoria):")
    for mid, cat in missing_param_ids:
        debug.print_md("- {} ({})".format(mid, cat))
else:
    debug.print_md("Wszystkie elementy miały parametr HC_System.")
