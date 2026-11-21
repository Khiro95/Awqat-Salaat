using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Controls;
using AwqatSalaat.WinUI.Helpers;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace AwqatSalaat.WinUI
{
    internal class TaskBarWidget : IDisposable
    {
        private const string WidgetsButtonAutomationId = "WidgetsButton";
        private const int DefaultWidgetHostWidth = 126; // 118 for the button (2 for borders) + 4 for left margin + 4 for right margin
        private const int CompactWidgetHostWidth = 70; // 62 for the button (2 for borders) + 4 for left margin + 4 for right margin

        private const int VerticalWidgetHostHeight = 44; // two lines of text (time + countdown) when the taskbar is on a side

        private static readonly bool IsRtlUI = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
        private static readonly string SecondaryClockClassName = SystemInfos.IsWindows10 ? "ClockButton" : "SystemTray.OmniButton";

        private readonly double dpiScale;

        private readonly IntPtr hwndShell;
        private readonly IntPtr hwndTrayNotify;
        private readonly IntPtr hwndReBar;
        private readonly string WidgetClassName = "AwqatSalaatWidgetWinRT";
        private readonly string displayPath;

        private readonly TaskbarStructureWatcher taskbarWatcher;

        private IntPtr hwnd;
        private AppWindow appWindow;
        private WidgetSummary widgetSummary;
        private DesktopWindowXamlSource host;
        private WndProc wndProc;
        private int WidgetHostWidth;
        private int WidgetHostHeight;
        private bool isVertical;
        private bool isOnRightEdge;
        private int draggingInnerOffsetY;
        private int lastCursorPositionY;
        private int currentOffsetX = int.MinValue;
        private int currentOffsetY = 0;
        private bool isDragging;
        private int draggingInnerOffsetX;
        private int lastCursorPositionX;
        private bool initialized;
        private bool destroyed;
        private bool disposedValue;

        public IntPtr Handle => hwnd != IntPtr.Zero ? hwnd : throw new InvalidOperationException("The widget is not initialized.");
        public DisplayEntity Display => DisplayHelper.AvailableDisplays.SingleOrDefault(d => d.Display.DevicePath == displayPath);

        public event EventHandler Destroying;

        public TaskBarWidget(DisplayEntity display)
        {
            displayPath = display.Display.DevicePath;
            (hwndShell, hwndReBar, hwndTrayNotify) = DisplayHelper.GetTaskbarFromDisplay(display);

            dpiScale = display.Dpi / 96d;
            WidgetHostWidth = (int)Math.Ceiling(dpiScale * DefaultWidgetHostWidth);
            Log.Debug($"Widget constructor: Display={display.Display.DevicePath}, DPI={display.Dpi}, Width={WidgetHostWidth}, Taskbar RTL={IsRtlUI}");

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

            isVertical = IsTaskbarVertical(out RECT taskbarRect);
            isOnRightEdge = IsTaskbarOnRightEdge(taskbarRect);
            Log.Debug("Taskbar rect: {@rect}, vertical={vertical}", taskbarRect, isVertical);

            if (isVertical)
            {
                WidgetHostWidth = taskbarRect.right - taskbarRect.left;
                WidgetHostHeight = (int)Math.Ceiling(dpiScale * VerticalWidgetHostHeight);
                appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, WidgetHostHeight));
            }
            else
            {
                appWindow.ResizeClient(new SizeInt32(WidgetHostWidth, taskbarRect.bottom - taskbarRect.top));
            }

            host.Initialize(id);
            host.SiteBridge.ResizePolicy = Microsoft.UI.Content.ContentSizePolicy.ResizeContentToParentWindow;
            widgetSummary = new WidgetSummary() { Margin = isVertical ? new Microsoft.UI.Xaml.Thickness(0) : new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0), MaxHeight = 40 };
            widgetSummary.SetTaskbarOrientation(isVertical, isOnRightEdge);
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
            if (e.IsTaskbarHidden || !initialized || destroyed)
            {
                e.Canceled = true;
                return;
            }

            int savedOffsetX = Properties.Settings.Default.CustomPosition;

            // -1 means the user didn't set manual position
            if (savedOffsetX == -1 && e.Reason == TaskbarChangeReason.Alignment)
            {
                Log.Debug("Triggering an animation (hide) due to taskbar alignment change");
                // This only to make the widget show an animation :)
                widgetSummary.DispatcherQueue.TryEnqueue(() => (host.Content as GridEx).Children.Clear());
            }
            else if (savedOffsetX > -1 && e.Reason != TaskbarChangeReason.TabletMode && !HasOrientationChanged())
            {
                e.Canceled = true;
            }
        }

        private void TaskbarWatcher_TaskbarChangedNotificationCompleted(object sender, TaskbarChangedEventArgs e)
        {
            if (initialized && !destroyed)
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

            if (isVertical)
            {
                // In a vertical taskbar the widget always spans the taskbar's width, see UpdatePositionVerticalAsync
                return;
            }

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

        private async Task UpdatePositionImpl(TaskbarChangeReason changeReason, bool isCentered, bool isWidgetsEnabled)
        {
            Log.Information($"Updating widget position. Reason={changeReason}, Taskbar centered={isCentered}, Widgets enabled={isWidgetsEnabled}");

            bool vertical = IsTaskbarVertical(out RECT shellRect);

            if (vertical != isVertical)
            {
                Log.Information($"Taskbar orientation changed. Vertical={vertical}");
                isVertical = vertical;
                currentOffsetX = int.MinValue;
                currentOffsetY = int.MinValue;

                if (!vertical)
                {
                    WidgetHostWidth = (int)Math.Ceiling(dpiScale * DefaultWidgetHostWidth);
                }
            }

            isOnRightEdge = IsTaskbarOnRightEdge(shellRect);

            if (vertical)
            {
                await UpdatePositionVerticalAsync(changeReason, shellRect);
                return;
            }

            int offsetX = Properties.Settings.Default.CustomPosition;

            RECT taskbarRect = GetTaskbarRect();

            // -1 means the user didn't set manual position, so we have to find the best one
            if (offsetX == -1)
            {
                Log.Debug("Updating position in auto mode");
                var widgetsButton = isWidgetsEnabled ? await taskbarWatcher.GetAutomationElementAsync(WidgetsButtonAutomationId) : null;
                var widgetsButtonRect = widgetsButton?.CurrentBoundingRectangle;

                // convert to local coords
                if (widgetsButtonRect.HasValue)
                {
                    var displayPosition = Display.CachedSettings.Position;
                    var wbRect = widgetsButtonRect.Value;
                    wbRect.left -= displayPosition.X;
                    wbRect.right -= displayPosition.X;
                    wbRect.top -= displayPosition.Y;
                    wbRect.bottom -= displayPosition.Y;
                    widgetsButtonRect = wbRect;
                }

                RECT trayNotifyRect = await GetTrayAreaRectAsync();

                Log.Debug("Widgets button rect: {@widgetsButtonRect}", widgetsButtonRect);

                if (isCentered)
                {
                    if (IsRtlUI)
                    {
                        offsetX = (widgetsButtonRect?.left ?? taskbarRect.right) - WidgetHostWidth;
                    }
                    else
                    {
                        offsetX = widgetsButtonRect?.right ?? 0;
                    }
                }
                else
                {
                    if (IsRtlUI)
                    {
                        if (widgetsButton is not null && (widgetsButtonRect.Value.left - trayNotifyRect.right) < WidgetHostWidth)
                        {
                            offsetX = widgetsButtonRect.Value.right;
                        }
                        else
                        {
                            offsetX = trayNotifyRect.right;
                        }
                    }
                    else
                    {
                        if (widgetsButton is not null && (trayNotifyRect.left - widgetsButtonRect.Value.right) < WidgetHostWidth)
                        {
                            offsetX = widgetsButtonRect.Value.left - WidgetHostWidth;
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

            RefreshWidgetAfterMove(changeReason);
        }

        private void RefreshWidgetAfterMove(TaskbarChangeReason changeReason)
        {
            widgetSummary.DispatcherQueue.TryEnqueue(() =>
            {
                widgetSummary.Margin = isVertical ? new Microsoft.UI.Xaml.Thickness(0) : new Microsoft.UI.Xaml.Thickness(4, 0, 4, 0);
                widgetSummary.SetTaskbarOrientation(isVertical, isOnRightEdge);
                InvalidateElementsAlignment(Properties.Settings.Default.AutoAlignment);

                // This only to make the widget show an animation :)
                var grid = host.Content as GridEx;

                if (changeReason == TaskbarChangeReason.Alignment || grid.Children.Count == 0)
                {
                    Log.Debug("Triggering an animation (show) due to taskbar alignment change");
                    grid.Children.Add(widgetSummary);
                }
            });
        }

        // ---- Vertical taskbar (taskbar docked to the left or right edge) ----

        private bool IsTaskbarVertical(out RECT shellRect)
        {
            User32.GetWindowRect(hwndShell, out shellRect);
            return (shellRect.bottom - shellRect.top) > (shellRect.right - shellRect.left);
        }

        private bool HasOrientationChanged() => IsTaskbarVertical(out _) != isVertical;

        private bool IsTaskbarOnRightEdge(RECT shellRect)
        {
            var settings = Display.CachedSettings;
            int centerX = (shellRect.left + shellRect.right) / 2 - settings.Position.X;

            return centerX > settings.Resolution.Width / 2;
        }

        // Allowed range for the widget's top edge, relative to the top of the taskbar.
        // The widget stays clear of the system tray area, wherever the taskbar puts it.
        private (int min, int max) GetVerticalRange(RECT shellRect)
        {
            int taskbarHeight = shellRect.bottom - shellRect.top;
            int min = 0;
            int max = Math.Max(0, taskbarHeight - WidgetHostHeight);

            if (hwndTrayNotify != IntPtr.Zero)
            {
                User32.GetWindowRect(hwndTrayNotify, out RECT tray);

                if (tray.bottom > tray.top)
                {
                    Log.Debug("System tray rect: {@trayRect}", tray);
                    int trayCenter = (tray.top + tray.bottom) / 2 - shellRect.top;

                    if (trayCenter > taskbarHeight / 2)
                    {
                        // tray at the bottom
                        max = Math.Clamp(tray.top - shellRect.top - WidgetHostHeight, 0, max);
                    }
                    else
                    {
                        // tray at the top
                        min = Math.Clamp(tray.bottom - shellRect.top, 0, max);
                    }
                }
            }

            return (min, Math.Max(min, max));
        }

        private async Task UpdatePositionVerticalAsync(TaskbarChangeReason changeReason, RECT shellRect)
        {
            Log.Information("Updating widget position for a vertical taskbar");

            // A child window is positioned relatively to its parent (the taskbar)
            int taskbarHeight = shellRect.bottom - shellRect.top;
            WidgetHostWidth = shellRect.right - shellRect.left;
            WidgetHostHeight = (int)Math.Ceiling(dpiScale * VerticalWidgetHostHeight);

            var (min, max) = GetVerticalRange(shellRect);
            int offsetY = Properties.Settings.Default.CustomPosition;

            // -1 means the user didn't set manual position, so we put the widget next to the system tray area
            if (offsetY == -1)
            {
                offsetY = max;

                try
                {
                    // Avoid overlapping other injected widgets
                    foreach (var wnd in GetOtherInjectedWindows())
                    {
                        User32.GetWindowRect(wnd, out var bounds);
                        int top = bounds.top - shellRect.top;
                        int bottom = bounds.bottom - shellRect.top;

                        if (bottom <= offsetY || top >= offsetY + WidgetHostHeight)
                        {
                            continue;
                        }

                        Log.Debug("Found an overlapping injected widget: {@bounds}", bounds);
                        offsetY = top - WidgetHostHeight;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error occured while looking for other injected widgets: {ex.Message}");
#if DEBUG
                    throw;
#endif
                }
            }

            offsetY = Math.Clamp(offsetY, min, max);
            Log.Debug($"Final OffsetY: {offsetY} (previous={currentOffsetY}); taskbar height={taskbarHeight}, host={WidgetHostWidth}x{WidgetHostHeight}");

            appWindow.MoveAndResize(new RectInt32(0, offsetY, WidgetHostWidth, WidgetHostHeight));
            currentOffsetX = 0;
            currentOffsetY = offsetY;

            RefreshWidgetAfterMove(changeReason);

            await Task.CompletedTask;
        }

        private void DragVertically()
        {
            IsTaskbarVertical(out RECT shellRect);
            var (min, max) = GetVerticalRange(shellRect);

            User32.GetCursorPos(out var lpPoint);

            int minCursorY = shellRect.top + min + draggingInnerOffsetY;
            int maxCursorY = shellRect.top + max + draggingInnerOffsetY;
            lpPoint.y = Math.Clamp(lpPoint.y, minCursorY, maxCursorY);

            int delta = lpPoint.y - lastCursorPositionY;
            int newY = delta + appWindow.Position.Y - shellRect.top;

            appWindow.Move(new PointInt32(0, newY));
            lastCursorPositionY = lpPoint.y;

            // This is necessary to make sure the content can raise keyboard events
            host.Content.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }

        private RECT GetTaskbarRect()
        {
            User32.GetWindowRect(hwndShell, out RECT taskbarRect);
            Log.Debug("Current taskbar rect: {@rect}", taskbarRect);

            // convert to local coords
            var displaySettings = Display.CachedSettings;
            taskbarRect.left -= displaySettings.Position.X;
            taskbarRect.right -= displaySettings.Position.X;
            taskbarRect.top -= displaySettings.Position.Y;
            taskbarRect.bottom -= displaySettings.Position.Y;

            return taskbarRect;
        }

        private async Task<RECT> GetTrayAreaRectAsync()
        {
            User32.GetWindowRect(hwndTrayNotify, out RECT trayNotifyRect);

            // In secondary displays there is no tray area, so we set the values of system clock
            if (hwndTrayNotify == IntPtr.Zero)
            {
                Log.Debug("There is no system tray area, use clock element instead");
                var clockElement = await taskbarWatcher.GetAutomationElementByClassNameAsync(SecondaryClockClassName);
                var displayPosX = Display.CachedSettings.Position.X;

                if (IsRtlUI)
                {
                    trayNotifyRect.right = clockElement.CurrentBoundingRectangle.right - displayPosX;
                }
                else
                {
                    trayNotifyRect.left = clockElement.CurrentBoundingRectangle.left - displayPosX;
                }
            }
            
            Log.Debug("System tray rect: {@trayRect}", trayNotifyRect);

            return trayNotifyRect;
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
                (host.Content as GridEx).SetCursor(isVertical ? Microsoft.UI.Input.InputSystemCursorShape.SizeNorthSouth : Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
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
                else if (isVertical)
                {
                    IsTaskbarVertical(out RECT shellRect);
                    int newY = appWindow.Position.Y - shellRect.top;
                    Log.Information($"New vertical offset={newY}; Old position={currentOffsetY}");
                    currentOffsetY = newY;
                    Properties.Settings.Default.CustomPosition = currentOffsetY;

                    if (Properties.Settings.Default.IsConfigured)
                    {
                        Properties.Settings.Default.Save();
                    }
                }
                else
                {
                    int newX = appWindow.Position.X - Display.CachedSettings.Position.X;
                    Log.Information($"New offset={newX}; Old position={currentOffsetX}");
                    currentOffsetX = newX;
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
            draggingInnerOffsetY = lpPoint.y - appWindow.Position.Y;
            lastCursorPositionY = lpPoint.y;
            // The order is VERY IMPORTANT!
            draggingInnerOffsetX = lpPoint.x - appWindow.Position.X;
            lpPoint.x -= Display.CachedSettings.Position.X;
            lastCursorPositionX = lpPoint.x;
        }

        private async void Content_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isVertical)
            {
                if (isDragging)
                {
                    DragVertically();
                }

                return;
            }

            RECT taskbarRect = GetTaskbarRect();
            RECT trayNotifyRect = await GetTrayAreaRectAsync();

            if (!isDragging)
            {
                return;
            }

            var displayPosX = Display.CachedSettings.Position.X;
            User32.GetCursorPos(out var lpPoint);
            lpPoint.x -= displayPosX;

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
            int newX = delta + appWindow.Position.X - displayPosX;

            appWindow.Move(new PointInt32(newX, currentOffsetY));
            lastCursorPositionX = lpPoint.x;
            InvalidateElementsAlignment(Properties.Settings.Default.AutoAlignment);

            // This is necessary to make sure the content can raise keyboard events
            host.Content.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }

        public void InvalidateElementsAlignment(bool autoAlign)
        {
            if (isVertical)
            {
                widgetSummary.ElementsAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                return;
            }

            if (autoAlign)
            {
                User32.GetWindowRect(hwndShell, out RECT taskbarRect);
                int taskbarWidth = taskbarRect.right - taskbarRect.left;
                int widgetMiddleX = appWindow.Position.X + appWindow.Size.Width / 2 - Display.CachedSettings.Position.X;
                float resolution = 0.15f;
                bool condition = IsRtlUI ? widgetMiddleX >= taskbarWidth * (1 - resolution) : widgetMiddleX <= taskbarWidth * resolution;

                if (condition && IsRtlUI == LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft)
                {
                    widgetSummary.ElementsAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Left;
                    return;
                }
            }

            widgetSummary.ElementsAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
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

            var displayPosition = Display.CachedSettings.Position;

            var hwnd = User32.CreateWindowEx(
                dwExStyle: WindowStylesExtended.WS_EX_LAYERED,
                lpClassName: WidgetClassName,
                lpWindowName: "WidgetHost",
                dwStyle: WindowStyles.WS_POPUP,
                x: displayPosition.X, y: displayPosition.Y,
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
                    or "WorkerW"
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
