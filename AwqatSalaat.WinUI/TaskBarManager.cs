using AwqatSalaat.Helpers;
using H.NotifyIcon.Core;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Windows.Input;

namespace AwqatSalaat.WinUI
{
    internal static class TaskBarManager
    {
        private const string IconResourceName = "AwqatSalaat.WinUI.Assets.as_ico_win11.ico";

        private static TrayIconWithContextMenu trayIcon;
        private static TaskBarWidget taskBarWidget;
        private static DispatcherQueue dispatcher;

        private static PopupMenuItem showItem;
        private static PopupMenuItem hideItem;
        private static PopupMenuItem repositionItem;
        private static PopupMenuItem quitItem;

        public static ICommand ShowWidget { get; }
        public static ICommand HideWidget { get; }
        public static ICommand RepositionWidget { get; }

        static TaskBarManager()
        {
            ShowWidget = new RelayCommand(static o => ShowWidgetExecute());
            HideWidget = new RelayCommand(static o => HideWidgetExecute());
            RepositionWidget = new RelayCommand(static o => taskBarWidget?.UpdatePosition(true));

            App.Quitting += App_Quitting;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateTrayIconStrings();
        }

        public static void Initialize(DispatcherQueue dispatcherQueue)
        {
            dispatcher = dispatcherQueue;

            showItem = new PopupMenuItem("Show", (_, _) => dispatcher.TryEnqueue(ShowWidgetExecute));
            hideItem = new PopupMenuItem("Hide", (_, _) => dispatcher.TryEnqueue(HideWidgetExecute));
            repositionItem = new PopupMenuItem("Re-position", (_, _) => dispatcher.TryEnqueue(() => taskBarWidget?.UpdatePosition(true)));
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
                }
            };

            using (Stream stream = typeof(TaskBarManager).Assembly.GetManifestResourceStream(IconResourceName))
            {
                var ico = new System.Drawing.Icon(stream);
                trayIcon.Icon = ico.Handle;
            }

            UpdateTrayIconStrings();
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

        private static void UpdateTrayIconStrings()
        {
            trayIcon.UpdateToolTip(LocaleManager.Default.Get("Data.AppName"));
            showItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Show");
            hideItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Hide");
            repositionItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Reposition");
            quitItem.Text = LocaleManager.Default.Get("UI.ContextMenu.Quit");
        }
    }
}
