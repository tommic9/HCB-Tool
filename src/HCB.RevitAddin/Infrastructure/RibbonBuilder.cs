using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin
{
    internal static class RibbonBuilder
    {
        public static void Create(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(RibbonDefinitions.TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // The tab already exists in this Revit session.
            }

            foreach (RibbonPanelDefinition panelDefinition in RibbonDefinitions.Create())
            {
                RibbonPanel panel = GetOrCreatePanel(application, panelDefinition.Name);
                AddItems(panel, panelDefinition.Items);
            }
        }

        private static void AddItems(RibbonPanel panel, IReadOnlyList<RibbonItemDefinition> items)
        {
            foreach (RibbonItemDefinition item in items)
            {
                switch (item)
                {
                    case RibbonPushButtonDefinition pushButton:
                        panel.AddItem(CreatePushButtonData(pushButton));
                        break;

                    case RibbonPulldownDefinition pulldown:
                        AddPulldown(panel, pulldown);
                        break;

                    case RibbonStackDefinition stack:
                        AddStack(panel, stack);
                        break;
                }
            }
        }

        private static void AddStack(RibbonPanel panel, RibbonStackDefinition stack)
        {
            if (stack.Items.Count < 2 || stack.Items.Count > 3)
            {
                throw new InvalidOperationException("Ribbon stacks must contain 2 or 3 items.");
            }

            List<RibbonItemData> itemData = new(stack.Items.Count);
            foreach (RibbonStackItemDefinition item in stack.Items)
            {
                itemData.Add(CreateRibbonItemData(item));
            }

            IList<RibbonItem> createdItems = itemData.Count switch
            {
                2 => panel.AddStackedItems(itemData[0], itemData[1]),
                3 => panel.AddStackedItems(itemData[0], itemData[1], itemData[2]),
                _ => throw new InvalidOperationException("Unsupported ribbon stack size.")
            };

            for (int index = 0; index < stack.Items.Count; index++)
            {
                if (stack.Items[index] is RibbonPulldownDefinition pulldown &&
                    createdItems[index] is PulldownButton pulldownButton)
                {
                    AddPulldownButtons(pulldownButton, pulldown.Buttons);
                }
            }
        }

        private static void AddPulldown(RibbonPanel panel, RibbonPulldownDefinition definition)
        {
            PulldownButtonData pulldownData = CreatePulldownButtonData(definition);
            PulldownButton pulldownButton = (PulldownButton)panel.AddItem(pulldownData);
            AddPulldownButtons(pulldownButton, definition.Buttons);
        }

        private static void AddPulldownButtons(PulldownButton pulldownButton, IReadOnlyList<RibbonPushButtonDefinition> buttons)
        {
            foreach (RibbonPushButtonDefinition button in buttons)
            {
                pulldownButton.AddPushButton(CreatePushButtonData(button));
            }
        }

        private static RibbonItemData CreateRibbonItemData(RibbonStackItemDefinition definition)
        {
            return definition switch
            {
                RibbonPushButtonDefinition pushButton => CreatePushButtonData(pushButton),
                RibbonPulldownDefinition pulldown => CreatePulldownButtonData(pulldown),
                _ => throw new InvalidOperationException("Unsupported ribbon stack item.")
            };
        }

        private static PushButtonData CreatePushButtonData(RibbonPushButtonDefinition definition)
        {
            string assemblyPath = definition.CommandType.Assembly.Location;

            PushButtonData buttonData = new(
                definition.Name,
                definition.Text,
                assemblyPath,
                definition.CommandType.FullName)
            {
                ToolTip = definition.ToolTip,
                LongDescription = definition.LongDescription
            };

            RibbonIconResolver.ApplyTo(buttonData, definition.CommandType);
            return buttonData;
        }

        private static PulldownButtonData CreatePulldownButtonData(RibbonPulldownDefinition definition)
        {
            PulldownButtonData buttonData = new(definition.Name, definition.Text)
            {
                ToolTip = definition.ToolTip,
                LongDescription = definition.LongDescription
            };

            RibbonIconResolver.ApplyTo(buttonData, definition.IconCommandType, definition.IconResourceDirectory);
            return buttonData;
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication application, string panelName)
        {
            foreach (RibbonPanel panel in application.GetRibbonPanels(RibbonDefinitions.TabName))
            {
                if (string.Equals(panel.Name, panelName, StringComparison.Ordinal))
                {
                    return panel;
                }
            }

            return application.CreateRibbonPanel(RibbonDefinitions.TabName, panelName);
        }
    }
}
