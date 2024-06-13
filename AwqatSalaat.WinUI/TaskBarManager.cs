using AwqatSalaat.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
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
        private static PopupMenuItem quitItem;

        public static IntPtr CurrentWidgetHandle => taskBarWidget?.Handle ?? throw new InvalidOperationException("The taskbar widget is missing.");
        public static ICommand ShowWidget { get; }
        public static ICommand HideWidget { get; }
        public static ICommand RepositionWidget { get; }

        static TaskBarManager()
        {
            ShowWidget = new RelayCommand(static o => ShowWidgetExecute());
            HideWidget = new RelayCommand(static o => HideWidgetExecute());
            RepositionWidget = new RelayCommand(static o => taskBarWidget?.UpdatePosition());

            App.Quitting += App_Quitting;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateTrayIconLocalization();

            AppIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }

        public static void Initialize(DispatcherQueue dispatcherQueue)
        {
            dispatcher = dispatcherQueue;

            showItem = new PopupMenuItem("Show", (_, _) => dispatcher.TryEnqueue(ShowWidgetExecute));
            hideItem = new PopupMenuItem("Hide", (_, _) => dispatcher.TryEnqueue(HideWidgetExecute));
            repositionItem = new PopupMenuItem("Re-position", (_, _) => taskBarWidget?.UpdatePosition());
            quitItem = new PopupMenuItem("Quit", (_, _) => dispatcher.TryEnqueue(() => App.Quit.Execute(null)));

            trayIcon = new TrayIconWithContextMenu()
            {
                ContextMenu = new PopupMenu
                {
                    Items =
                    {
                        showItem,
                        hideItem,
                        repositionItem,
                        new PopupMenuSeparator(),
                        quitItem,
                    }
                },
                Icon = AppIcon.Handle,
            };

            UpdateTrayIconLocalization();
            trayIcon.MessageWindow.TaskbarCreated += (_, _) => dispatcher.TryEnqueue(OnTaskbarCreated);
            trayIcon.Create();

            ShowWidgetExecute();
        }

        private static void App_Quitting()
        {
            using (trayIcon)
            {
                trayIcon.TryRemove();
            }

            HideWidgetExecute();
        }

        private static void ShowWidgetExecute()
        {
            if (taskBarWidget is null)
            {
                var widget = new TaskBarWidget();

                widget.Initialize();

                widget.Show();

                taskBarWidget = widget;
            }
        }

        private static void HideWidgetExecute()
        {
            if (taskBarWidget is not null)
            {
                using (taskBarWidget)
                {
                    taskBarWidget.Destroy();
                }

                taskBarWidget = null;
            }
        }

        private static void OnTaskbarCreated()
        {
            try
            {
                _ = trayIcon.TryRemove();
                trayIcon.Create();
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }

            HideWidgetExecute();
            ShowWidgetExecute();
        }

        private static void UpdateTrayIconLocalization()
        {
            trayIcon.UpdateToolTip(LocaleManager.Default.Get("Data.AppName"));
            showItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Show");
            hideItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Hide");
            repositionItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Reposition");
            quitItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Quit");

            trayIcon.ContextMenu.RightToLeft = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft;
        }
    }
}
