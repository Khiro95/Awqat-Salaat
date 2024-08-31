using AwqatSalaat.Helpers;
using System;
using System.Management;
using System.Security.Principal;
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

        private bool widgetsButtonEnabled;
        private ManagementEventWatcher watcher;

        public TaskbarStructureWatcher(IntPtr hwndTaskbar, Action changeCallback)
        {
            taskbarElement = pUIAutomation.ElementFromHandle(hwndTaskbar);
            this.changeCallback = changeCallback;
            RegisterEventHandlers();
            widgetsButtonEnabled = SystemInfos.IsTaskBarWidgetsEnabled();
            CreateRegistryWatcher();
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

        private void CreateRegistryWatcher()
        {
            var currentUser = WindowsIdentity.GetCurrent();

            WqlEventQuery query = new WqlEventQuery(
                "SELECT * FROM RegistryKeyChangeEvent WHERE " +
                 "Hive = 'HKEY_USERS' " +
                 @"AND KeyPath = '" + currentUser.User.Value + @"\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced'");

            query.WithinInterval = new TimeSpan(0, 0, 0, 1);

            watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += new EventArrivedEventHandler(RegistryKeyChanged);
            watcher.Start();
        }

        private void RegistryKeyChanged(object sender, EventArrivedEventArgs e)
        {
            bool isWidgetsButtonEnabled = SystemInfos.IsTaskBarWidgetsEnabled();

            if (isWidgetsButtonEnabled != widgetsButtonEnabled)
            {
                widgetsButtonEnabled = isWidgetsButtonEnabled;
                changeCallback?.Invoke();
            }
        }

        public void Dispose()
        {
            Task.Run(UnregisterEventHandlers);
            watcher?.Dispose();
        }
    }
}
