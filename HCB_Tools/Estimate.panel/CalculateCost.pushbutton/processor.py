# -*- coding: utf-8 -*-
from Autodesk.Revit.DB import *

PARAM_PRICE = "HC_Cena_Jednostkowa"
PARAM_COST  = "HC_Koszt"
PARAM_ANGLE = "HC_Kąt"
PARAM_AREA_CUSTOM = "HC_Area"  # m² dla Duct Fitting (rect)

CATS = {
    "Duct": BuiltInCategory.OST_DuctCurves,
    "Duct Fitting": BuiltInCategory.OST_DuctFitting,
    "Duct Accessory": BuiltInCategory.OST_DuctAccessory,
    "Flex Duct": BuiltInCategory.OST_FlexDuctCurves
}

RECT_BUCKETS = [(0,500), (500,1000), (1000,1500), (1500,2000), (2000,2500)]

def match_typename_key(mapobj, category, tn, dia, angle):
    for (cat, tnp, d, ang) in mapobj.keys():
        if cat != category:
            continue
        if d is not None and int(round(dia)) != int(round(d)):
            continue
        if angle is None and ang is not None:
            continue
        if tnp and tnp in tn:
            return (cat, tnp, d, ang)
    return (category, '__nomatch__', None, angle)

def find_price_round_any(idx, category, diameter_mm):
    k = (category, int(round(diameter_mm)))
    rows = idx['round_any'].get(k)
    return rows[0] if rows else None

def find_price_round_any_size(idx, diameter_mm):
    k = int(round(diameter_mm))
    rows = idx['round_any_size'].get(k)
    return rows[0] if rows else None

def find_price_round_typename(idx, category, typename, diam_candidates_mm, angle_opt):
    """Find catalog price for round elements by typename.

    If a match for a ``BKU`` typename is not found in the catalog,
    fall back to searching for a corresponding ``BU`` entry.
    """

    tn_base = (typename or '').lower()
    # Prepare typename variants: original and optional BKU->BU fallback
    tn_variants = [tn_base]
    if 'bku' in tn_base:
        tn_variants.append(tn_base.replace('bku', 'bu'))

    if not isinstance(diam_candidates_mm, (list, tuple)):
        diam_candidates_mm = [diam_candidates_mm]
    uniq = []
    for d in diam_candidates_mm:
        di = int(round(d))
        if di not in uniq:
            uniq.append(di)
    di_sorted = sorted(uniq, reverse=True)

    for tn in tn_variants:
        if len(uniq) >= 2:
            s2 = di_sorted[:2]
            d_big, d_small = s2[0], s2[1]
            key_pair = (category, tn, d_big, d_small, angle_opt)
            rows = idx['round_pair_typename'].get(key_pair)
            if rows:
                return rows[0]
            key_pair2 = (category, tn, d_big, d_small, None)
            rows2 = idx['round_pair_typename'].get(key_pair2)
            if rows2:
                return rows2[0]

        angle_checks = [angle_opt] if angle_opt is not None else [None]
        if angle_opt is not None:
            angle_checks.append(None)
        for ang in angle_checks:
            for di in di_sorted:
                key = match_typename_key(idx['round_typename'], category, tn, di, ang)
                rows = idx['round_typename'].get(key)
                if rows:
                    return rows[0]
    return None

def find_price_rect_bucket(idx, category, shape, max_dim_mm):
    """Find catalog price for rectangular elements.

    Matches by category, shape and the bucketed maximum dimension.
    """
    shp = (shape or '').lower()
    for _, hi in RECT_BUCKETS:
        if max_dim_mm <= hi:
            k = (category, shp, hi)
            rows = idx['rect_bucket'].get(k)
            if rows:
                return rows[0]
            break
    return None

def get_category_name(el):
    bic = el.Category.BuiltInCategory
    for name, cat in CATS.items():
        if cat == bic:
            return name
    return None

def get_size_string(el):
    p = el.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE)
    if p and p.AsString():
        return p.AsString().strip()
    q = el.LookupParameter("Size")
    if q and q.AsString():
        return q.AsString().strip()
    return ""

def get_length_m(el):
    p = el.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)
    if not p:
        return 0.0
    return UnitUtils.ConvertFromInternalUnits(p.AsDouble(), UnitTypeId.Meters)

def get_angle_inst(el):
    p = el.LookupParameter(PARAM_ANGLE)
    if not p:
        return None
    try:
        if p.StorageType == StorageType.Double:
            # HC_Kąt is stored in Revit internal units (radians). Convert
            # to degrees so the reported value matches the catalog and
            # user expectations.
            val_deg = UnitUtils.ConvertFromInternalUnits(
                p.AsDouble(), UnitTypeId.Degrees)
            return int(round(val_deg))
        if p.StorageType == StorageType.Integer:
            return int(p.AsInteger())
        if p.StorageType == StorageType.String:
            return int(float((p.AsString() or '').replace(',','.')))
    except:
        return None
    return None

def get_typename(el):
    """Return the element type name using ELEM_TYPE_PARAM."""
    try:
        p = el.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)
        if p:
            if p.StorageType == StorageType.ElementId:
                t = el.Document.GetElement(p.AsElementId())
                if t:
                    n = t.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)
                    if n and n.AsString():
                        return n.AsString()
                    return getattr(t, 'Name', '') or ''
            elif p.StorageType == StorageType.String and p.AsString():
                return p.AsString()
    except Exception:
        pass
    try:
        if hasattr(el, 'Symbol') and el.Symbol:
            nam = getattr(el.Symbol, 'Name', '') or ''
            if nam:
                return nam
    except Exception:
        pass
    try:
        return getattr(el, 'Name', '') or ''
    except Exception:
        return ''

def collect_elements(doc):
    els = []
    for _, bic in CATS.items():
        flt = FilteredElementCollector(doc).OfCategory(bic).WhereElementIsNotElementType()
        els.extend(list(flt))
    return els

def ensure_params_writable(el):
    p1 = el.LookupParameter(PARAM_PRICE)
    p2 = el.LookupParameter(PARAM_COST)
    return (p1 and not p1.IsReadOnly) and (p2 and not p2.IsReadOnly)
