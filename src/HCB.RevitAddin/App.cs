using System.Collections.Generic;
using Autodesk.Revit.UI;

namespace HCB.RevitAddin
{
    public class App : IExternalApplication
    {
        private Queue<RibbonPanelDefinition>? _pendingPanels;
        private bool _tabCreated;

        public Result OnStartup(UIControlledApplication application)
        {
            _pendingPanels = new Queue<RibbonPanelDefinition>(RibbonDefinitions.Create());
            application.Idling += (sender, _) => OnApplicationIdling(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void OnApplicationIdling(object? sender)
        {
            if (sender is not UIControlledApplication application || _pendingPanels == null)
            {
                return;
            }

            try
            {
                if (!_tabCreated)
                {
                    RibbonBuilder.EnsureTab(application);
                    _tabCreated = true;
                }

                if (_pendingPanels.Count == 0)
                {
                    _pendingPanels = null;
                    return;
                }

                RibbonBuilder.CreatePanel(application, _pendingPanels.Dequeue());
            }
            catch
            {
                _pendingPanels = null;
                throw;
            }
        }
    }
}

