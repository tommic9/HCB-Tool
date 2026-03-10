# -*- coding: utf-8 -*-
__title__ = "Duct&Fitting Numbering"  # krótka nazwa
__doc__ = """Numeruje kanały (Duct) i kształtki (DuctFitting) w Revit 2024+.
- Używa skrótu systemu MEP (parametr typu "Abbreviation") jako prefiksu numeru.
- Wynik trafia do parametru instancji "LIN_POSITION_NUMBER_A" w formacie
  "ABBR.N" lub "N" gdy brak skrótu.
- Działa na zaznaczeniu; przy braku selekcji obejmuje wszystkie Duct i
  DuctFitting w projekcie.
- Każdy skrót systemu numerowany jest osobno; identyczne elementy (rozmiar i
  długość dla Duct; rozmiar, typ i wymiary LIN_VE_* dla DuctFitting) dostają
  wspólny numer.
"""

import sys
import clr

from Autodesk.Revit.DB import (BuiltInCategory, BuiltInParameter,
                               FilteredElementCollector, StorageType,
                               Transaction, TransactionStatus, UnitUtils)
from Autodesk.Revit.UI import TaskDialog
from pyrevit import forms, script

# Revit 2024+ API / starsze wersje
try:
    from Autodesk.Revit.DB import UnitTypeId
    MM_UNIT = UnitTypeId.Millimeters
    DEG_UNIT = UnitTypeId.Degrees
except ImportError:  # pragma: no cover
    from Autodesk.Revit.DB import DisplayUnitType
    MM_UNIT = DisplayUnitType.DUT_MILLIMETERS
    DEG_UNIT = DisplayUnitType.DUT_DECIMAL_DEGREES

# Parametr długości dla kanałów (ustawiany przez użytkownika w UI)
DUCT_LENGTH_PARAM = "HC_Order_Length"

# Czy w projekcie istnieje parametr "Powiększona długość" na kanałach
HAS_EXTENDED_LENGTH_PARAM = False

uidoc = __revit__.ActiveUIDocument
if uidoc is None:
    raise Exception("Brak aktywnego dokumentu Revit.")

doc = uidoc.Document

# Ścieżka do pliku XAML w bundle
XAML_FILE = script.get_bundle_file('DuctNumeration.xaml')


# ---------------------------------------------------------------------------
# Klasa okna WPF
# ---------------------------------------------------------------------------

class DuctNumerationWindow(forms.WPFWindow):
    def __init__(self):
        forms.WPFWindow.__init__(self, XAML_FILE)
        # wynik wyboru: "HC_Order_Length" / "Length" / "Powiększona długość" / None
        self.length_param = None

        # Obsługa dostępności parametru "Powiększona długość"
        global HAS_EXTENDED_LENGTH_PARAM
        cb_ext = self.FindName("checkbox_extended")
        if cb_ext is not None and not HAS_EXTENDED_LENGTH_PARAM:
            cb_ext.IsEnabled = False
            cb_ext.IsChecked = False
            cb_ext.Content = "Powiększona długość (parametr projektu niedostępny)"

    # Zdarzenia z XAML

    def header_drag(self, sender, args):
        """Pozwala przeciągać okno za nagłówek."""
        try:
            self.DragMove()
        except:
            pass

    def button_close(self, sender, args):
        self.length_param = None
        self.Close()

    def button_cancel(self, sender, args):
        self.length_param = None
        self.Close()

    def button_ok(self, sender, args):
        cb_len = self.FindName("checkbox_length")
        cb_hc = self.FindName("checkbox_hc")
        cb_ext = self.FindName("checkbox_extended")

        # Kolejność priorytetu: Powiększona długość -> HC_Order_Length -> Length
        if cb_ext is not None and cb_ext.IsEnabled and cb_ext.IsChecked:
            self.length_param = "Powiększona długość"
        elif cb_hc is not None and cb_hc.IsChecked:
            self.length_param = "HC_Order_Length"
        else:
            self.length_param = "Length"

        self.Close()

    def checkbox_length_checked(self, sender, args):
        """Zachowanie jak radio – zaznaczenie Length odznacza pozostałe."""
        cb_hc = self.FindName("checkbox_hc")
        cb_ext = self.FindName("checkbox_extended")
        if cb_hc is not None and cb_hc.IsChecked:
            cb_hc.IsChecked = False
        if cb_ext is not None and cb_ext.IsChecked:
            cb_ext.IsChecked = False

    def checkbox_hc_checked(self, sender, args):
        """Zachowanie jak radio – zaznaczenie HC_Order_Length odznacza pozostałe."""
        cb_len = self.FindName("checkbox_length")
        cb_ext = self.FindName("checkbox_extended")
        if cb_len is not None and cb_len.IsChecked:
            cb_len.IsChecked = False
        if cb_ext is not None and cb_ext.IsChecked:
            cb_ext.IsChecked = False

    def checkbox_extended_checked(self, sender, args):
        """Zachowanie jak radio – zaznaczenie Powiększona długość odznacza pozostałe."""
        cb_len = self.FindName("checkbox_length")
        cb_hc = self.FindName("checkbox_hc")
        if cb_len is not None and cb_len.IsChecked:
            cb_len.IsChecked = False
        if cb_hc is not None and cb_hc.IsChecked:
            cb_hc.IsChecked = False

    def Hyperlink_RequestNavigate(self, sender, args):
        try:
            import System
            System.Diagnostics.Process.Start(str(args.Uri))
            args.Handled = True
        except:
            pass


# ---------------------------------------------------------------------------
# Funkcje pomocnicze – bez logów do konsoli
# ---------------------------------------------------------------------------

def normalize_text(value):
    return value.strip() if value else ""


def round_value(value, precision):
    if value is None:
        return None
    return round(value / precision) * precision


def get_param_value(elem, name):
    param = elem.LookupParameter(name)
    if param is None or not param.HasValue:
        return None
    if param.StorageType == StorageType.String:
        return param.AsString()
    value_string = param.AsValueString()
    return value_string if value_string not in (None, "") else param.AsDouble()


def get_Parameter(elem, param_id):
    """Wrapper dla get_Parameter w pyRevit"""
    return elem.get_Parameter(param_id)


def get_text(elem, name):
    value = get_param_value(elem, name)
    if value is None:
        return ""
    return normalize_text(str(value))


def convert_length(param_value):
    if param_value is None:
        return None
    try:
        return UnitUtils.ConvertFromInternalUnits(param_value, MM_UNIT)
    except Exception:
        return None


def get_length(elem, name, precision=1.0):
    param = elem.LookupParameter(name)
    if param is None or not param.HasValue:
        return None
    length_mm = convert_length(param.AsDouble())
    return round_value(length_mm, precision) if length_mm is not None else None


def get_angle(elem, name, precision=0.1):
    param = elem.LookupParameter(name)
    if param is None or not param.HasValue:
        return None
    try:
        angle_deg = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), DEG_UNIT)
    except Exception:
        return None
    return round_value(angle_deg, precision)


def get_system_abbr(element):
    # 1. Spróbuj bezpośrednio z instancji (Duct / DuctFitting, Revit 2024+)
    try:
        inst_param = element.get_Parameter(BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM)
    except Exception:
        inst_param = None

    if inst_param is not None and inst_param.HasValue:
        return normalize_text(inst_param.AsString())

    # 2. Fallback – MEPSystem
    system = getattr(element, "MEPSystem", None)
    if system is None:
        # 3. Dodatkowy fallback – przez złącza (ważne dla kształtek)
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


def is_flange_l_family(elem):
    """Czy element należy do rodziny L_Flange_RV."""
    try:
        symbol = doc.GetElement(elem.GetTypeId())
        if symbol is None:
            return False
        family = symbol.Family
        fam_name = family.Name if family is not None else None
        return fam_name == "L_Flange_RV"
    except Exception:
        return False


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
    if is_flange_l_family(elem):
        return True
    if is_fabricair(elem):
        return True
    return False


def build_duct_key(element):
    size = get_text(element, "Size")
    length = get_length(element, DUCT_LENGTH_PARAM)
    return (size, length)


def build_fitting_key(element):
    dim_names = [
        "LIN_VE_DIM_A", "LIN_VE_DIM_B", "LIN_VE_DIM_C", "LIN_VE_DIM_D",
        "LIN_VE_DIM_E", "LIN_VE_DIM_F", "LIN_VE_DIM_H", "LIN_VE_DIM_L",
        "LIN_VE_DIM_M", "LIN_VE_DIM_N", "LIN_VE_DIM_R", "LIN_VE_DIM_R1",
        "LIN_VE_DIM_R2",
    ]
    size = get_text(element, "Size")
    dim_type = get_text(element, "LIN_VE_DIM_TYP")
    dims = [get_length(element, name) for name in dim_names]
    angle = get_angle(element, "LIN_VE_ANG_W")
    return tuple([size, dim_type] + dims + [angle])


def collect_targets():
    selected_ids = list(uidoc.Selection.GetElementIds())
    allowed_cats = (
        int(BuiltInCategory.OST_DuctCurves),
        int(BuiltInCategory.OST_DuctFitting)
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

    ducts = (FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_DuctCurves)
             .WhereElementIsNotElementType()
             .ToElements())
    fittings = (FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DuctFitting)
                .WhereElementIsNotElementType()
                .ToElements())

    ducts = [d for d in ducts if not should_skip_element(d)]
    fittings = [f for f in fittings if not should_skip_element(f)]

    return list(ducts) + list(fittings)


def group_elements(elements):
    groups = {}
    for elem in elements:
        if elem is None or elem.Category is None:
            continue

        sys_abbr = get_system_abbr(elem)
        sys_group = groups.setdefault(sys_abbr, {"ducts": {}, "fittings": {}})

        cat_id = elem.Category.Id.IntegerValue
        if cat_id == int(BuiltInCategory.OST_DuctCurves):
            key = build_duct_key(elem)
            sys_group["ducts"].setdefault(key, []).append(elem)
        elif cat_id == int(BuiltInCategory.OST_DuctFitting):
            key = build_fitting_key(elem)
            sys_group["fittings"].setdefault(key, []).append(elem)
    return groups


def sorted_keys(keys, is_fitting=False):
    none_safe = lambda x: ("" if x is None else x) if isinstance(x, basestring) else (-1 if x is None else x)
    if not is_fitting:
        return sorted(keys, key=lambda k: (none_safe(k[0]), none_safe(k[1])))
    return sorted(keys, key=lambda k: tuple(none_safe(part) for part in k))


def format_number(prefix, number):
    return "%s.%s" % (prefix, number) if prefix else str(number)


def assign_numbers(groups):
    t = Transaction(doc, __title__)
    t.Start()
    stats = {
        "systems": len(groups),
        "ducts": 0,
        "fittings": 0,
        "total": 0,
        "same_number": 0,
    }
    try:
        for sys_abbr in sorted(groups.keys()):
            data = groups[sys_abbr]
            current = 1

            # Kanały
            for key in sorted_keys(data["ducts"].keys()):
                value = format_number(sys_abbr, current)
                elems = data["ducts"][key]
                for elem in elems:
                    set_position_number(elem, value)
                stats["ducts"] += len(elems)
                stats["total"] += len(elems)
                if len(elems) > 1:
                    stats["same_number"] += len(elems) - 1
                current += 1

            # Kształtki
            for key in sorted_keys(data["fittings"].keys(), is_fitting=True):
                value = format_number(sys_abbr, current)
                elems = data["fittings"][key]
                for elem in elems:
                    set_position_number(elem, value)
                stats["fittings"] += len(elems)
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


def set_position_number(element, value):
    param = element.LookupParameter("LIN_POSITION_NUMBER_A")
    if param is None or param.IsReadOnly:
        return
    try:
        param.Set(value)
    except Exception:
        # Brak logów – cicho pomijamy błędy zapisu
        pass


def detect_extended_length_param():
    """Sprawdza, czy na kanałach istnieje parametr 'Powiększona długość'."""
    try:
        ducts = (FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_DuctCurves)
                 .WhereElementIsNotElementType()
                 .ToElements())
        for d in ducts:
            if d.LookupParameter("Powiększona długość") is not None:
                return True
        return False
    except Exception:
        return False


def choose_duct_length_param():
    """Otwiera okno WPF i zwraca nazwę parametru długości lub None."""
    win = DuctNumerationWindow()
    win.ShowDialog()
    return win.length_param


# ---------------------------------------------------------------------------
# Główna funkcja
# ---------------------------------------------------------------------------

def main():
    global DUCT_LENGTH_PARAM, HAS_EXTENDED_LENGTH_PARAM

    # Sprawdzenie dostępności parametru projektu "Powiększona długość"
    HAS_EXTENDED_LENGTH_PARAM = detect_extended_length_param()

    # Wybór parametru długości dla kanałów
    choice = choose_duct_length_param()
    if not choice:
        # Użytkownik anulował
        return

    DUCT_LENGTH_PARAM = choice

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
        "Kanały: {ducts}\n"
        "Kształtki: {fittings}\n"
        "Elementów współdzielących numer: {same_number}\n"
        "Systemy: {systems}\n"
        "Parametr długości kanałów: {length_param}\n"
        "Pominięto: rodziny 'L_Flange_RV' oraz Manufacturer = 'FabricAir'."
    ).format(length_param=DUCT_LENGTH_PARAM, **stats)

    summary_message = summary
    if status != TransactionStatus.Committed:
        summary_message += "\nTransakcja zwróciła status: %s" % status

    TaskDialog.Show(__title__, summary_message)


if __name__ == "__main__":
    main()
