using System;
using System.Threading.Tasks;
using UIAutomationClient;

namespace AwqatSalaat.WinUI
{
    internal class TaskbarStructureWatcher : IUIAutomationStructureChangedEventHandler, IUIAutomationPropertyChangedEventHandler, IDisposable
    {
        private const int UIA_BoundingRectanglePropertyId = 30001;
        private const int UIA_AutomationIdPropertyId = 30011;

        private static readonly IUIAutomation pUIAutomation = new CUIAutomation();

        private readonly IUIAutomationElement taskbarElement;
        private readonly Action changeCallback;

        public TaskbarStructureWatcher(IntPtr hwndTaskbar, Action changeCallback)
        {
            taskbarElement = pUIAutomation.ElementFromHandle(hwndTaskbar);
            this.changeCallback = changeCallback;
            RegisterEventHandlers();
        }

        public IUIAutomationElement GetAutomationElement(string automationId)
        {
            if (taskbarElement is not null)
            {
                IUIAutomationCondition condition = pUIAutomation.CreatePropertyCondition(UIA_AutomationIdPropertyId, automationId);
                return taskbarElement.FindFirst(TreeScope.TreeScope_Descendants | TreeScope.TreeScope_Children, condition);
            }

            return null;
        }

        void IUIAutomationStructureChangedEventHandler.HandleStructureChangedEvent(IUIAutomationElement sender, StructureChangeType changeType, Array runtimeId)
        {
            if (sender.CurrentName == taskbarElement.CurrentName)
            {
                changeCallback?.Invoke();
            }
        }

        void IUIAutomationPropertyChangedEventHandler.HandlePropertyChangedEvent(IUIAutomationElement sender, int propertyId, object newValue)
        {
            if (sender.CurrentName == taskbarElement.CurrentName)
            {
                changeCallback?.Invoke();
            }
        }

        private void RegisterEventHandlers()
        {
            if (taskbarElement is not null)
            {
                pUIAutomation.AddStructureChangedEventHandler(taskbarElement, TreeScope.TreeScope_Subtree, null, this);
                pUIAutomation.AddPropertyChangedEventHandler(taskbarElement, TreeScope.TreeScope_Children, null, this, new int[] { UIA_BoundingRectanglePropertyId });
            }
        }

        private void UnregisterEventHandlers()
        {
            if (taskbarElement is not null)
            {
                pUIAutomation.RemoveAllEventHandlers();
            }
        }

        public void Dispose()
        {
            Task.Run(UnregisterEventHandlers);
        }
    }
}
