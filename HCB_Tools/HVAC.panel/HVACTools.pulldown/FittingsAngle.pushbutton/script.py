# -*- coding: utf-8 -*-
__title__ = "Fittings Angle"
__doc__ = """Data: 23.06.2025
_____________________________________________________________________
Komentarz:
Skrypt kopiuje wartości predefiniowanych parametrów oraz dodatkowego parametru użytkownika kątów kształtek
do parametru współdzielonego HC_Kąt.

Dla prostokątnych kształtek kanałowych (DuctFitting) zaokrąglenie jest wykonywane do 1 stopnia,
dla pozostałych elementów do 5 stopni.

Jeśli masz wybrane elementy, skrypt skopiuje wartości tylko dla zaznaczonych elementów.

Jeśli nic nie zaznaczysz, skrypt skopiuje wartości dla wszystkich elementów w projekcie
z wybranych kategorii.

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
import math
from Autodesk.Revit.DB import (
    FilteredElementCollector, BuiltInCategory, Transaction, TransactionStatus,
    ElementCategoryFilter, LogicalOrFilter, ElementFilter,
    ConnectorProfileType, ElementId
)
from pyrevit import forms, script
import clr
clr.AddReference("System")
from System.Collections.Generic import List as DotNetList

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ==================================================
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument

TARGET_SHARED_PARAM_NAME = "HC_Kąt"
PREDEFINED_ANGLE_PARAMS = ["Angle", "angle", "w", "arc", "RSen_P_c01_angle"]

CATEGORY_OPTIONS = {
    "Pipe Fittings": BuiltInCategory.OST_PipeFitting,
    "Duct Fittings": BuiltInCategory.OST_DuctFitting,
    "Cable Tray Fittings": BuiltInCategory.OST_CableTrayFitting,
    "Conduit Fittings": BuiltInCategory.OST_ConduitFitting,
}

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ FUNCTIONS
# ==================================================
def get_angle_value_from_element(element, param_names):
    for param_name in param_names:
        param = element.LookupParameter(param_name)
        if param and param.HasValue:
            try:
                return param.AsDouble()
            except Exception as e:
                pass
    return None

def convert_round_and_revert_angle(radians_value, step_degrees=5.0):
    if radians_value is None: return None
    degrees = math.degrees(radians_value)
    rounded_degrees = round(degrees / step_degrees) * step_degrees
    return math.radians(rounded_degrees)

def is_rectangular_duct_fitting(element):
    if element.Category.Id.IntegerValue == int(BuiltInCategory.OST_DuctFitting):
        try:
            connectors = element.MEPModel.ConnectorManager.Connectors
            if connectors.Size > 0:
                return all(c.Shape == ConnectorProfileType.Rectangular for c in connectors)
        except Exception as e:
            pass
    return False

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ MAIN
# ==================================================
initial_selection = uidoc.Selection.GetElementIds()
has_selection = initial_selection and initial_selection.Count > 0

sorted_category_names = sorted(CATEGORY_OPTIONS.keys())
selected_names = forms.SelectFromList.show(
    sorted_category_names, title="Wybierz kategorie do przetworzenia",
    multiselect=True, button_name="Wybierz"
)
if not selected_names:
    forms.alert("Nie wybrano kategorii. Skrypt zostanie przerwany.", title="Anulowano")
    script.exit()

selected_bics = [CATEGORY_OPTIONS[name] for name in selected_names]

user_param = forms.ask_for_string(
    default="", prompt="Opcjonalnie: Podaj dodatkową nazwę parametru kąta:",
    title="Dodatkowy parametr kąta"
)

angle_params = []
if user_param and user_param.strip():
    angle_params.append(user_param.strip())
angle_params.extend(PREDEFINED_ANGLE_PARAMS)
angle_params = list(dict.fromkeys(angle_params))

processed_count = 0
updated_count = 0
source_not_found = 0
target_missing_or_readonly = 0
elements_with_set_error = []  # <= dla selekcji po błędach
all_candidates = []

# ╔═╗╔═╗╔╦╗╔═╗╔╦╗╦ ╦╔═╗╦═╗
# ╚═╗║╣  ║ ║╣  ║ ╚╦╝║╣ ╠╦╝
# ╚═╝╚═╝ ╩ ╚═╝ ╩  ╩ ╚═╝╩╚═ TRANSACTION
# ==================================================
t = Transaction(doc, "Kopiuj i zaokrągl kąt do HC_Kąt")
try:
    t.Start()

    raw_elements = []
    if has_selection:
        cat_filters = DotNetList[ElementFilter]()
        for bic in selected_bics:
            cat_filters.Add(ElementCategoryFilter(bic))
        combined_filter = cat_filters[0] if cat_filters.Count == 1 else LogicalOrFilter(cat_filters)
        collector = FilteredElementCollector(doc, initial_selection).WherePasses(combined_filter).WhereElementIsNotElementType()
        raw_elements = list(collector)
    else:
        for bic in selected_bics:
            collector = FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType()
            raw_elements.extend(collector)

    unique_elements = {el.Id.IntegerValue: el for el in raw_elements}.values()
    all_candidates = [el for el in unique_elements if get_angle_value_from_element(el, angle_params) is not None]

    if not all_candidates:
        forms.alert("Nie znaleziono elementów z parametrami kąta.", title="Brak danych")
        t.RollBack()
        script.exit()

    has_target_param = any(el.LookupParameter(TARGET_SHARED_PARAM_NAME) for el in all_candidates)
    if not has_target_param:
        forms.alert(
            "Nie znaleziono parametru '{}' w żadnym z przetwarzanych elementów.\n\n"
            "Upewnij się, że parametr współdzielony został dodany do projektu.".format(TARGET_SHARED_PARAM_NAME),
            title="Brak parametru docelowego"
        )
        t.RollBack()
        script.exit()

    for el in all_candidates:
        processed_count += 1
        source_val = get_angle_value_from_element(el, angle_params)
        if source_val is not None:
            step = 1.0 if is_rectangular_duct_fitting(el) else 5.0
            rounded_val = convert_round_and_revert_angle(source_val, step)

            if rounded_val is not None:
                target_param = el.LookupParameter(TARGET_SHARED_PARAM_NAME)
                if target_param and not target_param.IsReadOnly:
                    try:
                        target_param.Set(rounded_val)
                        updated_count += 1
                    except Exception as e:
                        target_missing_or_readonly += 1
                        elements_with_set_error.append(el.Id)
                else:
                    target_missing_or_readonly += 1
                    elements_with_set_error.append(el.Id)
            else:
                source_not_found += 1
        else:
            source_not_found += 1

    if updated_count > 0:
        t.Commit()
    else:
        t.RollBack()

except Exception as e:
    if t.HasStarted() and t.GetStatus() == TransactionStatus.Started:
        t.RollBack()
    forms.alert("Wystąpił błąd: {}. Zmiany nie zostały zapisane.".format(e), title="Błąd skryptu")

# ╔═╗╔═╗╔═╗╔╦╗╦═╗╔═╗╔═╗╦═╗╦ ╦╔═╗╔╦╗╔═╗
# ╠═╣╚═╗║╣  ║ ╠╦╝║╣ ║ ╦╠╦╝║ ║║ ║ ║ ║╣
# ╩ ╩╚═╝╚═╝ ╩ ╩╚═╚═╝╚═╝╩╚═╚═╝╚═╝ ╩ ╚═╝ SUMMARY
# ==================================================
if elements_with_set_error:
    uidoc.Selection.SetElementIds(DotNetList[ElementId](elements_with_set_error))
    forms.alert(
        "Niektóre elementy nie mogły zostać zaktualizowane (brak lub zablokowany parametr '{}').\n"
        "Zostały zaznaczone w widoku.".format(TARGET_SHARED_PARAM_NAME),
        title="Błędy zapisu"
    )

summary = "\n".join([
    "Ukończono.", "",
    "Wykonano dla: {}".format(", ".join(selected_names)),
    "Elementy wybrano według: {}".format("Zaznaczonych" if has_selection else "Wszystkich elementów z projektu"),
    "W sumie elementów: {}".format(len(all_candidates)),
    "Zaktualizowano: {}".format(updated_count),
    "Nie znaleziono parametrów źródłowych dla: {}".format(source_not_found),
    "Brak lub parametr tylko do odczytu ({}): {}".format(TARGET_SHARED_PARAM_NAME, target_missing_or_readonly)
])
forms.alert(summary, title="Podsumowanie operacji")
