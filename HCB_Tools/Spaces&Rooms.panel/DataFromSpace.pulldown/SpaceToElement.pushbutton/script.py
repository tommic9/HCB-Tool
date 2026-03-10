# -*- coding: utf-8 -*-
__title__ = "SpaceToElement"
__doc__ = """Data  = 06.2025
_____________________________________________________________________
Komentarz:
Pobiera numer i nazwę przestrzeni (MEPSpaces) z modelu i przypisuje je do elementów MEP/HVAC
na podstawie lokalizacji geometrycznej (centroid).

Jak korzystać:
- Zaznacz przestrzenie w widoku (lub anuluj, aby pobrać wszystkie).
- Uruchom skrypt.
- Na koniec zobacz logi w konsoli pyRevit.

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

import clr
from pyrevit import forms, script
clr.AddReference('RevitServices')
from Snippets._selection import ISelectionFilter_Categories
clr.AddReference('RevitAPI')
import Autodesk.Revit.DB as DB
from Autodesk.Revit.DB import (
    Transaction, FilteredElementCollector, BuiltInCategory, Options, Solid,
    SharedParameterElement, InstanceBinding, CategorySet, BuiltInParameter,
    GeometryInstance, LocationPoint, LocationCurve
)
clr.AddReference('RevitAPIUI')

# Revit 2024 i starsze vs 2025+
# W 2025+ wprowadzono GroupTypeId, wcześniejsze wersje używały BuiltInParameterGroup.
try:
    from Autodesk.Revit.DB import BuiltInParameterGroup as ParamGroup  # 2024-
except ImportError:  # 2025+
    from Autodesk.Revit.DB import GroupTypeId as ParamGroup

# Config
uidoc = __revit__.ActiveUIDocument
doc = uidoc.Document
app = __revit__.Application

target_categories = [
    BuiltInCategory.OST_MechanicalEquipment,
    BuiltInCategory.OST_DuctCurves,
    BuiltInCategory.OST_FlexDuctCurves,
    BuiltInCategory.OST_DuctFitting,
    BuiltInCategory.OST_DuctAccessory,
    BuiltInCategory.OST_DuctTerminal,
    BuiltInCategory.OST_PipeCurves,
    BuiltInCategory.OST_FlexPipeCurves,
    BuiltInCategory.OST_PipeFitting,
    BuiltInCategory.OST_PipeAccessory,
    BuiltInCategory.OST_PlumbingEquipment,
    BuiltInCategory.OST_CommunicationDevices,
    BuiltInCategory.OST_PlumbingFixtures,
    BuiltInCategory.OST_PlumbingEquipment,
    BuiltInCategory.OST_ElectricalEquipment,
    BuiltInCategory.OST_ElectricalFixtures,
    BuiltInCategory.OST_PlumbingFixtures,
    BuiltInCategory.OST_LightingFixtures,
]
shared_params = ["LIN_ROOM_NUMBER", "LIN_ROOM_NAME"]
DEBUG = True

# Helpers
def ensure_shared_parameters():
    output = script.get_output()
    bindings = doc.ParameterBindings
    existing_defs = {sp.GetDefinition().Name: sp for sp in FilteredElementCollector(doc).OfClass(SharedParameterElement).ToElements()}
    missing = [p for p in shared_params if p not in existing_defs]
    if missing:
        file_path = forms.pick_file(file_ext='txt', title='Wskaż plik Shared Parameters')
        if not file_path:
            output.print_md(":x: Nie znaleziono definicji parametrów i nie wskazano pliku.")
            return False
        app.SharedParametersFilename = file_path
        def_file = app.OpenSharedParameterFile()
        for group in def_file.Groups:
            for defn in group.Definitions:
                if defn.Name in missing:
                    SharedParameterElement.Create(doc, defn)
        existing_defs = {sp.GetDefinition().Name: sp for sp in FilteredElementCollector(doc).OfClass(SharedParameterElement).ToElements()}
    t = Transaction(doc, "Bind Shared Params")
    t.Start()
    cat_set = app.Create.NewCategorySet()
    for bic in target_categories:
        cat_set.Insert(doc.Settings.Categories.get_Item(bic))
    for name in shared_params:
        sp_elem = existing_defs.get(name)
        if sp_elem:
            definition = sp_elem.GetDefinition()
            inst_bind = app.Create.NewInstanceBinding(cat_set)
            if bindings.Contains(definition):
                bindings.Remove(definition)
            bindings.Insert(definition, inst_bind, ParamGroup.Constraints)
    t.Commit()
    return True


def _element_centroid(elem, opts):
    """Zwraca punkt reprezentujący element (środek)."""
    loc = elem.Location
    if isinstance(loc, LocationPoint):
        return loc.Point
    if isinstance(loc, LocationCurve):
        try:
            return loc.Curve.Evaluate(0.5, True)
        except Exception:
            pass
    geom = elem.get_Geometry(opts)
    if geom:
        for obj in geom:
            insts = obj.GetInstanceGeometry() if isinstance(obj, GeometryInstance) else [obj]
            for inst in insts:
                if isinstance(inst, Solid) and inst.Volume > 0:
                    try:
                        return inst.ComputeCentroid()
                    except Exception:
                        bb = inst.GetBoundingBox()
                        return (bb.Min + bb.Max) / 2
    return None


def select_spaces():
    """Pozwala zaznaczyć przestrzenie MEPSpaces w widoku. Anuluj, aby pobrać wszystkie."""
    from Autodesk.Revit.UI.Selection import ObjectType
    from Autodesk.Revit.Exceptions import OperationCanceledException
    output = script.get_output()
    try:
        refs = uidoc.Selection.PickObjects(
            ObjectType.Element,
            ISelectionFilter_Categories([BuiltInCategory.OST_MEPSpaces]),
            "Zaznacz przestrzenie MEPSpaces"
        )
        spaces = [doc.GetElement(r.ElementId) for r in refs]
    except OperationCanceledException:
        spaces = FilteredElementCollector(doc, uidoc.ActiveView.Id)\
            .OfCategory(BuiltInCategory.OST_MEPSpaces)\
            .WhereElementIsNotElementType()\
            .ToElements()
    opts = Options()
    space_data = []
    for sp in spaces:
        centroid = _element_centroid(sp, opts)
        bbox = sp.get_BoundingBox(None)
        space_data.append((sp, centroid, bbox))
    if DEBUG:
        output.print_md("**Selected spaces:**")
        for sp, cen, _ in space_data:
            num = sp.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() or ""
            name = sp.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() or ""
            output.linkify(sp.Id, "{} - {}".format(num, name))
        output.print_md("Processing {} spaces...".format(len(space_data)))
    return space_data


def get_target_elements():
    elems = []
    for bic in target_categories:
        elems += FilteredElementCollector(doc)\
            .OfCategory(bic)\
            .WhereElementIsNotElementType()\
            .ToElements()
    return elems


def _point_in_bbox(pt, bbox):
    return (
        bbox.Min.X <= pt.X <= bbox.Max.X and
        bbox.Min.Y <= pt.Y <= bbox.Max.Y and
        bbox.Min.Z <= pt.Z <= bbox.Max.Z
    )


def map_elements_to_spaces(elements, space_data):
    opts = Options()
    mapping = {}
    errors = []
    for elem in elements:
        mapping[elem.Id] = None
        try:
            pt = _element_centroid(elem, opts)
            if pt is None:
                continue
            for sp, cen, bbox in space_data:
                if bbox and not _point_in_bbox(pt, bbox):
                    continue
                if sp.IsPointInSpace(pt):
                    mapping[elem.Id] = sp
                    break
        except Exception as ex:
            errors.append((elem.Id, str(ex)))
    return mapping, errors


def main():
    if not ensure_shared_parameters():
        return
    output = script.get_output()
    space_data = select_spaces()
    all_elems = get_target_elements()
    mapping, errors = map_elements_to_spaces(all_elems, space_data)
    elems = [e for e in all_elems if mapping[e.Id]]
    skipped = len(all_elems) - len(elems)
    if DEBUG:
        output.print_md("**Elements to update:** {} (skipped: {})".format(len(elems), skipped))
    updated = 0
    t = Transaction(doc, __title__)
    t.Start()
    for elem in elems:
        sp = mapping[elem.Id]
        p_num = elem.LookupParameter("LIN_ROOM_NUMBER")
        p_name = elem.LookupParameter("LIN_ROOM_NAME")
        elem_link = output.linkify(elem.Id) if DEBUG else str(elem.Id)
        try:
            if sp and p_num and p_name and not p_num.IsReadOnly and not p_name.IsReadOnly:
                num = sp.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString() or ""
                name = sp.get_Parameter(BuiltInParameter.ROOM_NAME).AsString() or ""
                p_num.Set(num)
                p_name.Set(name)
                updated += 1
        except Exception as ex:
            output.print_md("Elem {}: {}".format(elem_link, ex))
    t.Commit()
    # Podsumowanie
    summary = "**Summary:** Zaktualizowano {} elementów z {} przestrzeni".format(updated, len(space_data))
    output.print_md(summary)
    if skipped:
        output.print_md("_Pominięto {} elementów spoza wybranych przestrzeni_".format(skipped))
    if errors:
        output.print_md("**Mapping errors:** {} items".format(len(errors)))
        for eid, msg in errors:
            output.print_md(":x: Map Err {}: {}".format(eid, msg))
    forms.alert("Zaktualizowano {} elementów\nPominięto: {}\nBłędy mapowania: {}".format(updated, skipped, len(errors)))

if __name__ == "__main__":
    main()
