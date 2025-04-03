using AwqatSalaat.Configurations;
using AwqatSalaat.Extensions;
using AwqatSalaat.Helpers;
using Serilog;
using System;
using System.Threading;

namespace AwqatSalaat.ViewModels
{
    public class PrayerTimeViewModel : ObservableObject
    {
        private DateTime time, apiTime;
        private bool isNext;
        private bool isActive;
        private bool isNotificationDismissed;
        private bool isVisible = true;
        private PrayerTimeState state;
        private Timer timer;
        private readonly PrayerConfig config;

        private bool IsInEnteredNotificationPeriod => !IsShuruq && IsEntered && DistanceElapsed > 0 && !isNotificationDismissed && Elapsed.TotalMinutes <= DistanceElapsed;
        private bool IsInNearNotificationPeriod => !IsShuruq && isNext && Distance > 0 && !IsEntered && !isNotificationDismissed && Countdown.TotalMinutes <= Distance;

        public string Name => LocaleManager.Default.Get($"Data.Salaat.{Key}");
        public string Key { get; }
        public bool IsShuruq { get; }
        public bool IsVisible { get => isVisible; private set => SetProperty(ref isVisible, value); }
        public DateTime Time { get => time; private set => SetProperty(ref time, value); }
        public bool IsNext { get => isNext; set { isNext = value; UpdateState(); } }
        public bool IsActive { get => isActive; set => Activate(value); }
        public ushort Distance { get; private set; }
        public byte DistanceElapsed { get; private set; }
        public TimeSpan Countdown => time - TimeStamp.Now;
        public TimeSpan Elapsed => TimeStamp.Now - time;
        public bool IsEntered => time < TimeStamp.Now;
        public PrayerTimeState State
        {
            get => state;
            set
            {
                var current = state;

                if (SetProperty(ref state, value))
                {
                    DismissNotification.RaiseCanExecuteChanged();
                    Log.Debug($"Updated state for: {Key}. From={current}, To={value}");
                }
            }
        }
        public RelayCommand DismissNotification { get; }

        public event EventHandler Entered;
        public event EventHandler EnteredNotificationDone;

        public PrayerTimeViewModel(string key)
        {
            IsShuruq = key == nameof(Data.PrayerTimes.Shuruq);
            Key = key;
            config = Properties.Settings.Default.GetPrayerConfig(key);
            InvalidateConfig();
            DismissNotification = new RelayCommand(DismissExecute, o => IsInNearNotificationPeriod || IsInEnteredNotificationPeriod);
            LocaleManager.Default.CurrentChanged += (_, __) => OnPropertyChanged(nameof(Name));
        }

        public void SetTime(DateTime apiTime)
        {
            this.apiTime = apiTime;
            Time = apiTime.AddMinutes(config.Adjustment);
            Log.Debug($"Updating time for: {Key}. API time={apiTime:u}, Final time={Time:u}");
            UpdateState();
        }

        public void InvalidateConfig()
        {
            if (apiTime != DateTime.MinValue)
            {
                Time = apiTime.AddMinutes(config.Adjustment);
            }

            Distance = config.EffectiveReminderOffset();
            DistanceElapsed = config.EffectiveElapsedTime();
            IsVisible = config.IsVisible;
        }

        private void Activate(bool active)
        {
            Log.Debug($"Updating time activation for: {Key}. From={isActive}, To={active}");
            isActive = active;

            if (active)
            {
                timer = new Timer(TimerTick, null, 0, 1000);
            }
            else
            {
                timer?.Dispose();
                timer = null;
                // Call one more time to notify about any changes
                TimerTick(null);
            }
        }

        private void DismissExecute(object obj)
        {
            Log.Debug($"Dismiss invoked for: {Key}");
            isNotificationDismissed = true;
            TimerTick(null);
        }

        private void TimerTick(object arg)
        {
            UpdateState();

            OnPropertyChanged(nameof(Countdown));
            OnPropertyChanged(nameof(Elapsed));
        }

        private void UpdateState()
        {
            if (IsEntered)
            {
                bool shouldRaiseEvents = false;

                // transition from previous state
                if (state == PrayerTimeState.ShuruqComing || state == PrayerTimeState.Next || state == PrayerTimeState.Near)
                {
                    isNotificationDismissed = false;
                    shouldRaiseEvents = true;
                }

                if (IsInEnteredNotificationPeriod)
                {
                    State = PrayerTimeState.EnteredRecently;

                    if (shouldRaiseEvents)
                    {
                        Log.Debug($"Raising Entered event for: {Key} ({state})");
                        Entered?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    var previousState = state;
                    shouldRaiseEvents |= state == PrayerTimeState.EnteredRecently;

                    State = PrayerTimeState.Entered;

                    if (shouldRaiseEvents)
                    {
                        if (previousState != PrayerTimeState.EnteredRecently)
                        {
                            Log.Debug($"Raising Entered event for: {Key} ({state})");
                            Entered?.Invoke(this, EventArgs.Empty);
                        }

                        Log.Debug($"Raising EnteredNotificationDone event for: {Key} ({state})");
                        EnteredNotificationDone?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            else if (IsNext)
            {
                // transition from previous state
                if (state == PrayerTimeState.Coming)
                {
                    isNotificationDismissed = false;
                }

                if (IsInNearNotificationPeriod)
                {
                    State = PrayerTimeState.Near;
                }
                else
                {
                    State = PrayerTimeState.Next;
                }
            }
            else
            {
                State = IsShuruq ? PrayerTimeState.ShuruqComing : PrayerTimeState.Coming;
            }
        }
    }

    public enum PrayerTimeState
    {
        Coming,
        ShuruqComing,
        Next,
        Near,
        Entered,
        EnteredRecently
    }
}
