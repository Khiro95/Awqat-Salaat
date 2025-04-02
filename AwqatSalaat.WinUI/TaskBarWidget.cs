using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Controls;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace AwqatSalaat.WinUI
{
    internal class TaskBarWidget : IDisposable
    {
        private const string TaskBarClassName = "Shell_TrayWnd";
        private const string ReBarWindow32ClassName = "ReBarWindow32";
        private const string NotificationAreaClassName = "TrayNotifyWnd";
        private const string WidgetsButtonAutomationId = "WidgetsButton";
        private const int DefaultWidgetHostWidth = 126; // 118 for the button (2 for borders) + 4 for left margin + 4 for right margin
        private const int CompactWidgetHostWidth = 70; // 62 for the button (2 for borders) + 4 for left margin + 4 for right margin

        private static readonly bool IsRtlUI = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private readonly double dpiScale;

        private readonly IntPtr hwndShell;
        private readonly IntPtr hwndTrayNotify;
        private readonly IntPtr hwndReBar;
        private readonly string WidgetClassName = "AwqatSalaatWidgetWinRT";

        private readonly TaskbarStructureWatcher taskbarWatcher;

        private IntPtr hwnd;
        private AppWindow appWindow;
        private WidgetSummary widgetSummary;
        private DesktopWindowXamlSource host;
        private WndProc wndProc;
        private int WidgetHostWidth;
        private int currentOffsetX = int.MinValue;
        private int currentOffsetY = 0;
        private bool isDragging;
        private int draggingInnerOffsetX;
        private int lastCursorPositionX;
        private bool initialized;
        private bool destroyed;
        private bool disposedValue;

        public IntPtr Handle => hwnd != IntPtr.Zero ? hwnd : throw new InvalidOperationException("The widget is not initialized.");

        public event EventHandler Destroying;

        public TaskBarWidget()
        {
            hwndShell = User32.FindWindow(TaskBarClassName, null);
            hwndTrayNotify = User32.FindWindowEx(hwndShell, IntPtr.Zero, NotificationAreaClassName , null);
            hwndReBar = User32.FindWindowEx(hwndShell, IntPtr.Zero, ReBarWindow32ClassName , null);

            var dpi = User32.GetDpiForWindow(hwndShell);
            dpiScale = dpi / 96d;
            WidgetHostWidth = (int)Math.Ceiling(dpiScale * DefaultWidgetHostWidth);
            Log.Debug($"Widget constructor: DPI={dpi}, Width={WidgetHostWidth}, Taskbar RTL={IsRtlUI}");

            taskbarWatcher = new TaskbarStructureWatcher(hwndShell, hwndReBar);
            taskbarWatcher.TaskbarChangedNotificationStarted += TaskbarWatcher_TaskbarChangedNotificationStarted;
            taskbarWatcher.TaskbarChangedNotificationCompleted += TaskbarWatcher_TaskbarChangedNotificationCompleted;

            // Workaround for issues caused by Start11 v2
            var hwndStart11Taskbar = User32.FindWindowEx(hwndShell, IntPtr.Zero, "#32770", "SDTaskbar");

            if (hwndStart11Taskbar != IntPtr.Zero)
            {
                WidgetClassName = "#32770";
            }
        }

        public void Initialize()
        {
            Log.Information("Initializing widget host");
            host = new DesktopWindowXamlSource();

            hwnd = CreateHostWindow(hwndShell);

            var id = Win32Interop.GetWindowIdFromWindow(hwnd);

            appWindow = AppWindow.GetFromWindowId(id);
            appWindow.IsShownInSwitchers = false;
            appWindow.Destroying += AppWindow_Destroying;

            var taskbarRect = SystemInfos.GetTaskBarBounds();
            appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, taskbarRect.bottom - taskbarRect.top));
            Log.Debug("Taskbar rect: {@rect}", taskbarRect);

            host.Initialize(id);
            host.SiteBridge.ResizePolicy = Microsoft.UI.Content.ContentSizePolicy.ResizeContentToParentWindow;
            widgetSummary = new WidgetSummary() { Margin = new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0), MaxHeight = 40 };
            widgetSummary.DisplayModeChanged += WidgetSummary_DisplayModeChanged;
            host.Content = new GridEx
            {
                Children = { widgetSummary },
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Transparent)
            };

            InjectIntoTaskbar();

            UpdatePositionImpl(TaskbarChangeReason.None);

            initialized = true;
            Log.Information("Widget host initialization done");
        }

        private void TaskbarWatcher_TaskbarChangedNotificationStarted(object sender, TaskbarChangedEventArgs e)
        {
            if (e.IsTaskbarHidden || !initialized)
            {
                e.Canceled = true;
                return;
            }

            int savedOffsetX = Properties.Settings.Default.CustomPosition;

            // -1 means the user didn't set manual position
            if (savedOffsetX == -1 && e.Reason == TaskbarChangeReason.Alignment)
            {
                Log.Debug("Exciting an animation (hide) due to taskbar alignment change");
                // This only to make the widget show an animation :)
                widgetSummary.DispatcherQueue.TryEnqueue(() => (host.Content as GridEx).Children.Clear());
            }
            else if (savedOffsetX > -1 && e.Reason != TaskbarChangeReason.TabletMode)
            {
                e.Canceled = true;
            }
        }

        private void TaskbarWatcher_TaskbarChangedNotificationCompleted(object sender, TaskbarChangedEventArgs e)
        {
            if (initialized)
            {
                UpdatePositionImpl(e.Reason, e.IsTaskbarCentered, e.IsTaskbarWidgetsEnabled);
            }
        }

        private void InjectIntoTaskbar()
        {
            Log.Information("Attempting to inject widget into taskbar");
            int attempts = 0;

            while (attempts++ <= 3)
            {
                Log.Debug($"Attempt #{attempts} to inject the widget");
                var result = User32.SetParent(hwnd, hwndShell);

                if (result != IntPtr.Zero)
                {
                    Log.Information("Widget injected successfully");
                    return;
                }

                System.Threading.Thread.Sleep(1000);
            }

            Dispose();
            
            throw new WidgetNotInjectedException("Could not inject the widget into the taskbar.\nThe taskbar may be in use.");
        }

        private void WidgetSummary_DisplayModeChanged(DisplayMode displayMode)
        {
            Log.Information($"Display mode changed: {displayMode}");
            int width = DefaultWidgetHostWidth;

            if (displayMode is DisplayMode.Compact or DisplayMode.CompactNoCountdown)
            {
                width = CompactWidgetHostWidth;
            }
            
            width = (int)Math.Ceiling(dpiScale * width);

            if (width != WidgetHostWidth)
            {
                WidgetHostWidth = width;

                Log.Debug($"Resizing widget host to new width: {width}");
                appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, appWindow.Size.Height));

                UpdatePosition();
            }
        }

        private void AppWindow_Destroying(AppWindow sender, object args)
        {
            Log.Information("Widget host is being destroyed");
            appWindow.Destroying -= AppWindow_Destroying;
            Destroying?.Invoke(this, EventArgs.Empty);
            destroyed = true;
        }

        public void Show()
        {
            Log.Debug("Showing widget host's appwindow");
            appWindow.Show(false);
        }

        public void Destroy()
        {
            Log.Debug("Destroying widget host's appwindow");
            appWindow.Destroy();
        }

        public void UpdatePosition(bool force = false, TaskbarChangeReason reason = TaskbarChangeReason.None)
        {
            if (force && Properties.Settings.Default.CustomPosition != -1)
            {
                Log.Information("Resetting widget position to auto mode");
                Properties.Settings.Default.CustomPosition = -1;

                if (Properties.Settings.Default.IsConfigured)
                {
                    Properties.Settings.Default.Save();
                }
            }

            Task.Run(() => UpdatePositionImpl(reason));
        }

        private void UpdatePositionImpl(TaskbarChangeReason changeReason)
        {
            bool isCentered = SystemInfos.IsTaskBarCentered();
            bool isWidgetsEnabled = SystemInfos.IsTaskBarWidgetsEnabled();

            UpdatePositionImpl(changeReason, isCentered, isWidgetsEnabled);
        }

        private void UpdatePositionImpl(TaskbarChangeReason changeReason, bool isCentered, bool isWidgetsEnabled)
        {
            Log.Information($"Updating widget position. Reason={changeReason}, Taskbar centered={isCentered}, Widgets enabled={isWidgetsEnabled}");
            int offsetX = Properties.Settings.Default.CustomPosition;

            User32.GetWindowRect(hwndShell, out RECT taskbarRect);
            Log.Debug("Current taskbar rect: {@rect}", taskbarRect);

            // -1 means the user didn't set manual position, so we have to find the best one
            if (offsetX == -1)
            {
                Log.Debug("Updating position in auto mode");
                var widgetsButton = isWidgetsEnabled ? taskbarWatcher.GetAutomationElement(WidgetsButtonAutomationId) : null;
                var widgetsButtonBoundingRectangle = widgetsButton?.CurrentBoundingRectangle;
                User32.GetWindowRect(hwndTrayNotify, out RECT trayNotifyRect);
                Log.Debug("System tray rect: {@trayRect}", trayNotifyRect);
                Log.Debug("Widgets button rect: {@widgetsButtonRect}", widgetsButtonBoundingRectangle);

                if (isCentered)
                {
                    if (IsRtlUI)
                    {
                        offsetX = (widgetsButtonBoundingRectangle?.left ?? taskbarRect.right) - WidgetHostWidth;
                    }
                    else
                    {
                        offsetX = widgetsButtonBoundingRectangle?.right ?? 0;
                    }
                }
                else
                {
                    if (IsRtlUI)
                    {
                        if (widgetsButton is not null && (widgetsButtonBoundingRectangle.Value.left - trayNotifyRect.right) < WidgetHostWidth)
                        {
                            offsetX = widgetsButtonBoundingRectangle.Value.right;
                        }
                        else
                        {
                            offsetX = trayNotifyRect.right;
                        }
                    }
                    else
                    {
                        if (widgetsButton is not null && (trayNotifyRect.left - widgetsButtonBoundingRectangle.Value.right) < WidgetHostWidth)
                        {
                            offsetX = widgetsButtonBoundingRectangle.Value.left - WidgetHostWidth;
                        }
                        else
                        {
                            offsetX = trayNotifyRect.left - WidgetHostWidth;
                        }
                    }
                }

                Log.Debug($"Estimated OffsetX based on alignment: {offsetX}");

                try
                {
                    Log.Debug("Looking for other injected widgets");
                    List<IntPtr> wnds = GetOtherInjectedWindows();
                    Log.Debug($"Count of other injected widgets: {wnds.Count}");

                    foreach (var wnd in wnds)
                    {
                        User32.GetWindowRect(wnd, out var bounds);

                        if (bounds.right < offsetX || bounds.left > (offsetX + WidgetHostWidth))
                        {
                            continue;
                        }

                        Log.Debug("Found an overlapping injected widget: {@bounds}", bounds);

                        if (isCentered == IsRtlUI)
                        {
                            offsetX = bounds.left - WidgetHostWidth;
                        }
                        else
                        {
                            offsetX = bounds.right;
                        }
                    }

                    Log.Debug($"Estimated OffsetX after looking for other injected widgets: {offsetX}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error occured while looking for other injected widgets: {ex.Message}");
#if DEBUG
                    throw;
#endif
                }

                if (IsRtlUI)
                {
                    offsetX = Math.Clamp(offsetX, trayNotifyRect.right, taskbarRect.right - WidgetHostWidth);
                }
                else
                {
                    offsetX = Math.Clamp(offsetX, 0, trayNotifyRect.left - WidgetHostWidth);
                }

                Log.Debug($"Final OffsetX: {offsetX} (previous={currentOffsetX})");
            }

            User32.GetWindowRect(hwndReBar, out RECT barRect);
            int offsetY = barRect.top - taskbarRect.top;
            Log.Debug($"Final OffsetY: {offsetY} (previous={currentOffsetY})");

            if (currentOffsetY != offsetY)
            {
                appWindow.MoveAndResize(new RectInt32(offsetX, offsetY, WidgetHostWidth, barRect.bottom - barRect.top));
                currentOffsetX = offsetX;
                currentOffsetY = offsetY;
            }
            else if (currentOffsetX != offsetX)
            {
                appWindow.Move(new PointInt32(offsetX, offsetY));
                currentOffsetX = offsetX;
            }

            // This only to make the widget show an animation :)
            widgetSummary.DispatcherQueue.TryEnqueue(() =>
            {
                var grid = host.Content as GridEx;

                if (changeReason == TaskbarChangeReason.Alignment || grid.Children.Count == 0)
                {
                    Log.Debug("Exciting an animation (show) due to taskbar alignment change");
                    grid.Children.Add(widgetSummary);
                }
            });
        }

        public void StartDragging()
        {
            if (!isDragging)
            {
                Log.Information("Start dragging widget");
                widgetSummary.IsHitTestVisible = false;
                User32.SetCursorPos(appWindow.Position.X + appWindow.Size.Width / 2, appWindow.Position.Y + appWindow.Size.Height / 2);
                host.Content.KeyUp += Content_KeyUp;
                host.Content.PointerPressed += Content_PointerPressed;
                host.Content.PointerReleased += Content_PointerReleased;
                (host.Content as GridEx).SetCursor(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
                isDragging = true;

                // If the command is triggered from tray menu then we need to make the window focused to receive keyboard events
                if (!host.HasFocus && hwnd != User32.GetForegroundWindow())
                {
                    User32.SetForegroundWindow(hwnd);
                }
            }
        }

        public void EndDragging(bool revert)
        {
            if (isDragging)
            {
                Log.Information("End dragging widget");
                isDragging = false;
                host.Content.ReleasePointerCaptures();
                host.Content.KeyUp -= Content_KeyUp;
                host.Content.PointerMoved -= Content_PointerMoved;
                host.Content.PointerPressed -= Content_PointerPressed;
                host.Content.PointerReleased -= Content_PointerReleased;
                (host.Content as GridEx).ResetCursor();
                widgetSummary.IsHitTestVisible = true;
                
                if (revert)
                {
                    appWindow.Move(new PointInt32(currentOffsetX, currentOffsetY));
                }
                else
                {
                    Log.Information($"New offset={appWindow.Position.X}; Old position={currentOffsetX}");
                    currentOffsetX = appWindow.Position.X;
                    Properties.Settings.Default.CustomPosition = currentOffsetX;

                    if (Properties.Settings.Default.IsConfigured)
                    {
                        Properties.Settings.Default.Save(); 
                    }
                }
            }
        }

        private void Content_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape && isDragging)
            {
                Log.Information("Pressed on Esc key while dragging widget");
                EndDragging(true);
            }
        }

        private void Content_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndDragging(false);
        }

        private void Content_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            host.Content.PointerMoved += Content_PointerMoved;
            host.Content.CapturePointer(e.Pointer);
            User32.GetCursorPos(out var lpPoint);
            lastCursorPositionX = lpPoint.x;
            draggingInnerOffsetX = lpPoint.x - appWindow.Position.X;
        }

        private void Content_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            User32.GetWindowRect(hwndShell, out RECT taskbarRect);
            User32.GetWindowRect(hwndTrayNotify, out RECT trayNotifyRect);
            User32.GetCursorPos(out var lpPoint);

            int minCursorX, maxCursorX;

            if (IsRtlUI)
            {
                minCursorX = trayNotifyRect.right + draggingInnerOffsetX;
                maxCursorX = taskbarRect.right - WidgetHostWidth + draggingInnerOffsetX;
            }
            else
            {
                minCursorX = draggingInnerOffsetX;
                maxCursorX = trayNotifyRect.left - WidgetHostWidth + draggingInnerOffsetX;
            }

            lpPoint.x = Math.Clamp(lpPoint.x, minCursorX, maxCursorX);

            int delta = lpPoint.x - lastCursorPositionX;
            int newX = delta + appWindow.Position.X;

            appWindow.Move(new PointInt32(newX, currentOffsetY));
            lastCursorPositionX = lpPoint.x;

            // This is necessary to make sure the content can raise keyboard events
            host.Content.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
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

        // https://stackoverflow.com/a/28055461/4644774
        private List<IntPtr> GetOtherInjectedWindows()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                User32.EnumChildWindows(hwndShell, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            if (hWnd != this.hwnd && User32.GetAncestor(hWnd, GetAncestorFlags.GA_PARENT) == hwndShell)
            {
                StringBuilder builder = new StringBuilder(256);
                User32.GetClassName(hWnd, builder, builder.MaxCapacity);
                string className = builder.ToString();

                if (!IsSystemWindow(className) && className != "#32770")
                {
                    GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);
                    List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
                    childHandles.Add(hWnd);
                }
            }

            return true;

            static bool IsSystemWindow(string className)
            {
                return className
                    // Common
                    is "Start"
                    or "TrayDummySearchControl"
                    or "ReBarWindow32"
                    or "TrayNotifyWnd"
                    // Windows 10 only
                    or "TrayButton"
                    or "DynamicContent2"
                    // Windows 11 only
                    or "Windows.UI.Core.CoreWindow"
                    or "Windows.UI.Composition.DesktopWindowContentBridge";
            }
        }

        private IntPtr WindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            var msg = (WindowMessage)uMsg;

            if (msg == WindowMessage.WM_SETTINGCHANGE && lParam != IntPtr.Zero)
            {
                string area = Marshal.PtrToStringAnsi(lParam);

                if (area is "UserInteractionMode" or "ConvertibleSlateMode")
                {
                    Log.Information($"Detected Windows settings change: {area}");
                    UpdatePosition(reason: TaskbarChangeReason.TabletMode);
                }
            }

            return User32.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Log.Information("Disposing widget host");

                    // TODO: dispose managed state (managed objects)

                    if (!destroyed)
                    {
                        appWindow?.Destroy();
                    }

                    widgetSummary.DisplayModeChanged -= WidgetSummary_DisplayModeChanged;
                    taskbarWatcher.TaskbarChangedNotificationStarted -= TaskbarWatcher_TaskbarChangedNotificationStarted;
                    taskbarWatcher.TaskbarChangedNotificationCompleted -= TaskbarWatcher_TaskbarChangedNotificationCompleted;
                    taskbarWatcher.Dispose();
                    host.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                User32.UnregisterClass(WidgetClassName, Kernel32.GetModuleHandle(null));
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~TaskBarWidget()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
