using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.WinUI.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.WinUI
{
    internal static class TaskBarManager
    {
        private static readonly System.Drawing.Icon AppIcon;
        private static TrayIconWithContextMenu trayIcon;
        private static TaskBarWidget taskBarWidget;
        private static DispatcherQueue dispatcher;

        private static PopupMenuItem showItem;
        private static PopupMenuItem hideItem;
        private static PopupMenuItem repositionItem;
        private static PopupMenuItem manualPositionItem;
        private static PopupMenuItem quitItem;
        private static PopupMenuItem primaryItem;
        private static PopupSubMenu displaySubMenu;
        private static PopupMenuSeparator displaySeparator = new PopupMenuSeparator();

        private static string latestDisplay;
        private static bool isPurposelyHidden;
        private static bool isQuitting;

        public static IntPtr CurrentWidgetHandle => taskBarWidget?.Handle ?? throw new InvalidOperationException("The taskbar widget is missing.");
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
                taskBarWidget?.UpdatePosition(true);
            });
            ManuallyPositionWidget = new RelayCommand(static o =>
            {
                Log.Information("Clicked on Manual position");
                taskBarWidget?.StartDragging();
            });
            SetDisplay = new RelayCommand(static o =>
            {
                string option = o.ToString();
                Log.Information("Clicked on a Display option: " + option);
                SwitchDisplay(option);
            },
            static o => (o as string) == "PRIMARY" || SystemInfos.ShowTaskbarOnAllDisplays());

            App.Quitting += App_Quitting;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateTrayIconLocalization();
            DisplayHelper.DisplayChanged += DisplayHelper_DisplayChanged;
            TaskbarSettingsWatcher.SettingChanged += TaskbarSettingsWatcher_SettingChanged;
            Notification.NotificationManager.ShowWidgetRequested += NotificationManager_ShowWidgetRequested;
            Properties.Settings.Realtime.PropertyChanged += Settings_PropertyChanged;

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
            }
        }

        private static void DisplayHelper_DisplayChanged(object sender, DisplayChangedEventArgs e)
        {
            var affectedDisplay = e.DisplayEntity?.Display?.DevicePath;
            var displaySetting = Properties.Settings.Default.Display;

            if (e.Reason == DisplayChangedReason.PrimaryDisplay && displaySetting == "PRIMARY")
            {
                Log.Information("Moving the widget to the new primary display");
                dispatcher.TryEnqueue(HideThenShow);
            }
            else if (e.Reason is DisplayChangedReason.PrimaryDuplicated or DisplayChangedReason.PrimaryDeduplicated
                && displaySetting != "PRIMARY"
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
            dispatcher?.TryEnqueue(UpdateTrayIconVisibility);
        }

        private static void HideThenShow()
        {
            if (taskBarWidget is not null)
            {
                HideWidgetExecute();
                ShowWidgetExecute();
            }
        }

        private static void SwitchDisplay(string target)
        {
            try
            {
                Log.Information($"Switching display to: {target}");
                bool widgetVisible = taskBarWidget is not null;

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
            if (taskBarWidget is null)
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
                    taskBarWidget?.UpdatePosition(true);
                });
                manualPositionItem = new PopupMenuItem("Manual position", (_, _) =>
                {
                    Log.Information("Clicked on Manual position from tray icon");
                    dispatcher.TryEnqueue(() => taskBarWidget?.StartDragging());
                });
                displaySubMenu = new PopupSubMenu("Display")
                {
                    Items =
                    {
                        (primaryItem = new PopupMenuItem("Always Primary", (s, _) => OnDisplayMenuItemClick((PopupMenuItem)s, "PRIMARY")))
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

        public static void InvalidateWidgetElementsAlignment(bool autoAlign) => taskBarWidget?.InvalidateElementsAlignment(autoAlign);

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
            isQuitting = true;
            Properties.Settings.Realtime.PropertyChanged -= Settings_PropertyChanged;

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

            if (taskBarWidget is null)
            {
                Log.Information("Creating widget");

                var display = DisplayHelper.FindProperDisplay(Properties.Settings.Default.Display);
                if (display is null)
                {
                    UpdateTrayMenuItemsStates(false);
                    UpdateTrayIconVisibility();
                    return;
                }

                var widget = new TaskBarWidget(display);

                widget.Initialize();

                widget.Destroying += Widget_Destroying;

                widget.Show();

                taskBarWidget = widget;
                isPurposelyHidden = false;
                latestDisplay = display.Display.DevicePath;

                UpdateTrayMenuItemsStates(true);
                UpdateTrayIconVisibility();
            }
        }

        private static void HideWidgetExecute(bool showNotification = false)
        {
            Log.Information("Hiding widget");

            if (taskBarWidget is not null)
            {
                using (taskBarWidget)
                {
                    Log.Information("Destroying widget");
                    taskBarWidget.Destroy();
                    isPurposelyHidden = true;
                }

                if (showNotification)
                {
                    Notification.NotificationManager.SendWidgetStillRunningToast();
                }
            }
        }

        private static void Widget_Destroying(object sender, EventArgs e)
        {
            (sender as TaskBarWidget).Destroying -= Widget_Destroying;
            UpdateTrayMenuItemsStates(false);

            taskBarWidget = null;
            UpdateTrayIconVisibility();
            Log.Information("Widget destroyed");
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

            HideThenShow();
            UpdateTrayIconVisibility();
        }

        private static void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.ShowTrayIcon))
            {
                dispatcher?.TryEnqueue(UpdateTrayIconVisibility);
            }
        }

        private static void UpdateTrayIconVisibility()
        {
            if (isQuitting || trayIcon is null)
                return;

            // Keep the tray accessible when the widget has no usable window or display.
            bool widgetAvailable = taskBarWidget?.Display is not null
                && User32.GetWindowRect(taskBarWidget.Handle, out _);
            var visibility = Properties.Settings.Realtime.ShowTrayIcon || !widgetAvailable
                ? IconVisibility.Visible
                : IconVisibility.Hidden;

            try
            {
                // Preserve the message window used for Explorer and session events.
                trayIcon.UpdateVisibility(visibility);
            }
            catch (InvalidOperationException ex)
            {
                trayIcon.Visibility = visibility;
                Log.Warning(ex, "Could not update tray icon visibility");
            }
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
            primaryItem.Checked = "PRIMARY" == Properties.Settings.Default.Display;

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
            quitItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Quit");

            trayIcon.ContextMenu.RightToLeft = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft;
        }
    }
}
