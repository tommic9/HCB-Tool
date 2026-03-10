# -*- coding: utf-8 -*-
__title__ = "Level From HVAC Elements"
__doc__ = """Data: 06.2025
_____________________________________________________________________
Opis:
Stabilna wersja skryptu kopiującego poziom (Level lub Reference Level)
z elementów HVAC do wskazanego parametru instancyjnego typu String lub ElementId.

Zabezpieczenia:
- Tylko edytowalne parametry instancyjne
- Maksymalnie 500 elementów
- Bezpieczne operacje Set()

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import ObjectType
from pyrevit import forms, script
from System.Collections.Generic import List as DotNetList

doc = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

# -------------------------------------
# KONFIGURACJA: obsługiwane kategorie i parametry źródłowe
# -------------------------------------
category_level_param_map = {
    BuiltInCategory.OST_DuctTerminal: "Level",
    BuiltInCategory.OST_DuctAccessory: "Level",
    BuiltInCategory.OST_DuctFitting: "Reference Level",
    BuiltInCategory.OST_DuctCurves: "Level",
    BuiltInCategory.OST_FlexDuctCurves: "Reference Level",
    BuiltInCategory.OST_MechanicalEquipment: "Level",
}

supported_bic_ids = set(category_level_param_map.keys())
max_elements_allowed = 500

# -------------------------------------
# FUNKCJE
# -------------------------------------
def get_selected_hvac_elements():
    try:
        refs = uidoc.Selection.PickObjects(ObjectType.Element, "Wybierz elementy HVAC")
        elements = [doc.GetElement(r) for r in refs]
        hvac_elements = []
        for el in elements:
            try:
                bic = el.Category.GetBuiltInCategory()
                if bic in supported_bic_ids:
                    hvac_elements.append(el)
            except:
                continue
        return hvac_elements
    except Exception:
        return []

def get_editable_safe_param_names(element):
    try:
        return sorted([
            p.Definition.Name for p in element.Parameters
            if not p.IsReadOnly and p.StorageType in [StorageType.String, StorageType.ElementId]
        ])
    except:
        return []

def copy_level_to_param_safe(element, target_param_name):
    try:
        bic = element.Category.GetBuiltInCategory()
        source_name = category_level_param_map.get(bic)
        if not source_name:
            return False

        level_param = element.LookupParameter(source_name)
        target_param = element.LookupParameter(target_param_name)

        if not (level_param and target_param and level_param.HasValue and not target_param.IsReadOnly):
            return False

        # Sprawdzenie zgodności StorageType
        if level_param.StorageType != target_param.StorageType:
            return False

        try:
            if level_param.StorageType == StorageType.String:
                target_param.Set(level_param.AsString())
            elif level_param.StorageType == StorageType.ElementId:
                target_param.Set(level_param.AsElementId())
            return True
        except:
            return False
    except:
        return False

# -------------------------------------
# GŁÓWNY BLOK
# -------------------------------------
selected_elements = get_selected_hvac_elements()

if not selected_elements:
    forms.alert("Nie wybrano żadnych elementów HVAC.", title="Błąd")
    script.exit()

if len(selected_elements) > max_elements_allowed:
    forms.alert("Zaznaczono zbyt wiele elementów (>{}).\nPodziel operację na mniejsze grupy.".format(max_elements_allowed), title="Limit elementów")
    script.exit()

# Wybór parametru docelowego
sample = selected_elements[0]
param_names = get_editable_safe_param_names(sample)

if not param_names:
    forms.alert("Nie znaleziono żadnych bezpiecznych parametrów (String lub ElementId).", title="Brak parametrów")
    script.exit()

target_param = forms.SelectFromList.show(
    param_names, title="Wybierz parametr docelowy", multiselect=False, button_name="OK"
)

if not target_param:
    forms.alert("Nie wybrano parametru docelowego. Przerwano.", title="Anulowano")
    script.exit()

# -------------------------------------
# TRANSAKCJA
# -------------------------------------
t = Transaction(doc, "Skopiuj poziom do '{}' (SAFE MODE)".format(target_param))
t.Start()

processed = 0
failed_ids = []

for el in selected_elements:
    if copy_level_to_param_safe(el, target_param):
        processed += 1
    else:
        failed_ids.append(el.Id)

t.Commit()

# Zaznacz elementy z błędami
if failed_ids:
    uidoc.Selection.SetElementIds(DotNetList[ElementId](failed_ids))
    forms.alert(
        "Nie udało się zaktualizować {} elementów.\nZostały zaznaczone w widoku.".format(len(failed_ids)),
        title="Błędy"
    )

# Podsumowanie
forms.alert(
    "Operacja zakończona.\n\nPrzetworzono: {}\nPominięto: {}".format(processed, len(failed_ids)),
    title="Podsumowanie"
)
