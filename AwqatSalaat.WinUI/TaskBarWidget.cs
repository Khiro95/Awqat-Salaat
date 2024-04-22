using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using UIAutomationClient;
using Windows.Graphics;

namespace AwqatSalaat.WinUI
{
    internal class TaskBarWidget : IDisposable
    {
        private const string WidgetClassName = "AwqatSalaatWidgetWinRT";
        private const string TaskBarClassName = "Shell_TrayWnd";
        private const string NotificationAreaClassName = "TrayNotifyWnd";
        private const string WidgetsButtonAutomationId = "WidgetsButton";
        private const int WidgetHostWidth = 118; // 110 for the button + 4 for left margin + 4 for right margin

        private static IntPtr hwndShell;
        private static IntPtr hwndTrayNotify;
        private static RECT taskbarRect;
        private static RECT trayNotifyRect;

        private IntPtr hwnd;
        private AppWindow appWindow;
        private DesktopWindowXamlSource host;
        private ManagementEventWatcher watcher;
        private WndProc wndProc;
        private bool? isTaskBarCentered;
        private bool? isTaskBarWidgetsEnabled;
        private bool disposedValue;

        public event EventHandler Destroying;

        public TaskBarWidget()
        {
            hwndShell = User32.FindWindow(TaskBarClassName, null);
            hwndTrayNotify = User32.FindWindowEx(hwndShell, IntPtr.Zero, NotificationAreaClassName , null);
        }

        public void Initialize()
        {
            host = new DesktopWindowXamlSource();

            hwnd = CreateHostWindow(hwndShell);

            var id = Win32Interop.GetWindowIdFromWindow(hwnd);

            appWindow = AppWindow.GetFromWindowId(id);
            appWindow.IsShownInSwitchers = false;
            appWindow.Destroying += AppWindow_Destroying;

            taskbarRect = SystemInfos.GetTaskBarBounds();
            appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, taskbarRect.bottom - taskbarRect.top));

            User32.GetWindowRect(hwndTrayNotify, out trayNotifyRect);
            UpdatePosition();

            host.Initialize(id);
            host.Content = new WidgetSummary() { MaxWidth = 110, MaxHeight = 40 };

            User32.SetParent(hwnd, hwndShell);

            CreateRegistryWatcher();
        }

        private void AppWindow_Destroying(AppWindow sender, object args)
        {
            appWindow.Destroying -= AppWindow_Destroying;
            Destroying?.Invoke(this, EventArgs.Empty);
        }

        public void Show() => appWindow.Show(false);

        public void Destroy() => appWindow.Destroy();

        public void UpdatePosition(bool forceUpdate = false)
        {
            bool isCentered = SystemInfos.IsTaskBarCentered();
            bool isWidgetsEnabled = SystemInfos.IsTaskBarWidgetsEnabled();

            if (forceUpdate || (isCentered != isTaskBarCentered) || (isWidgetsEnabled != isTaskBarWidgetsEnabled))
            {
                int offsetX = 0;
                bool osRTL = System.Globalization.CultureInfo.InstalledUICulture.TextInfo.IsRightToLeft;

                if (isCentered)
                {
                    var widgetsButton = isWidgetsEnabled ? GetAutomationElement(WidgetsButtonAutomationId) : null;

                    if (osRTL)
                    {
                        offsetX = (widgetsButton?.CurrentBoundingRectangle.left ?? taskbarRect.right) - WidgetHostWidth;
                    }
                    else
                    {
                        offsetX = widgetsButton?.CurrentBoundingRectangle.right ?? 0;
                    }
                }
                else
                {
                    if (osRTL)
                    {
                        offsetX = trayNotifyRect.right;
                    }
                    else
                    {
                        offsetX = trayNotifyRect.left - WidgetHostWidth;
                    }
                }

                appWindow.Move(new PointInt32(offsetX, 0));

                isTaskBarCentered = isCentered;
                isTaskBarWidgetsEnabled = isWidgetsEnabled;
            }
        }

        private void RegisterWindowClass()
        {
            wndProc = new WndProc(WindowProc);
            var wndClass = new WNDCLASSEX();
            wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
            wndClass.hInstance = Kernel32.GetModuleHandle(null);
            wndClass.lpfnWndProc = wndProc;
            wndClass.lpszClassName = WidgetClassName;

            User32.RegisterClassEx(ref wndClass);
        }

        private IntPtr CreateHostWindow(IntPtr parent)
        {
            RegisterWindowClass();

            var hwnd = User32.CreateWindowEx(
                dwExStyle: WindowStylesExtended.WS_EX_LAYERED,
                lpClassName: WidgetClassName,
                lpWindowName: "WidgetHost",
                dwStyle: WindowStyles.WS_POPUP,
                x: 0, y: 0,
                nWidth: 0, nHeight: 0,
                hWndParent: parent,
                hMenu: IntPtr.Zero,
                hInstance: IntPtr.Zero,
                lpParam: IntPtr.Zero);

            return hwnd;
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
            // Taskbar alignment may have been changed or Widgets button is enabled/disabled, try to update position
            UpdatePosition();
        }

        private static IUIAutomationElement GetAutomationElement(string automationId)
        {
            IUIAutomation pUIAutomation = new CUIAutomation();
            IUIAutomationElement taskbarElement = pUIAutomation.ElementFromHandle(hwndShell);

            if (taskbarElement != null)
            {
                IUIAutomationElementArray elementArray = null;
                IUIAutomationCondition condition = pUIAutomation.CreateTrueCondition();
                elementArray = taskbarElement.FindAll(TreeScope.TreeScope_Descendants | TreeScope.TreeScope_Children, condition);

                if (elementArray != null)
                {
                    int count = elementArray.Length;

                    for (int i = 0; i <= count - 1; i++)
                    {
                        IUIAutomationElement element = elementArray.GetElement(i);

                        if (element.CurrentAutomationId == automationId)
                        {
                            return element;
                        }
                    }
                }
            }

            return null;
        }

        private static IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            return User32.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    watcher.Dispose();
                    host.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TaskBarWidget()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
