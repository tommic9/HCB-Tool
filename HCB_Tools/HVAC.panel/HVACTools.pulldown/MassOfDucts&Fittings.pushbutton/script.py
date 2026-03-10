# -*- coding: utf-8 -*-
__title__ = "MassOfDucts&Fittings"

__doc__ = u"""Obliczanie masy elementów Ducts oraz Ducts Fittings
Prostokątne elementy używają HC_Area oraz doliczany jest ciężar ramki w zależności od wymiaru. 
Okrągłe Ducts mają predefiniowane wagi na mb
Wynik nadpisywany dla elementu do parametru HC_Masa
"""

from pyrevit import revit, script
from Autodesk.Revit.DB import (

    BuiltInCategory, BuiltInParameter, ElementCategoryFilter,
    FilteredElementCollector, LogicalOrFilter, UnitUtils, UnitTypeId,
    Transaction, StorageType, ConnectorProfileType,
)
from Snippets._selection import get_selected_elements
import math

# ------------------------------------------------------------
# Helpers

doc = revit.doc
uidoc = revit.uidoc
output = script.get_output()

STEEL_DENSITY = 7850.0  # kg/m3
SHEET_THICKNESS_M = 0.001  # 1 mm
FRAME_THICKNESS_M = 0.002  # 2 mm

# Mapping of maximum dimension (mm) -> frame width (mm)
FRAME_WIDTH_BY_DIM_MM = {
    500: 20,
    1000: 30,
    1500: 40,
    999999: 50,
}


# Allowed categories
ALLOWED_CATS = [
    BuiltInCategory.OST_DuctCurves,
    BuiltInCategory.OST_DuctFitting,
    BuiltInCategory.OST_FlexDuctCurves,
    BuiltInCategory.OST_DuctAccessory,
]
ALLOWED_CAT_IDS = {int(c) for c in ALLOWED_CATS}

# Mass per meter for round ducts {diameter_mm: kg_per_m}
ROUND_DUCT_WEIGHTS = {
    63: 0.85,
    80: 0.82,
    100: 1.02,
    112: 1.14,
    125: 1.28,
    140: 1.43,
    150: 1.53,
    160: 1.64,
    180: 2.04,
    200: 2.27,
    224: 2.54,
    250: 2.84,
    280: 3.58,
    300: 3.83,
    315: 4.02,
    355: 4.54,
    400: 6.01,
    450: 7.03,
    500: 7.81,
    560: 8.74,
    600: 9.37,
    630: 9.84,
    710: 13.1,
    800: 14.8,
    900: 21.7,
    1000: 24.1,
    1120: 27.0,
    1250: 30.2,
    1400: 47.5,
    1500: 50.9,
    1600: 54.3,
    1800: 63.1,
    2000: 71.9,
}

def get_frame_width_mm(max_dim_mm):
    for limit in sorted(FRAME_WIDTH_BY_DIM_MM.keys()):
        if max_dim_mm <= limit:
            return FRAME_WIDTH_BY_DIM_MM[limit]
    return FRAME_WIDTH_BY_DIM_MM[max(FRAME_WIDTH_BY_DIM_MM.keys())]


def profile_data(element):

    """Return max dimension, perimeter, connector count and shape."""
    try:
        connectors = element.MEPModel.ConnectorManager.Connectors
    except Exception:
        return 0.0, 0.0, 0, "unknown"


    widths, heights, diams = [], [], []
    for c in connectors:
        try:
            if c.Shape == ConnectorProfileType.Round:
                diams.append(c.Radius * 2.0)
            else:
                widths.append(getattr(c, "Width", 0.0))
                heights.append(getattr(c, "Height", 0.0))
        except Exception:
            pass

    max_dim = 0.0
    perimeter = 0.0

    shape = "unknown"
    if widths and heights:
        w = max(widths)
        h = max(heights)
        max_dim = max(w, h)
        perimeter = 2.0 * (w + h)

        shape = "rect"

    elif diams:
        d = max(diams)
        max_dim = d
        perimeter = math.pi * d
        shape = "round"
    return max_dim, perimeter, len(list(connectors)), shape


# ------------------------------------------------------------
# Collect elements


success_rows = []
error_rows = []

selected = get_selected_elements()
if selected:
    elements = []
    for el in selected:
        cat = el.Category
        if cat and cat.Id.IntegerValue in ALLOWED_CAT_IDS:
            elements.append(el)
        else:
            name = cat.Name if cat else ""
            error_rows.append([output.linkify(el.Id), name, u"Niedozwolona kategoria"])
else:
    filters = [ElementCategoryFilter(c) for c in ALLOWED_CATS]
    collector = (FilteredElementCollector(doc)
                 .WherePasses(LogicalOrFilter(filters))
                 .WhereElementIsNotElementType())
    elements = list(collector)


# ------------------------------------------------------------
# Processing

with Transaction(doc, __title__) as t:
    t.Start()
    for el in elements:

        cat = el.Category
        cat_name = cat.Name if cat else ""
        link = output.linkify(el.Id)
        cat_id = cat.Id.IntegerValue if cat else None

        max_dim, perim, conn_count, shape = profile_data(el)

        # Round duct with predefined weights
        if cat_id == int(BuiltInCategory.OST_DuctCurves) and shape == "round":
            mass_param = el.LookupParameter("HC_Masa")
            if not mass_param:
                error_rows.append([link, cat_name, u"Brak parametru HC_Masa"])
                continue
            length_param = el.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)
            if not length_param or length_param.StorageType != StorageType.Double:
                error_rows.append([link, cat_name, u"Brak parametru długości"])
                continue
            diam_mm = int(round(UnitUtils.ConvertFromInternalUnits(max_dim, UnitTypeId.Millimeters)))
            weight_per_m = ROUND_DUCT_WEIGHTS.get(diam_mm)
            if weight_per_m is None:
                error_rows.append([link, cat_name, u"Brak wagi dla Ø{} mm".format(diam_mm)])
                continue
            length_m = UnitUtils.ConvertFromInternalUnits(length_param.AsDouble(), UnitTypeId.Meters)
            mass = length_m * weight_per_m
            mass_param.Set(mass)
            success_rows.append([link, cat_name, "-", round(mass, 2)])
            continue

        # Skip round fittings for now
        if cat_id == int(BuiltInCategory.OST_DuctFitting) and shape == "round":
            error_rows.append([link, cat_name, u"Okrągła kształtka"])
            continue

        area_param = el.LookupParameter("HC_Area")
        mass_param = el.LookupParameter("HC_Masa")
        if not mass_param:
            error_rows.append([link, cat_name, u"Brak parametru HC_Masa"])
            continue
        if not area_param or area_param.StorageType != StorageType.Double:
            error_rows.append([link, cat_name, u"Brak parametru HC_Area"])
            continue
        area_m2 = UnitUtils.ConvertFromInternalUnits(area_param.AsDouble(), UnitTypeId.SquareMeters)
        if area_m2 <= 0:
            error_rows.append([link, cat_name, u"HC_Area <= 0"])
            continue
        max_dim_mm = UnitUtils.ConvertFromInternalUnits(max_dim, UnitTypeId.Millimeters)
        perim_m = UnitUtils.ConvertFromInternalUnits(perim, UnitTypeId.Meters)
        frame_width_m = get_frame_width_mm(max_dim_mm) / 1000.0
        frame_mass = perim_m * frame_width_m * FRAME_THICKNESS_M * STEEL_DENSITY * conn_count
        sheet_mass = area_m2 * SHEET_THICKNESS_M * STEEL_DENSITY
        total_mass = sheet_mass + frame_mass
        mass_param.Set(total_mass)
        success_rows.append([link, cat_name, round(area_m2, 3), round(total_mass, 2)])

    t.Commit()

# ------------------------------------------------------------
# Output tables

if success_rows:
    output.print_table(
        table_data=success_rows,
        columns=["ID", "Kategoria", "HC_Area [m²]", "HC_Masa [kg]"],
        title=u"Raport masy elementów"
    )
if error_rows:
    output.print_table(
        table_data=error_rows,
        columns=["ID", "Kategoria", "Powód"],
        title=u"Elementy pominięte"
    )
