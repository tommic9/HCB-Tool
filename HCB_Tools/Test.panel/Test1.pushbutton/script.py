# -*- coding: utf-8 -*-
__title__ = "ViewAlign"
__doc__ = """Data  = 07.2024
_____________________________________________________________________
Komentarz:
Narzędzie wyrównuje kamerę widoku 3D względem osi globalnych projektu lub obraca ją o zadany kąt.

Jak korzystać:
- krok 1 - w aktywnym widoku wybieramy funkcję
- krok 2 - Ustawiamy oś, kierunek oraz (opcjonalnie) kąt obrotu
- krok 3  - zatwierdzamy OK lub Anulujemy

Autor: Tomasz Michałek, HellCold BIM PROJECT
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
import math
from collections import OrderedDict

from pyrevit import forms, revit, script

from Autodesk.Revit.DB import (  # pylint: disable=import-error
    Transaction,
    View3D,
    ViewOrientation3D,
    XYZ,
    Transform,
)

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ CONFIG & VARIABLES
# ==================================================
doc = revit.doc
active_view = doc.ActiveView

AXES = OrderedDict([
    ("Oś X", XYZ.BasisX),
    ("Oś Y", XYZ.BasisY),
    ("Oś Z", XYZ.BasisZ),
])
DIRECTIONS = OrderedDict([
    ("Dodatni", 1.0),
    ("Ujemny", -1.0),
])

# ╔╦╗╔═╗╦╔╗╔
# ║║║╠═╣║║║║
# ╩ ╩╩ ╩╩╝╚╝ FUNKCJE NARZĘDZIOWE
# ==================================================
def _normalize(vector):
    """Zwraca znormalizowany wektor XYZ."""
    length = vector.GetLength()
    if length == 0:
        return vector
    return XYZ(vector.X / length, vector.Y / length, vector.Z / length)


def _is_parallel(vec_a, vec_b):
    """Sprawdza czy dwa wektory są równoległe."""
    cross = vec_a.CrossProduct(vec_b)
    return cross.GetLength() < 1e-9


def _default_up(forward):
    """Dobiera wektor UP, aby nie był równoległy do forward."""
    candidates = [XYZ.BasisZ, XYZ.BasisY, XYZ.BasisX]
    for candidate in candidates:
        if not _is_parallel(forward, candidate):
            right = forward.CrossProduct(candidate)
            if right.GetLength() > 1e-6:
                return _normalize(right.CrossProduct(forward))
    return XYZ.BasisZ


def _get_view_target(view3d, orientation):
    """Próbuje ustalić punkt, na który patrzy kamera."""
    try:
        return view3d.Origin
    except AttributeError:
        return orientation.EyePosition + orientation.ForwardDirection


def _apply_orientation(view3d, eye, up, forward):
    """Ustawia orientację widoku 3D."""
    orientation = ViewOrientation3D(eye, up, forward)
    view3d.SetOrientation(orientation)


def main():
    """Główna funkcja narzędzia."""
    if not isinstance(active_view, View3D):
        forms.alert(u"Aktywny widok nie jest widokiem 3D.")
        script.exit()

    orientation = active_view.GetOrientation()
    eye = orientation.EyePosition
    forward = orientation.ForwardDirection
    up = orientation.UpDirection
    target = _get_view_target(active_view, orientation)
    distance_vec = target - eye
    distance = distance_vec.GetLength()
    if distance < 1e-3:
        distance = 10.0

    components = [
        forms.Label(u"Wybierz oś"),
        forms.ComboBox("axis", AXES),
        forms.Label(u"Kierunek"),
        forms.ComboBox("direction", DIRECTIONS),
        forms.CheckBox("exact", u"Wyrównaj dokładnie do osi", default=True),
        forms.Label(u"Kąt (stopnie)"),
        forms.TextBox("angle", Text="0"),
        forms.Separator(),
        forms.Button(u"OK"),
        forms.Button(u"Anuluj"),
    ]

    dialog = forms.FlexForm(__title__, components)
    if not dialog.show():
        script.exit()

    values = dialog.values
    axis_vector = values.get("axis")
    direction_multiplier = values.get("direction", 1.0)
    exact_mode = values.get("exact", False)

    if axis_vector is None:
        forms.alert(u"Nie wybrano osi.")
        script.exit()

    axis_vector = _normalize(axis_vector).Multiply(direction_multiplier)

    angle_input = values.get("angle", "0").replace(",", ".")
    try:
        angle_degrees = float(angle_input)
    except ValueError:
        forms.alert(u"Nieprawidłowa wartość kąta.")
        script.exit()

    transaction = Transaction(doc, __title__)
    transaction.Start()
    try:
        if exact_mode:
            new_forward = axis_vector
            new_up = _default_up(new_forward)
            new_eye = target - new_forward.Multiply(distance)
            _apply_orientation(active_view, new_eye, new_up, new_forward)
        else:
            if abs(angle_degrees) < 1e-9:
                raise ValueError(u"Podaj kąt różny od zera lub zaznacz wyrównanie do osi.")
            angle_radians = math.radians(angle_degrees)
            rotation = Transform.CreateRotationAtPoint(axis_vector, angle_radians, target)
            new_eye = rotation.OfPoint(eye)
            new_forward = _normalize(rotation.OfVector(forward))
            new_up = _normalize(rotation.OfVector(up))
            _apply_orientation(active_view, new_eye, new_up, new_forward)
        transaction.Commit()
    except ValueError as err:
        transaction.RollBack()
        forms.alert(unicode(err) if not isinstance(err, basestring) else err)
        script.exit()


if __name__ == "__main__":
    main()