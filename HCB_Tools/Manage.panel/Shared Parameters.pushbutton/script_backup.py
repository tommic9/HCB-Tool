
# -*- coding: utf-8 -*-
__title__ = "Shared parameters"
__doc__ = """Date    =  07.08.2024
_____________________________________________________________________
Description:
Checking Shared parameters
"""

# ╦╔╦╗╔═╗╔═╗╦═╗╔╦╗╔═╗
# ║║║║╠═╝║ ║╠╦╝ ║ ╚═╗
# ╩╩ ╩╩  ╚═╝╩╚═ ╩ ╚═╝ IMPORTS
# ==================================================
# Regular + Autodesk
from Autodesk.Revit.DB import *
from Autodesk.Revit.UI.Selection import ObjectType, Selection
# pyRevit


# .NET Imports
import clr
clr.AddReference("System")

# ╦  ╦╔═╗╦═╗╦╔═╗╔╗ ╦  ╔═╗╔═╗
# ╚╗╔╝╠═╣╠╦╝║╠═╣╠╩╗║  ║╣ ╚═╗
#  ╚╝ ╩ ╩╩╚═╩╩ ╩╚═╝╩═╝╚═╝╚═╝ VARIABLES
# ==================================================
doc   = __revit__.ActiveUIDocument.Document
uidoc = __revit__.ActiveUIDocument
app   = __revit__.Application

selection = uidoc.Selection # type: Selection

# Main

# #Get Parameter Bindings Map
# bm = doc.ParameterBindings
#
# # Create a Forward Iterator
# itor = bm.ForwardIterator()
# itor.Reset()
#
# # Iterate over the map
# # loaded_parameters = []
# while itor.MoveNext():
#     try:
#         d = itor.Key #type: Definition
#         print(d.Name)
#     except:
#         pass
#

# def check_missing_params(list_p_names):
#     """Sprawdzenie czy wszystkie parametry wymagane w projekcie zostały wprowadzone
#     :param list_p_names:
#     :return:
#     """
#     #Get Parameter Bindings Map
#     bm = doc.ParameterBindings
#
#     # Create a Forward Iterator
#     itor = bm.ForwardIterator()
#     itor.Reset()
#
#     # Iterate over the map
#     loaded_parameters = []
#     while itor.MoveNext():
#         try:
#             d = itor.Key #type: Definition
#             loaded_parameters.append(d.Name)
#             # print(d.Name)
#         except:
#             pass
#
#     missing_params = [p_name for p_name in list_p_names if p_name not in loaded_parameters]
#
#     # missing_params = []
#     # for p_name in req_params:
#     #     if p_name not in loaded_parameters:
#     #         missing_params.append(p_name)
#     return missing_params

# if missing_params:
#     print("Missing Parameters:")
#     for p_name in missing_params:
#         print(p_name)

# # Acces Shared parameter file
# sp_file = app.OpenSharedParameterFile()
# #app.SharedParameterFilename = 'C:\Users\tomci\OneDrive - HellCold Sp. z o.o\Biblioteki BIM\010_Parametry współdzielone\Parametry współdzielone Hellcold.txt'
#
# # Find matching definition to missing parameter names
# missing_def = []
# if sp_file:
#     for group in sp_file.Groups:
#         #print('\nGroup Name: {}'.format(group.Name))
#         for p_def in group.Definitions:
#             if p_def.Name in missing_params:
#                 missing_def.append(p_def)
#
# # Select Category
# all_cats = doc.Settings.Categories
# cat_views = all_cats.get_Item(BuiltInCategory.OST_Views)
# cat_sheets = all_cats.get_Item(BuiltInCategory.OST_Sheets)
# cat_ductfitting = all_cats.get_Item(BuiltInCategory.OST_DuctFitting)
# cat_duct = all_cats.get_Item(BuiltInCategory.OST_DuctCurves)
#
# # Create
# cat_set = CategorySet()
# cat_set.Insert(cat_views)
# cat_set.Insert(cat_sheets)
# cat_set.Insert(cat_ductfitting)
# cat_set.Insert(cat_duct)
#
#
# # Create Binding
# binding = InstanceBinding(cat_set)
# #binding = TypeBinding(category_set)
#
# param_group = BuiltInParameterGroup.PG_IDENTITY_DATA
#
# # Add parameters
# t = Transaction(doc, 'AddSharedParameters')
# t.Start()
# for p_def in missing_def:
#     if not doc.ParameterBindings.Contains(p_def):
#         doc.ParameterBindings.Insert(p_def, binding, param_group)
#         print('Adding SharedParameter: {}'.format(p_def.Name))
#
# t.Commit()


# t = Transaction(doc, 'SetVariesByGroup')
# t.Start()
#
# binding_map = doc.ParameterBindings
# it = binding_map.ForwardIterator()
# it.Reset()
#
# while it.MoveNext():
#     p_def = it.Key
#
#     if req_params in p_def.Name:
#         try:
#             p_def.SetAllowCaryBetweenGroup(doc, True)
#             print('SetVaryByGroup for: {}'.format(p_def.Name))
#         except:
#             pass
#
# t.Commit()
from pyrevit import forms

# FUNCTIONS
def get_loaded_parameters_as_def():
    """Get Loaded Parameters in the project as Definitions"""
    definitions = []

    binding_map = doc.ParameterBindings
    it = binding_map.ForwardIterator()
    for i in it:
        definition = it.Key
        definitions.append(definition)
    return definitions



def check_missing_params(list_param_names):
    """Function to check Loaded Shared Parameters.
    :param list_param_names: List of Parameter names
    :return:                 List of Missing Parameter Names"""
    #1️⃣ Read Loaded Parameter Names
    loaded_p_definitions = get_loaded_parameters_as_def()
    loaded_param_names   = [d.Name for d in loaded_p_definitions]

    # Check if Parameters Missing
    missing_params = [p_name for p_name in list_param_names if p_name not in loaded_param_names]
    # 👇 It's the same as
    # missing_params = []
    # for p_name in list_param_names:
    #     if p_name not in loaded_param_names:
    #         missing_params.append(p_name)
    return missing_params

def load_params(p_names_to_load,
                bic_cats,
                bind_mode = 'instance',
                p_group   = BuiltInParameterGroup.PG_IDENTITY_DATA):
    #type: (list, list, str, BuiltInParameterGroup) -> None
    """Function to check Loaded Shared Parameters.
    :param p_names_to_load: List of Parameter names
    :param bic_cats:        List of BuiltInCategories for Parameters
    :param param_cat_map:   Dictionary with Parameter names as keys and list of categories
    :param bind_mode:       Binding Mode: 'instance' / 'type'
    :param p_group:         BuiltInParameterGroup"""

    # 📁 Ensure SharedParameterFile is available
    sp_file = app.OpenSharedParameterFile()
    if not sp_file:
        forms.alert('Could not find SharedParameter File. '
                    '\nPlease Set the File in Revit and Try Again', title=__title__, exitscript=True)
    p_filepath = r'C:\Users\tomci\OneDrive - HellCold Sp. z o.o\Biblioteki BIM\010_Parametry współdzielone\Parametry współdzielone Hellcold.txt'

    #🙋‍ Ask for User Confirmation
    if missing_params:
        confirmed = forms.alert("There are {n_missing} missing parameters for the script."
                                "\n{missing_params}"
                                "\n\nWould you like to try loading them from the following SharedParameterFile:"
                                "\n{p_filepath}".format(n_missing      = len(missing_params),
                                                        missing_params = '\n'.join(missing_params),
                                                        p_filepath     = sp_file.Filename),
                                yes=True, no=True)

        if confirmed:
            # Prepare Categories
            all_cats = doc.Settings.Categories
            cats     = [all_cats.get_Item(bic_cat) for bic_cat in bic_cats]

            # Create Category Set
            cat_set = CategorySet()
            for cat in cats:
                cat_set.Insert(cat)

            # Create Binding
            binding = TypeBinding(cat_set) if bind_mode == 'type' \
                      else InstanceBinding(cat_set)

            # Add Parameters (if possible)
            for d_group in sp_file.Groups:
                for p_def in d_group.Definitions:
                    if p_def.Name in p_names_to_load:
                        doc.ParameterBindings.Insert(p_def, binding, p_group)
                        p_names_to_load.remove(p_def.Name)

            # SetAllowVaryBetweenGroups (If Possible
            loaded_p_definitions = get_loaded_parameters_as_def()
            for definition in loaded_p_definitions:
                if definition.Name in p_names_to_load:
                    try:
                        definition.SetAllowVaryBetweenGroups(doc, True)
                    except:
                        pass

            #👀 Reported Not Loaded Parameters
            if p_names_to_load:
                msg = "Couldn't Find following Parameters: \n{}".format('\n'.join(p_names_to_load))
                forms.alert(msg, title=__title__)

# MAIN
required_params = ['HC_Area', 'HC_System', 'HC_Duct_System', 'HC_Piping_System', 'HC_Do_Zamówienia', 'HC_Zamówione', 'HC_Etap', 'HC_Branża']
missing_params  = check_missing_params(required_params)

if missing_params:
    # 🔓 Transaction Start
    t = Transaction(doc, 'Add SharedParameters')
    t.Start()

    bic_cats = [BuiltInCategory.OST_DuctFitting,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_DuctAccessory,
                BuiltInCategory.OST_DuctTerminal,
                BuiltInCategory.OST_FlexDuctCurves,
                BuiltInCategory.OST_MechanicalEquipment]

    load_params(p_names_to_load = missing_params,
                bic_cats        = bic_cats,
                bind_mode       = 'instance',
                p_group         = BuiltInParameterGroup.PG_IDENTITY_DATA)

    t.Commit()
