using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Windows.Input;

namespace AwqatSalaat.WinUI.Controls
{
    class PrayerTimeItem : Control
    {
        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            "State",
            typeof(PrayerTimeState),
            typeof(PrayerTimeItem),
            new PropertyMetadata(PrayerTimeState.Coming, OnStateChanged));

        public static readonly DependencyProperty PrayerNameProperty = DependencyProperty.Register(
            "PrayerName",
            typeof(string),
            typeof(PrayerTimeItem),
            new PropertyMetadata(null));

        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            "Time",
            typeof(DateTime),
            typeof(PrayerTimeItem),
            new PropertyMetadata(DateTime.MinValue));

        public static readonly DependencyProperty TimeStringProperty = DependencyProperty.Register(
            "TimeString",
            typeof(string),
            typeof(PrayerTimeItem),
            new PropertyMetadata(""));

        public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
            "DismissCommand",
            typeof(ICommand),
            typeof(PrayerTimeItem),
            new PropertyMetadata(null));

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PrayerTimeItem prayerTimeItem = (PrayerTimeItem)d;

            VisualStateManager.GoToState(prayerTimeItem, e.NewValue.ToString(), false);
        }

        public PrayerTimeState State { get => (PrayerTimeState)GetValue(StateProperty); set => SetValue(StateProperty, value); }
        public string PrayerName { get => (string)GetValue(PrayerNameProperty); set => SetValue(PrayerNameProperty, value); }
        public DateTime Time { get => (DateTime)GetValue(TimeProperty); set => SetValue(TimeProperty, value); }
        public string TimeString { get => (string)GetValue(TimeStringProperty); set => SetValue(TimeStringProperty, value); }
        public ICommand DismissCommand { get => (ICommand)GetValue(DismissCommandProperty); set => SetValue(DismissCommandProperty, value); }

        public PrayerTimeItem()
        {
            DefaultStyleKey = typeof(PrayerTimeItem);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            VisualStateManager.GoToState(this, State.ToString(), false);
        }
    }
}
