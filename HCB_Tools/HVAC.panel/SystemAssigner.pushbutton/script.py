# -*- coding: utf-8 -*-
__title__ = "System Assigner"
__doc__ = """
Data: 06.2025
Skrypt do Revit 2025 (pyRevit, Python 2.7),
pozwalający na automatyczne nadpisanie wartości parametru współdzielonego 'HC_System'
wszystkim elementom w rozgałęzionym systemie MEP, kopiując tę wartość z wybranego urządzenia (MechanicalEquipment).

Jak korzystać:
- Wybierz urządzenia MEP (MechanicalEquipment), które są początkiem systemu
- Ustaw oczekiwaną wartość w parametrze HC_System na urządzeniu
- Kliknij przycisk uruchamiający skrypt
- Wszystkim połączonym elementom zostanie nadpisany parametr HC_System wartością z urządzenia

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import *
from pyrevit import script

doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ FUNKCJE NARZĘDZIOWE
# ==================================================
def get_param_value(element, param_name):
    """Zwraca wartość parametru o podanej nazwie dla elementu, lub None jeśli brak."""
    for param in element.Parameters:
        if param.Definition.Name == param_name:
            return param.AsString() if param.StorageType == StorageType.String else param.AsValueString()
    return None

def set_shared_param(element, param_name, value):
    """Ustawia wartość parametru współdzielonego elementu (jeśli istnieje)"""
    for param in element.Parameters:
        if param.Definition.Name == param_name:
            param.Set(value)
            return True
    return False

def get_assigned_mep_systems(element):
    """Zwraca listę systemów przypisanych do urządzenia (dla MEPModel)."""
    try:
        mep_model = element.MEPModel
        if hasattr(mep_model, "AssignedSystems"):
            return list(mep_model.AssignedSystems)
    except Exception:
        pass
    return []

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

with Transaction(doc, "Ustaw HC_System") as t:
    t.Start()
    changed_count = 0
    for picked_ref in picked_refs:
        eq = doc.GetElement(picked_ref.ElementId)
        # Pobierz wartość HC_System z urządzenia
        hc_value = get_param_value(eq, "HC_System")
        if not hc_value:
            debug_lines.append("UWAGA: Urządzenie ID {} nie ma ustawionego parametru HC_System lub jest pusty.".format(eq.Id))
            continue
        assigned_systems = get_assigned_mep_systems(eq)
        if not assigned_systems:
            debug_lines.append("UWAGA: Brak przypisanych systemów dla urządzenia ID {}".format(eq.Id))
            continue
        debug_lines.append(
            u"Urządzenie ID: {} | Wartość HC_System do kopiowania: {}".format(eq.Id, hc_value)
        )
        for mep_system in assigned_systems:
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
