using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Management;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using UIAutomationClient;

namespace AwqatSalaat.WinUI
{
    internal class TaskbarStructureWatcher : IUIAutomationStructureChangedEventHandler, IUIAutomationPropertyChangedEventHandler, IDisposable
    {
        private class UpdateContext
        {
            // The values are based on clock resolution which is ~15 ms
            // The delays give time to animations to play
            private const int DefaultDelay = 60;
            // When taskbar alignment change we need more time for animations
            private const int AlignmentChangeShortDelay = 105;
            // When taskbar alignment change while Widgets button enabled
            // we need even more time because the final position of the button
            // is only known after the end of its translation animation
            private const int AlignmentChangeLongDelay = 360;

            public readonly DateTime Time = DateTime.Now;
            public readonly TaskbarChangeReason Reason;
            public readonly TaskbarChangedEventArgs EventArgs;
            public readonly int Delay = DefaultDelay;

            public UpdateContext(TaskbarChangedEventArgs eventArgs)
            {
                EventArgs = eventArgs;
                Reason = eventArgs.Reason;

                if (Reason == TaskbarChangeReason.Alignment)
                {
                    Delay = eventArgs.IsTaskbarWidgetsEnabled ? AlignmentChangeLongDelay : AlignmentChangeShortDelay;
                }
            }

            public int EstimateDelay(TaskbarChangeReason changeReason)
            {
                if (Reason == changeReason)
                {
                    // We are estimating delay for the first invocation in the serie
                    return Delay;
                }
                else if (Delay > DefaultDelay)
                {
                    // If an alignment change triggered the serie of events, we need to respect the initial delay
                    var delta = DateTime.Now - Time;
                    int temp = Delay - (int)delta.TotalMilliseconds;

                    if (temp > 15)
                    {
                        return temp;
                    }
                }

                return DefaultDelay;
            }
        }

        private const int UIA_BoundingRectanglePropertyId = 30001;
        private const int UIA_NamePropertyId = 30005;
        private const int UIA_AutomationIdPropertyId = 30011;

        private static readonly IUIAutomation pUIAutomation = new CUIAutomation();

        private readonly IntPtr hwndTaskbar;
        private readonly IntPtr hwndReBar;
        private readonly IUIAutomationElement taskbarElement;
        private readonly object syncRoot = new object();

        private int rebarGap;
        private bool widgetsButtonEnabled;
        private bool taskbarCentered;
        private bool taskbarHidden;
        private CancellationTokenSource updateCancellation;
        private ManagementEventWatcher watcher;
        private UpdateContext updateContext;

        // Notify listeners about a change happened in the taskbar
        public event EventHandler<TaskbarChangedEventArgs> TaskbarChangedNotificationStarted;
        // Notify listeners about a change happened in the taskbar after handling
        // the serie of events, if any, and after waiting for necessary delays
        public event EventHandler<TaskbarChangedEventArgs> TaskbarChangedNotificationCompleted;

        public TaskbarStructureWatcher(IntPtr hwndTaskbar, IntPtr hwndReBar)
        {
            this.hwndTaskbar = hwndTaskbar;
            this.hwndReBar = hwndReBar;

            IUIAutomationCacheRequest cacheReq = pUIAutomation.CreateCacheRequest();
            cacheReq.AddProperty(UIA_NamePropertyId);
            taskbarElement = pUIAutomation.ElementFromHandleBuildCache(hwndTaskbar, cacheReq);

            widgetsButtonEnabled = SystemInfos.IsTaskBarWidgetsEnabled();
            taskbarCentered = SystemInfos.IsTaskBarCentered();
            taskbarHidden = IsTaskbarHidden();
            RegisterEventHandlers();
            CreateRegistryWatcher();

            rebarGap = GetReBarGap();
        }

        private int GetReBarGap()
        {
            User32.GetWindowRect(hwndTaskbar, out var taskbarRect);
            User32.GetWindowRect(hwndReBar, out var rebarRect);
            return rebarRect.top - taskbarRect.top;
        }

        private bool IsTaskbarHidden()
        {
            IntPtr isAutoHidePtr = User32.GetProp(hwndTaskbar, "IsAutoHideEnabled");
            bool autoHide = isAutoHidePtr == (IntPtr)1;

            if (!autoHide)
            {
                return false;
            }

            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwndTaskbar);
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            User32.GetWindowRect(hwndTaskbar, out var taskbarRect);

            return taskbarRect.bottom > displayArea.OuterBounds.Height;
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
            if (sender.CachedName == taskbarElement.CachedName)
            {
                RaiseNotification();
            }
        }

        void IUIAutomationPropertyChangedEventHandler.HandlePropertyChangedEvent(IUIAutomationElement sender, int propertyId, object newValue)
        {
            if (sender.CachedName == taskbarElement.CachedName)
            {
                RaiseNotification();
            }
        }

        // Try to determine the reason of taskbar change and raise notification.
        private void RaiseNotification()
        {
            bool isWidgetsButtonEnabled = SystemInfos.IsTaskBarWidgetsEnabled();
            bool isTaskbarCentered = SystemInfos.IsTaskBarCentered();
            bool isTaskbarHidden = IsTaskbarHidden();
            // The gap between Shell_TrayWnd and ReBarWindow32 change when Table Mode is enabled/disabled.
            int gap = GetReBarGap();

            TaskbarChangeReason reason = TaskbarChangeReason.Other;

            if (isWidgetsButtonEnabled != widgetsButtonEnabled)
            {
                reason = TaskbarChangeReason.WidgetsButton;
                widgetsButtonEnabled = isWidgetsButtonEnabled;
            }
            else if (isTaskbarCentered != taskbarCentered)
            {
                reason = TaskbarChangeReason.Alignment;
                taskbarCentered = isTaskbarCentered;
            }
            else if (gap != rebarGap)
            {
                reason = TaskbarChangeReason.TabletMode;
                rebarGap = gap;
            }
            else if (isTaskbarHidden != taskbarHidden)
            {
                reason = TaskbarChangeReason.Visibility;
                taskbarHidden = isTaskbarHidden;
            }

            RaiseNotification(reason);
        }

        // Note: In some circumstances, a change in the taskbar triggers a serie of
        // events when the change is observed by the UIAutomation system.
        // Thus we try to avoid handling all events since one notification is enough.
        private void RaiseNotification(TaskbarChangeReason changeReason)
        {
            lock (syncRoot)
            {
                // Only first invocation in the serie is needed to start notification
                // Note that it's highly unexpected to have a serie of invocations triggered for different (initial) reasons.
                if (updateContext is null)
                {
                    var args = new TaskbarChangedEventArgs
                    {
                        Reason = changeReason,
                        IsTaskbarHidden = taskbarHidden,
                        IsTaskbarCentered = taskbarCentered,
                        IsTaskbarWidgetsEnabled = widgetsButtonEnabled
                    };
                    updateContext = new UpdateContext(args);
                    TaskbarChangedNotificationStarted?.Invoke(this, args);

                    if (args.Canceled)
                    {
                        updateContext = null;
                        return;
                    }
                }

                // We want to make the last invocation to be the one that do the update
                // because it will be the closest one to the actual state of the taskbar.
                if (updateCancellation is not null)
                {
                    updateCancellation.Cancel();
                    updateCancellation.Dispose();
                }

                // Delay is needed to give time for some animations on the taskbar to take place.
                // Also the Widgets button may translate to different location which make it
                // report a wrong position if we don't wait for its animation to finish.
                int delay = updateContext.EstimateDelay(changeReason);

                updateCancellation = new CancellationTokenSource();
                Task.Delay(delay, updateCancellation.Token).ContinueWith(RaiseNotificationCompletedIfSuccess);
            }
        }

        private void RaiseNotificationCompletedIfSuccess(Task delayTask)
        {
            lock (syncRoot)
            {
                // If the task didn't succeed then it means we canceled it to replace it with a more recent one.
                if (delayTask.IsCompletedSuccessfully)
                {
                    TaskbarChangedNotificationCompleted?.Invoke(this, updateContext.EventArgs);
                    updateCancellation?.Dispose();
                    updateCancellation = null;
                    updateContext = null;
                }
            }
        }

        private void RegisterEventHandlers()
        {
            if (taskbarElement is not null)
            {
                IUIAutomationCacheRequest cacheReq = pUIAutomation.CreateCacheRequest();
                cacheReq.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;
                cacheReq.AddProperty(UIA_NamePropertyId);
                pUIAutomation.AddStructureChangedEventHandler(taskbarElement, TreeScope.TreeScope_Subtree, cacheReq, this);
                pUIAutomation.AddPropertyChangedEventHandler(taskbarElement, TreeScope.TreeScope_Children, cacheReq, this, new int[] { UIA_BoundingRectanglePropertyId });
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
            bool isTaskbarCentered = SystemInfos.IsTaskBarCentered();

            // We are only interested in registry notifications for widgets button change when taskbar is left-aligned
            if ((isWidgetsButtonEnabled != widgetsButtonEnabled) && !isTaskbarCentered)
            {
                widgetsButtonEnabled = isWidgetsButtonEnabled;
                RaiseNotification(TaskbarChangeReason.WidgetsButton);
            }
        }

        public void Dispose()
        {
            Task.Run(UnregisterEventHandlers);
            watcher?.Dispose();
        }
    }

    public class TaskbarChangedEventArgs : EventArgs
    {
        public bool Canceled { get; set; }
        public TaskbarChangeReason Reason { get; init; }
        public bool IsTaskbarHidden { get; init; }
        public bool IsTaskbarCentered { get; init; }
        public bool IsTaskbarWidgetsEnabled { get; init; }
    }

    public enum TaskbarChangeReason
    {
        None, // Used for manual re-position
        Alignment,
        Visibility,
        WidgetsButton,
        TabletMode,
        Other
    }
}
