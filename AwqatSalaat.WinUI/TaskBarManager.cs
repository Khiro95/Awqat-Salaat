using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.WinUI
{
    internal static class TaskBarManager
    {
        public const string PrimaryDisplayOption = "PRIMARY";
        // Show a widget in the taskbar of every display
        public const string AllDisplaysOption = "ALL";

        // The taskbar of a secondary display may be created a little after the display itself (or after the primary taskbar)
        private const int MissingTaskbarRetryDelay = 2000;
        private const int MissingTaskbarMaxRetries = 6;

        private static readonly System.Drawing.Icon AppIcon;
        // One widget per display. There is more than one only when the user choose to show the widget on all displays
        private static readonly List<TaskBarWidget> taskBarWidgets = new List<TaskBarWidget>();
        private static readonly object recreateSyncRoot = new object();
        private static TrayIconWithContextMenu trayIcon;
        private static DispatcherQueue dispatcher;
        private static CancellationTokenSource recreateCancellation;
        private static int missingTaskbarRetries;

        private static PopupMenuItem showItem;
        private static PopupMenuItem hideItem;
        private static PopupMenuItem repositionItem;
        private static PopupMenuItem manualPositionItem;
        private static PopupMenuItem quitItem;
        private static PopupMenuItem primaryItem;
        private static PopupMenuItem allDisplaysItem;
        private static PopupSubMenu displaySubMenu;
        private static PopupMenuSeparator displaySeparator = new PopupMenuSeparator();

        private static string latestDisplay;
        private static bool isPurposelyHidden;

        private static bool IsWidgetVisible => taskBarWidgets.Count > 0;
        private static TaskBarWidget MainWidget => taskBarWidgets.FirstOrDefault(w => w.IsMain) ?? taskBarWidgets.FirstOrDefault();
        private static bool IsAllDisplaysMode => Properties.Settings.Default.Display == AllDisplaysOption && SystemInfos.ShowTaskbarOnAllDisplays();

        public static IntPtr CurrentWidgetHandle => MainWidget?.Handle ?? throw new InvalidOperationException("The taskbar widget is missing.");
        public static ICommand ShowWidget { get; }
        public static ICommand HideWidget { get; }
        public static ICommand RepositionWidget { get; }
        public static ICommand ManuallyPositionWidget { get; }
        public static ICommand SetDisplay { get; }

        static TaskBarManager()
        {
            ShowWidget = new RelayCommand(static o =>
            {
                Log.Information("Clicked on Show");
                ShowWidgetExecute();
            });
            HideWidget = new RelayCommand(static o =>
            {
                Log.Information("Clicked on Hide");
                HideWidgetExecute(showNotification: true);
            });
            RepositionWidget = new RelayCommand(static o =>
            {
                Log.Information("Clicked on Re-position");
                RepositionWidgetExecute();
            });
            ManuallyPositionWidget = new RelayCommand(static o =>
            {
                Log.Information("Clicked on Manual position");
                GetWidgetNearCursor()?.StartDragging();
            });
            SetDisplay = new RelayCommand(static o =>
            {
                string option = o.ToString();
                Log.Information("Clicked on a Display option: " + option);
                SwitchDisplay(option);
            },
            static o => (o as string) == PrimaryDisplayOption || SystemInfos.ShowTaskbarOnAllDisplays());

            App.Quitting += App_Quitting;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateTrayIconLocalization();
            DisplayHelper.DisplayChanged += DisplayHelper_DisplayChanged;
            TaskbarSettingsWatcher.SettingChanged += TaskbarSettingsWatcher_SettingChanged;
            Notification.NotificationManager.ShowWidgetRequested += NotificationManager_ShowWidgetRequested;

            AppIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }

        private static void TaskbarSettingsWatcher_SettingChanged(object sender, TaskbarSettingChangedEventArgs e)
        {
            if (e.Setting == TaskbarSetting.ShowOnAllDisplays)
            {
                bool enabled = SystemInfos.ShowTaskbarOnAllDisplays();

                foreach (var item in displaySubMenu.Items.OfType<PopupMenuItem>())
                {
                    item.Enabled = ReferenceEquals(item, primaryItem) || enabled;
                }

                if (Properties.Settings.Default.Display == AllDisplaysOption)
                {
                    // The secondary taskbars are being created/destroyed
                    Log.Information("Re-creating the widgets because showing taskbar on all displays was turned " + (enabled ? "on" : "off"));
                    ScheduleWidgetsRecreation(1000);
                }
            }
        }

        private static void DisplayHelper_DisplayChanged(object sender, DisplayChangedEventArgs e)
        {
            var affectedDisplay = e.DisplayEntity?.Display?.DevicePath;
            var displaySetting = Properties.Settings.Default.Display;

            if (IsAllDisplaysMode)
            {
                if (e.Reason != DisplayChangedReason.Unknown)
                {
                    // Taskbars are created/destroyed/moved along with their displays, so we start over.
                    // We wait a little to let the shell finish its work.
                    Log.Information($"Re-creating the widgets on all displays after a display change: {e.Reason}");
                    ScheduleWidgetsRecreation(e.Reason == DisplayChangedReason.Disconnected ? 300 : 1000);
                }
            }
            else if (e.Reason == DisplayChangedReason.PrimaryDisplay && displaySetting == PrimaryDisplayOption)
            {
                Log.Information("Moving the widget to the new primary display");
                dispatcher.TryEnqueue(HideThenShow);
            }
            else if (e.Reason is DisplayChangedReason.PrimaryDuplicated or DisplayChangedReason.PrimaryDeduplicated
                && displaySetting != PrimaryDisplayOption
                && latestDisplay == affectedDisplay)
            {
                Log.Information("Re-showing the widget because the primary display was (de)duplicated to/from the current display");
                dispatcher.TryEnqueue(HideThenShow);
            }
            // If the widget is shown in the dosconnected display, then we need to move it to another one
            else if (e.Reason == DisplayChangedReason.Disconnected && affectedDisplay == latestDisplay)
            {
                // Wait a little to ensure the widget is destroyed if it was previously visible
                Task.Delay(100).ContinueWith(t =>
                {
                    if (!isPurposelyHidden)
                    {
                        Log.Information("Showing the widget again after disconnecting the related display");
                        dispatcher.TryEnqueue(ShowWidgetExecute);
                    }
                });
            }
            // If the connected display is the one chosen by the user, then we move the widget there
            else if (e.Reason == DisplayChangedReason.Connected && affectedDisplay == displaySetting)
            {
                Log.Information("Moving the widget the user's preferred display after connecting it");
                dispatcher.TryEnqueue(HideThenShow);
            }

            UpdateDisplayTrayMenus();
        }

        private static void HideThenShow()
        {
            if (IsWidgetVisible)
            {
                HideWidgetExecute();
                ShowWidgetExecute();
            }
        }

        // Unlike HideThenShow, this also brings back the widgets that were destroyed along with their taskbars
        private static void RecreateWidgets()
        {
            if (isPurposelyHidden)
            {
                return;
            }

            try
            {
                HideWidgetExecute();
                ShowWidgetExecute();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An error occured while re-creating the widgets: {ex.Message}");
#if DEBUG
                throw;
#endif
            }
        }

        private static void ScheduleWidgetsRecreation(int delay)
        {
            CancellationToken token;

            // Display changes usually come in bursts, only the last one matters
            lock (recreateSyncRoot)
            {
                recreateCancellation?.Cancel();
                recreateCancellation?.Dispose();
                recreateCancellation = new CancellationTokenSource();
                token = recreateCancellation.Token;
            }

            Task.Delay(delay, token).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    dispatcher.TryEnqueue(RecreateWidgets);
                }
            });
        }

        private static void RepositionWidgetExecute()
        {
            foreach (var widget in taskBarWidgets.ToArray())
            {
                widget.UpdatePosition(true);
            }
        }

        // When there are many widgets, the manual positioning starts on the one that is in the same display as the cursor
        private static TaskBarWidget GetWidgetNearCursor()
        {
            if (taskBarWidgets.Count > 1 && User32.GetCursorPos(out var cursor))
            {
                foreach (var widget in taskBarWidgets)
                {
                    var settings = widget.Display?.CachedSettings;

                    if (settings is not null
                        && cursor.x >= settings.Position.X && cursor.x < settings.Position.X + settings.Resolution.Width
                        && cursor.y >= settings.Position.Y && cursor.y < settings.Position.Y + settings.Resolution.Height)
                    {
                        return widget;
                    }
                }
            }

            return MainWidget;
        }

        private static void SwitchDisplay(string target)
        {
            try
            {
                Log.Information($"Switching display to: {target}");
                bool widgetVisible = IsWidgetVisible;

                if (widgetVisible)
                {
                    HideWidgetExecute();
                }

                Properties.Settings.Default.Display = target;
                Properties.Settings.Default.CustomPosition = -1;

                if (Properties.Settings.Default.IsConfigured)
                {
                    Properties.Settings.Default.Save();
                }

                UpdateDisplayTrayMenus();

                if (widgetVisible)
                {
                    ShowWidgetExecute();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"An error occured while switching the display");
#if DEBUG
                throw;
#endif
            }
        }

        private static void NotificationManager_ShowWidgetRequested()
        {
            if (!IsWidgetVisible)
            {
                Log.Information("Showing widget after clicking on toast notification");
                dispatcher.TryEnqueue(ShowWidgetExecute);
            }
        }

        public static void Initialize(DispatcherQueue dispatcherQueue)
        {
            dispatcher = dispatcherQueue;

            if (trayIcon is null)
            {
                Log.Information("Creating system tray icon");
                showItem = new PopupMenuItem("Show", (_, _) =>
                {
                    Log.Information("Clicked on Show from tray icon");
                    dispatcher.TryEnqueue(ShowWidgetExecute);
                });
                hideItem = new PopupMenuItem("Hide", (_, _) =>
                {
                    Log.Information("Clicked on Hide from tray icon");
                    dispatcher.TryEnqueue(() => HideWidgetExecute(showNotification: true));
                });
                repositionItem = new PopupMenuItem("Re-position", (_, _) =>
                {
                    Log.Information("Clicked on Re-position from tray icon");
                    dispatcher.TryEnqueue(RepositionWidgetExecute);
                });
                manualPositionItem = new PopupMenuItem("Manual position", (_, _) =>
                {
                    Log.Information("Clicked on Manual position from tray icon");
                    dispatcher.TryEnqueue(() => GetWidgetNearCursor()?.StartDragging());
                });
                displaySubMenu = new PopupSubMenu("Display")
                {
                    Items =
                    {
                        (primaryItem = new PopupMenuItem("Always Primary", (s, _) => OnDisplayMenuItemClick((PopupMenuItem)s, PrimaryDisplayOption))),
                        (allDisplaysItem = new PopupMenuItem("All Displays", (s, _) => OnDisplayMenuItemClick((PopupMenuItem)s, AllDisplaysOption)))
                    }
                };
                quitItem = new PopupMenuItem("Quit", (_, _) =>
                {
                    Log.Information("Clicked on Quit from tray icon");
                    dispatcher.TryEnqueue(() => App.Quit.Execute(null));
                });

                trayIcon = new TrayIconWithContextMenu()
                {
                    ContextMenu = new PopupMenu
                    {
                        Items =
                        {
                            showItem,
                            hideItem,
                            new PopupMenuSeparator(),
                            repositionItem,
                            manualPositionItem,
                            displaySeparator,
                            displaySubMenu,
                            new PopupMenuSeparator(),
                            quitItem,
                        }
                    },
                    Icon = AppIcon.Handle,
                };

                UpdateTrayIconLocalization();
                trayIcon.MessageWindow.TaskbarCreated += (_, _) => dispatcher.TryEnqueue(OnTaskbarCreated);
                trayIcon.Created += TrayIcon_Created;
                trayIcon.Create();
            }

            ShowWidgetExecute();
        }

        public static void InvalidateWidgetElementsAlignment(bool autoAlign)
        {
            foreach (var widget in taskBarWidgets.ToArray())
            {
                widget.InvalidateElementsAlignment(autoAlign);
            }
        }

        private static void TrayIcon_Created(object sender, EventArgs e)
        {
            UpdateDisplayTrayMenus();

            //Unfortunately, we can't handle WM_QUERYENDSESSION and WM_ENDSESSION messages in widget's window procedure because it has a parent.
            //So instead of creating an other hidden top-level window, we subclass the window of the tray icon
            //which receives WM_QUERYENDSESSION and WM_ENDSESSION messages. Two birds with one stone :)
            try
            {
                SubclassTrayIconWindow();
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not subclass tray icon window: {ex.Message}");
#if DEBUG
                throw;
#endif
            }
        }

        private static WndProc newWndProc;
        private static IntPtr oldWndProcPtr;
        private static IntPtr previousTrayIconWindowHandle = IntPtr.Zero;

        private static void SubclassTrayIconWindow()
        {
            const int GWLP_WNDPROC = -4;

            if (trayIcon.WindowHandle != previousTrayIconWindowHandle)
            {
                Log.Information("Subclassing tray icon's message window");
                newWndProc = new WndProc(SubclassWindowProc);
                var procPtr = Marshal.GetFunctionPointerForDelegate(newWndProc);
                var oldPtr = IntPtr.Size == 8
                    ? User32.SetWindowLongPtr(trayIcon.WindowHandle, GWLP_WNDPROC, procPtr)
                    : (IntPtr)User32.SetWindowLong(trayIcon.WindowHandle, GWLP_WNDPROC, (uint)procPtr.ToInt32());

                if (oldPtr == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Log.Warning($"Could not subclass tray icon window. Error=0x{error:X2}");
                    return;
                }

                oldWndProcPtr = oldPtr;
                previousTrayIconWindowHandle = trayIcon.WindowHandle;
            }
        }

        private static IntPtr SubclassWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
        {
            const int ENDSESSION_CLOSEAPP = 0x00000001;
            var msg = (WindowMessage)uMsg;

            if (msg == WindowMessage.WM_QUERYENDSESSION && ((lParam.ToInt64() & ENDSESSION_CLOSEAPP) == ENDSESSION_CLOSEAPP))
            {
                Log.Information("The widget is queried for session ending");
                // The app is being updated so we should restart
                Kernel32.RegisterApplicationRestart(null);
                return new IntPtr(1); // true
            }
            else if (msg == WindowMessage.WM_ENDSESSION && wParam.ToInt64() == 1 && ((lParam.ToInt64() & ENDSESSION_CLOSEAPP) == ENDSESSION_CLOSEAPP))
            {
                Log.Information("The widget is asked to end session");
                dispatcher.TryEnqueue(() => App.Quit.Execute(null));
            }

            return User32.CallWindowProc(oldWndProcPtr, hWnd, uMsg, wParam, lParam);
        }

        private static void App_Quitting()
        {
            using (trayIcon)
            {
                Log.Information("Removing tray icon");
                trayIcon.TryRemove();
            }

            HideWidgetExecute();
        }

        private static void ShowWidgetExecute()
        {
            Log.Information("Showing widget");

            if (!IsWidgetVisible)
            {
                missingTaskbarRetries = 0;
                CreateWidgets();
            }
        }

        // Create a widget for every targeted display that doesn't have one yet
        private static void CreateWidgets()
        {
            var displays = GetTargetDisplays(out bool incomplete);

            foreach (var display in displays)
            {
                string displayPath = display.Display.DevicePath;

                if (taskBarWidgets.Any(w => w.DisplayPath == displayPath))
                {
                    continue;
                }

                // The first targeted display is the preferred one, so it hosts the main widget
                bool isMain = !taskBarWidgets.Any(w => w.IsMain);

                Log.Information($"Creating widget (main: {isMain}) for display: {display.Summary}");

                try
                {
                    var widget = new TaskBarWidget(display, isMain);

                    widget.Initialize();

                    widget.Destroying += Widget_Destroying;
                    widget.ManualPositionChanged += Widget_ManualPositionChanged;

                    widget.Show();

                    taskBarWidgets.Add(widget);
                }
                catch (Exception ex) when (!isMain)
                {
                    // A failure in a secondary display should not take down the widget of the main one
                    Log.Error(ex, $"Could not create the widget for display {display.Summary}: {ex.Message}");
                    incomplete = true;
                    continue;
                }

                if (isMain)
                {
                    isPurposelyHidden = false;
                    latestDisplay = displayPath;

                    UpdateTrayMenuItemsStates(true);
                }
            }

            if (incomplete)
            {
                ScheduleMissingTaskbarRetry();
            }
            else
            {
                missingTaskbarRetries = 0;
            }
        }

        private static List<DisplayEntity> GetTargetDisplays(out bool incomplete)
        {
            incomplete = false;
            var displays = new List<DisplayEntity>();

            if (!IsAllDisplaysMode)
            {
                displays.Add(DisplayHelper.FindProperDisplay(Properties.Settings.Default.Display));
                return displays;
            }

            Log.Information("Looking for the taskbars of all displays");
            var taskbars = new HashSet<IntPtr>();
            var primary = DisplayHelper.PrimaryDisplay;

            displays.Add(primary);
            taskbars.Add(DisplayHelper.GetTaskbarFromDisplay(primary).Item1);

            foreach (var display in DisplayHelper.AvailableDisplays)
            {
                // Displays that duplicate the primary one share its taskbar
                if (display.IsPrimary)
                {
                    continue;
                }

                var hwndTaskbar = DisplayHelper.GetTaskbarFromDisplay(display).Item1;

                if (hwndTaskbar == IntPtr.Zero)
                {
                    Log.Information($"No taskbar was found (yet) for display: {display.Summary}");
                    incomplete = true;
                }
                // Displays that duplicate each other share the same taskbar
                else if (taskbars.Add(hwndTaskbar))
                {
                    displays.Add(display);
                }
            }

            return displays;
        }

        private static void ScheduleMissingTaskbarRetry()
        {
            if (missingTaskbarRetries >= MissingTaskbarMaxRetries)
            {
                Log.Warning("Giving up on the displays that have no taskbar");
                return;
            }

            missingTaskbarRetries++;
            Log.Information($"Some displays have no widget yet. Retry #{missingTaskbarRetries} is scheduled");

            Task.Delay(MissingTaskbarRetryDelay).ContinueWith(_ => dispatcher.TryEnqueue(() =>
            {
                // Make sure nothing changed while waiting
                if (IsWidgetVisible && IsAllDisplaysMode)
                {
                    try
                    {
                        CreateWidgets();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"An error occured while creating the missing widgets: {ex.Message}");
                    }
                }
            }));
        }

        private static void HideWidgetExecute(bool showNotification = false)
        {
            Log.Information("Hiding widget");

            if (IsWidgetVisible)
            {
                foreach (var widget in taskBarWidgets.ToArray())
                {
                    using (widget)
                    {
                        Log.Information("Destroying widget");
                        widget.Destroy();
                    }
                }

                // Normally the widgets remove themselves from the list when they get destroyed
                taskBarWidgets.Clear();
                isPurposelyHidden = true;

                if (showNotification)
                {
                    Notification.NotificationManager.SendWidgetStillRunningToast();
                }
            }
        }

        private static void Widget_Destroying(object sender, EventArgs e)
        {
            var widget = sender as TaskBarWidget;
            widget.Destroying -= Widget_Destroying;
            widget.ManualPositionChanged -= Widget_ManualPositionChanged;
            taskBarWidgets.Remove(widget);

            if (!IsWidgetVisible)
            {
                UpdateTrayMenuItemsStates(false);
            }

            Log.Information("Widget destroyed");
        }

        private static void Widget_ManualPositionChanged(object sender, EventArgs e)
        {
            // The custom position is shared, so the other widgets have to follow
            foreach (var widget in taskBarWidgets.ToArray())
            {
                if (!ReferenceEquals(widget, sender))
                {
                    widget.UpdatePosition();
                }
            }
        }

        private static void OnTaskbarCreated()
        {
            try
            {
                Log.Information("Taskbar created");
                _ = trayIcon.TryRemove();
                trayIcon.Create();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Something went wrong while creating tray icon after taskbar creation: {ex.Message}");
#if DEBUG
                throw;
#endif
            }

            // The previous widgets may have been destroyed along with the previous taskbars
            RecreateWidgets();
        }

        private static void OnDisplayMenuItemClick(PopupMenuItem item, string targetDisplay)
        {
            if (item.Checked || !Properties.Settings.Default.IsConfigured)
                return;

            Log.Information("Clicked on a Display option from tray icon: " + targetDisplay);
            dispatcher.TryEnqueue(() => SwitchDisplay(targetDisplay));
        }

        private static void UpdateDisplayTrayMenus()
        {
            Log.Information("Updating tray's Display context-menu");
            bool enabled = SystemInfos.ShowTaskbarOnAllDisplays();
            int count = 0;
            displaySubMenu.Items.Clear();
            displaySubMenu.Items.Add(primaryItem);
            displaySubMenu.Items.Add(allDisplaysItem);
            primaryItem.Checked = PrimaryDisplayOption == Properties.Settings.Default.Display;
            allDisplaysItem.Checked = AllDisplaysOption == Properties.Settings.Default.Display;
            allDisplaysItem.Enabled = enabled;

            foreach (var display in DisplayHelper.AvailableDisplays)
            {
                var item = new PopupMenuItem(display.Summary, (s, _) => OnDisplayMenuItemClick((PopupMenuItem)s, display.Display.DevicePath))
                {
                    Checked = display.Display.DevicePath == Properties.Settings.Default.Display,
                    Enabled = enabled,
                };
                displaySubMenu.Items.Add(item);
                count++;
            }

            Log.Information($"Added {count} tray sub-item's");

            displaySubMenu.Visible = displaySeparator.Visible = count > 1;
        }

        private static void UpdateTrayMenuItemsStates(bool isWidgetVisible)
        {
            Log.Information($"Updating tray icon menu states. (widget visible: {isWidgetVisible})");
            showItem.Enabled = !isWidgetVisible;
            hideItem.Enabled = isWidgetVisible;
            repositionItem.Enabled = isWidgetVisible;
            manualPositionItem.Enabled = isWidgetVisible;
        }

        private static void UpdateTrayIconLocalization()
        {
            trayIcon.UpdateToolTip(LocaleManager.Default.Get("Data.AppName"));
            showItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Show");
            hideItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Hide");
            repositionItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Reposition");
            manualPositionItem.Text = LocaleManager.Default.Get("UI.ContextMenu.ManualPosition");
            displaySubMenu.Text = LocaleManager.Default.Get("UI.ContextMenu.Display");
            primaryItem.Text = LocaleManager.Default.Get("UI.ContextMenu.AlwaysPrimary");
            allDisplaysItem.Text = LocaleManager.Default.Get("UI.ContextMenu.AllDisplays");
            quitItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Quit");

            trayIcon.ContextMenu.RightToLeft = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft;
        }
    }
}
