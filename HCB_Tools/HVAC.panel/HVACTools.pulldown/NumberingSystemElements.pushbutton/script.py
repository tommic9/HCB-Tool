# -*- coding: utf-8 -*-
__title__ = "NumSys"
__doc__ = """Data  = 06.2025
_____________________________________________________________________
Komentarz: Numeracja elementów DuctFitting i Duct wg połączeń MEP

Jak korzystać:
- Wskaż urządzenie początkowe (kategoria MechanicalEquipment)
- Skrypt wyznaczy kolejność elementów połączeń (DuctFitting z konektorami Rectangular oraz Duct)
- Ustawi shared parametr LIN_POSITION_NUMBER_A: SystemAbbreviation + numer

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
from Autodesk.Revit.DB import (
    FilteredElementCollector, BuiltInCategory, Transaction,
    ConnectorProfileType, ElementId
)
from Autodesk.Revit.UI.Selection import ISelectionFilter, ObjectType

# pyRevit globals
uidoc = __revit__.ActiveUIDocument
doc   = uidoc.Document

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ CONFIG & VARIABLES
# ==================================================
# Parametry do grupowania DuctFitting
dim_params = [
    'LIN_VE_DIM_TYP','LIN_VE_DIM_L','LIN_VE_DIM_A','LIN_VE_DIM_B','LIN_VE_DIM_C',
    'LIN_VE_DIM_D','LIN_VE_DIM_E','LIN_VE_DIM_F','LIN_VE_DIM_H','LIN_VE_DIM_M1',
    'LIN_VE_DIM_M2','LIN_VE_DIM_N','LIN_VE_DIM_R','LIN_VE_DIM_R1','LIN_VE_DIM_R2',
    'LIN_VE_DIM_R3','LIN_VE_DIM_R4'
]
# Kategorie jako int
MECH_EQUIP_CAT = int(BuiltInCategory.OST_MechanicalEquipment)
DUCTFITTING_CAT = int(BuiltInCategory.OST_DuctFitting)
DUCT_CAT       = int(BuiltInCategory.OST_DuctCurves)

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ FUNKCJE NARZĘDZIOWE
# ==================================================
class MechEquipFilter(ISelectionFilter):
    def AllowElement(self, elem):
        return elem.Category and elem.Category.Id.IntegerValue == MECH_EQUIP_CAT
    def AllowReference(self, ref, xyz):
        return False


def get_group_key(el):
    """Zwraca klucz grupujący na podstawie parametrów elementu"""
    cat_id = el.Category.Id.IntegerValue
    if cat_id == DUCTFITTING_CAT:
        vals = []
        for name in dim_params:
            p = el.LookupParameter(name)
            vals.append(p.AsValueString() if p and p.AsValueString() else "")
        return '|'.join(vals)
    elif cat_id == DUCT_CAT:
        # grupuj po Size i Length
        p_size   = el.LookupParameter('Size') or el.get_Parameter(BuiltInCategory.OST_DuctCurves)
        p_length = el.get_Parameter(BuiltInCategory.CURVE_ELEM_LENGTH_PARAM)
        size   = p_size.AsValueString() if p_size else ''
        length = p_length.AsValueString() if p_length else ''
        return '{}|{}'.format(size, length)
    return ''


def collect_neighbors(el):
    """Zwraca sąsiadów el. w sieci MEP (Duct/DuctFitting)"""
    neigh = []
    mep = getattr(el, 'MEPModel', None)
    if mep:
        for conn in mep.ConnectorManager.Connectors:
            # pomijaj inne kształty dla fittings
            if el.Category.Id.IntegerValue == DUCTFITTING_CAT and conn.Shape != ConnectorProfileType.Rectangular:
                continue
            for link in conn.AllRefs:
                other = link.Owner
                cid = other.Category.Id.IntegerValue if other.Category else None
                if cid in (DUCTFITTING_CAT, DUCT_CAT) and other.Id != el.Id:
                    neigh.append(other)
    return neigh


def traverse_system(start):
    """BFS po sieci MEP, zwraca listę elementów w kolejności"""
    visited, queue, order = set(), [start], []
    while queue:
        el = queue.pop(0)
        if el.Id in visited:
            continue
        visited.add(el.Id)
        cid = el.Category.Id.IntegerValue if el.Category else None
        if cid in (DUCTFITTING_CAT, DUCT_CAT):
            order.append(el)
        for nb in collect_neighbors(el):
            if nb.Id not in visited:
                queue.append(nb)
    return order


def main():
    # Wybór urządzenia początkowego
    try:
        ref = uidoc.Selection.PickObject(
            ObjectType.Element,
            MechEquipFilter(),
            "Wskaż MechanicalEquipment jako start"
        )
    except Exception:
        return
    start = doc.GetElement(ref)

    elems = traverse_system(start)
    key_map = {}
    counter = 1

    t = Transaction(doc, __title__)
    t.Start()
    for el in elems:
        key = get_group_key(el)
        if not key:
            continue
        if key not in key_map:
            key_map[key] = counter
            counter += 1
        num = key_map[key]
        # pobierz skrót systemu
        p_sys = el.LookupParameter('System Abbreviation')
        sys_abbr = p_sys.AsString() if p_sys else ''
        mark = "{}{}".format(sys_abbr, num)
        # ustaw shared parameter
        p_out = el.LookupParameter('LIN_POSITION_NUMBER_A')
        if p_out and not p_out.IsReadOnly:
            p_out.Set(mark)
    t.Commit()

# Wywołanie głównej funkcji
def __run__():
    main()

__run__()
