# -*- coding: utf-8 -*-
__title__ = "Accessory&Terminals Numbering"  # krótka nazwa
__doc__ = """Numeruje akcesoria (DuctAccessory)
oraz nawiewniki/wywiewniki (AirTerminals) w Revit 2024+.

- Używa skrótu systemu MEP (parametr typu "Abbreviation") jako prefiksu numeru.
- Wynik trafia do parametru instancji "LIN_POSITION_NUMBER_A" w formacie
  "ABBR.N" lub "N" gdy brak skrótu.
- Działa na zaznaczeniu; przy braku selekcji obejmuje wszystkie:
  DuctAccessory i AirTerminals w projekcie.
- Każdy skrót systemu numerowany jest osobno.

Grupowanie:
- DuctAccessory: Size + TypeName.
- AirTerminals: Size + TypeName + LIN_VE_AIRFLOWRATE.

Pominięte:
- elementy, których parametr typu Manufacturer = "FabricAir".
"""

import sys
import clr

from Autodesk.Revit.DB import (BuiltInCategory, BuiltInParameter,
                               FilteredElementCollector, StorageType,
                               Transaction, TransactionStatus, UnitUtils)
from Autodesk.Revit.UI import TaskDialog

# Jednostki (głównie pod przepływ / rezerwę do ewentualnych długości)
try:
    from Autodesk.Revit.DB import UnitTypeId
    MM_UNIT = UnitTypeId.Millimeters
    DEG_UNIT = UnitTypeId.Degrees
except ImportError:  # starsze API
    from Autodesk.Revit.DB import DisplayUnitType
    MM_UNIT = DisplayUnitType.DUT_MILLIMETERS
    DEG_UNIT = DisplayUnitType.DUT_DECIMAL_DEGREES

uidoc = __revit__.ActiveUIDocument
if uidoc is None:
    raise Exception("Brak aktywnego dokumentu Revit.")

doc = uidoc.Document


# ---------------------------------------------------------------------------
# Funkcje pomocnicze
# ---------------------------------------------------------------------------

def normalize_text(value):
    return value.strip() if value else ""


def round_value(value, precision):
    if value is None:
        return None
    return round(value / precision) * precision


def get_param_value(elem, name):
    """Zwraca wartość parametru instancji (tekst lub liczba)."""
    param = elem.LookupParameter(name)
    if param is None or not param.HasValue:
        return None
    if param.StorageType == StorageType.String:
        return param.AsString()
    value_string = param.AsValueString()
    return value_string if value_string not in (None, "") else param.AsDouble()


def get_text(elem, name):
    value = get_param_value(elem, name)
    if value is None:
        return ""
    return normalize_text(str(value))


def get_flow(elem, name="LIN_VE_AIRFLOWRATE", precision=1.0):
    """Pobiera przepływ dla nawiewników (AirTerminals)."""
    param = elem.LookupParameter(name)
    if param is None or not param.HasValue:
        return None

    if param.StorageType == StorageType.Double:
        val = param.AsDouble()
        return round_value(val, precision)
    else:
        return normalize_text(param.AsString())

def get_typemark(elem):
    """Zwraca Type Mark z instancji lub typu (ALL_MODEL_TYPE_MARK)."""
    try:
        # najpierw parametr instancji (jeśli jest)
        p = elem.LookupParameter("Type Mark")
        if p is not None and p.HasValue:
            return normalize_text(p.AsString())
    except Exception:
        pass

    try:
        # potem parametr typu
        symbol = doc.GetElement(elem.GetTypeId())
        if symbol is None:
            return ""
        p = symbol.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_MARK)
        if p is not None and p.HasValue:
            return normalize_text(p.AsString())
    except Exception:
        pass

    return ""



def get_system_abbr(element):
    """Skrót systemu MEP (Abbreviation) dla elementu."""
    # 1. Próba z parametru instancji
    try:
        inst_param = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM)
    except Exception:
        inst_param = None

    if inst_param is not None and inst_param.HasValue:
        return normalize_text(inst_param.AsString())

    # 2. Fallback – MEPSystem
    system = getattr(element, "MEPSystem", None)
    if system is None:
        # 3. Fallback – złącza
        mep_model = getattr(element, "MEPModel", None)
        conn_manager = getattr(mep_model, "ConnectorManager", None) if mep_model else None
        if conn_manager:
            systems = set()
            for c in conn_manager.Connectors:
                try:
                    s = c.MEPSystem
                except Exception:
                    s = None
                if s:
                    systems.add(s)
            if systems:
                system = list(systems)[0]

    if system is None:
        return ""

    type_id = system.GetTypeId()
    if type_id.IntegerValue == -1:
        return ""

    sys_type = doc.GetElement(type_id)
    if sys_type is None:
        return ""

    abbr_param = sys_type.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM)
    if abbr_param is None or not abbr_param.HasValue:
        return ""

    return normalize_text(abbr_param.AsString())


def is_fabricair(elem):
    """Czy element ma w typie Manufacturer == 'FabricAir'."""
    try:
        symbol = doc.GetElement(elem.GetTypeId())
        if symbol is None:
            return False
        manuf_param = symbol.get_Parameter(BuiltInParameter.ALL_MODEL_MANUFACTURER)
        if manuf_param is None or not manuf_param.HasValue:
            return False
        manu_val = normalize_text(manuf_param.AsString())
        return manu_val.lower() == "fabricair"
    except Exception:
        return False


def should_skip_element(elem):
    """Warunki pominięcia elementu w numeracji."""
    if elem is None:
        return True
    if is_fabricair(elem):
        return True
    return False


def get_type_name(elem):
    """Pobiera nazwę typu (Type Name)."""
    try:
        symbol = doc.GetElement(elem.GetTypeId())
        if symbol is not None:
            return normalize_text(symbol.Name)
    except Exception:
        pass
    # fallback
    try:
        return normalize_text(elem.Name)
    except Exception:
        return ""


# ---------------------------------------------------------------------------
# Klucze grupujące
# ---------------------------------------------------------------------------

def build_accessory_key(element):
    """DuctAccessory – wg Size + TypeName."""
    size = get_text(element, "Size")
    tname = get_type_name(element)
    return (size, tname)


def build_airterminal_key(element):
    """AirTerminal – wg Size + TypeName + LIN_VE_AIRFLOWRATE."""
    size = get_text(element, "Size")
    tname = get_type_name(element)
    airflow = get_flow(element, "LIN_VE_AIRFLOWRATE")
    return (size, tname, airflow)


# ---------------------------------------------------------------------------
# Zbieranie i grupowanie elementów
# ---------------------------------------------------------------------------

def collect_targets():
    """Zwraca listę elementów do numeracji (po filtrach)."""
    selected_ids = list(uidoc.Selection.GetElementIds())
    allowed_cats = (
        int(BuiltInCategory.OST_DuctAccessory),
        int(BuiltInCategory.OST_DuctTerminal),  # AirTerminals
    )

    if selected_ids:
        elems = [doc.GetElement(eid) for eid in selected_ids]
        filtered = [
            e for e in elems
            if e is not None
            and e.Category
            and e.Category.Id.IntegerValue in allowed_cats
            and not should_skip_element(e)
        ]
        return filtered

    accessories = (FilteredElementCollector(doc)
                   .OfCategory(BuiltInCategory.OST_DuctAccessory)
                   .WhereElementIsNotElementType()
                   .ToElements())
    terminals = (FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_DuctTerminal)
                 .WhereElementIsNotElementType()
                 .ToElements())

    accessories = [a for a in accessories if not should_skip_element(a)]
    terminals = [t for t in terminals if not should_skip_element(t)]

    return list(accessories) + list(terminals)


def group_elements(elements):
    """Grupuje elementy wg systemu oraz kategorii."""
    groups = {}
    for elem in elements:
        if elem is None or elem.Category is None:
            continue

        sys_abbr = get_system_abbr(elem)
        sys_group = groups.setdefault(
            sys_abbr,
            {"accessories": {}, "terminals": {}}
        )

        cat_id = elem.Category.Id.IntegerValue

        if cat_id == int(BuiltInCategory.OST_DuctAccessory):
            key = build_accessory_key(elem)
            sys_group["accessories"].setdefault(key, []).append(elem)

        elif cat_id == int(BuiltInCategory.OST_DuctTerminal):
            key = build_airterminal_key(elem)
            sys_group["terminals"].setdefault(key, []).append(elem)

    return groups


def sorted_keys(keys):
    """Sortowanie kluczy z obsługą None i stringów."""
    none_safe = lambda x: ("" if x is None else x) if isinstance(x, basestring) else (-1 if x is None else x)
    return sorted(keys, key=lambda k: tuple(none_safe(part) for part in k))


def format_number(prefix, number):
    return "%s.%s" % (prefix, number) if prefix else str(number)

def format_accessory_number(sys_abbr, number, typemark):
    """Numer dla DuctAccessory: [ABBR].[TypeMark].N / [ABBR].N / [TypeMark].N / N."""
    parts = []
    if sys_abbr:
        parts.append(sys_abbr)
    if typemark:
        parts.append(typemark)
    if parts:
        return "%s.%s" % (".".join(parts), number)
    return str(number)


def format_terminal_number(number):
    """Numer dla AirTerminals: AT.N (bez względu na system)."""
    return "AT.%s" % number



# ---------------------------------------------------------------------------
# Numeracja
# ---------------------------------------------------------------------------

def set_position_number(element, value):
    param = element.LookupParameter("LIN_POSITION_NUMBER_A")
    if param is None or param.IsReadOnly:
        return
    try:
        param.Set(value)
    except Exception:
        # Cicho pomijamy błędy zapisu
        pass


def assign_numbers(groups):
    t = Transaction(doc, __title__)
    t.Start()
    stats = {
        "systems": len(groups),
        "accessories": 0,
        "terminals": 0,
        "total": 0,
        "same_number": 0,
    }
    try:
        for sys_abbr in sorted(groups.keys()):
            data = groups[sys_abbr]
            current = 1

            # Akcesoria
            for key in sorted_keys(data["accessories"].keys()):
                elems = data["accessories"][key]
                if not elems:
                    continue
                typemark = get_typemark(elems[0])
                value = format_accessory_number(sys_abbr, current, typemark)
                for elem in elems:
                    set_position_number(elem, value)
                stats["accessories"] += len(elems)
                stats["total"] += len(elems)
                if len(elems) > 1:
                    stats["same_number"] += len(elems) - 1
                current += 1

            # Nawiewniki/wywiewniki
            for key in sorted_keys(data["terminals"].keys()):
                elems = data["terminals"][key]
                if not elems:
                    continue
                value = format_terminal_number(current)
                for elem in elems:
                    set_position_number(elem, value)
                stats["terminals"] += len(elems)
                stats["total"] += len(elems)
                if len(elems) > 1:
                    stats["same_number"] += len(elems) - 1
                current += 1


    except Exception:
        try:
            t.RollBack()
        except Exception:
            pass
        raise

    status = t.Commit()
    return stats, status


# ---------------------------------------------------------------------------
# Główna funkcja
# ---------------------------------------------------------------------------

def main():
    elements = collect_targets()
    if not elements:
        TaskDialog.Show(__title__, "Brak elementów do numeracji (lub wszystkie zostały pominięte).")
        return

    grouped = group_elements(elements)
    if not grouped:
        TaskDialog.Show(__title__, "Brak elementów do numeracji po grupowaniu.")
        return

    stats, status = assign_numbers(grouped)

    summary = (
        "Ponumerowano: {total}\n"
        "Akcesoria: {accessories}\n"
        "Nawiewniki/Wywiewniki: {terminals}\n"
        "Elementów współdzielących numer: {same_number}\n"
        "Systemy: {systems}\n"
        "Pominięto: Manufacturer = 'FabricAir'."
    ).format(**stats)

    summary_message = summary
    if status != TransactionStatus.Committed:
        summary_message += "\nTransakcja zwróciła status: %s" % status

    TaskDialog.Show(__title__, summary_message)


if __name__ == "__main__":
    main()
