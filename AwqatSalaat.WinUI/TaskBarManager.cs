using AwqatSalaat.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
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

        public static IntPtr CurrentWidgetHandle => taskBarWidget?.Handle ?? throw new InvalidOperationException("The taskbar widget is missing.");
        public static ICommand ShowWidget { get; }
        public static ICommand HideWidget { get; }
        public static ICommand RepositionWidget { get; }
        public static ICommand ManuallyPositionWidget { get; }

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
                HideWidgetExecute();
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

            App.Quitting += App_Quitting;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateTrayIconLocalization();

            AppIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
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
                    dispatcher.TryEnqueue(HideWidgetExecute);
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
                            new PopupMenuSeparator(),
                            quitItem,
                        }
                    },
                    Icon = AppIcon.Handle,
                };

                UpdateTrayIconLocalization();
                trayIcon.MessageWindow.TaskbarCreated += (_, _) => dispatcher.TryEnqueue(OnTaskbarCreated);
                trayIcon.Create();
            }

            ShowWidgetExecute();
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

            if (taskBarWidget is null)
            {
                Log.Information("Creating widget");
                var widget = new TaskBarWidget();

                widget.Initialize();

                widget.Destroying += Widget_Destroying;

                widget.Show();

                taskBarWidget = widget;

                UpdateTrayMenuItemsStates(true);
            }
        }

        private static void HideWidgetExecute()
        {
            Log.Information("Hiding widget");

            if (taskBarWidget is not null)
            {
                using (taskBarWidget)
                {
                    Log.Information("Destroying widget");
                    taskBarWidget.Destroy();
                }

                taskBarWidget = null;
                Log.Information("Widget destroyed");
            }
        }

        private static void Widget_Destroying(object sender, EventArgs e)
        {
            (sender as TaskBarWidget).Destroying -= Widget_Destroying;
            UpdateTrayMenuItemsStates(false);
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

            HideWidgetExecute();
            ShowWidgetExecute();
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
            quitItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Quit");

            trayIcon.ContextMenu.RightToLeft = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft;
        }
    }
}
