using AwqatSalaat.Helpers;
using System;
using System.Threading;

namespace AwqatSalaat.ViewModels
{
    public class PrayerTimeViewModel : ObservableObject
    {
        private DateTime time;
        private bool isNext;
        private bool isActive;
        private bool isNotificationDismissed;
        private PrayerTimeState state;
        private Timer timer;

        private bool IsInEnteredNotificationPeriod => IsEntered && DistanceElapsed > 0 && !isNotificationDismissed && Elapsed.TotalMinutes <= DistanceElapsed;
        private bool IsInNearNotificationPeriod => isNext && Distance > 0 && !IsEntered && !isNotificationDismissed && Countdown.TotalMinutes <= Distance;

        public string Name => LocaleManager.Default.Get($"Data.Salaat.{Key}");
        public string Key { get; }
        public DateTime Time { get => time; private set => SetProperty(ref time, value); }
        public bool IsNext { get => isNext; set { isNext = value; UpdateState(); } }
        public bool IsActive { get => isActive; set => Activate(value); }
        public ushort Distance { get; set; }
        public byte DistanceElapsed { get; set; }
        public TimeSpan Countdown => time - TimeStamp.Now;
        public TimeSpan Elapsed => TimeStamp.Now - time;
        public bool IsEntered => time < TimeStamp.Now;
        public PrayerTimeState State
        {
            get => state;
            set
            {
                if (SetProperty(ref state, value))
                {
                    DismissNotification.RaiseCanExecuteChanged();
                }
            }
        }
        public RelayCommand DismissNotification { get; }

        public event EventHandler Entered;
        public event EventHandler EnteredNotificationDone;

        public PrayerTimeViewModel(string key)
        {
            Key = key;
            DismissNotification = new RelayCommand(DismissExecute, o => IsInNearNotificationPeriod || IsInEnteredNotificationPeriod);
            LocaleManager.Default.CurrentChanged += (_, __) => OnPropertyChanged(nameof(Name));
        }

        public void SetTime(DateTime apiTime)
        {
            Time = apiTime;
            UpdateState();
        }

        private void Activate(bool active)
        {
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
                if (state == PrayerTimeState.Next || state == PrayerTimeState.Near)
                {
                    isNotificationDismissed = false;
                    shouldRaiseEvents = true;
                }

                if (IsInEnteredNotificationPeriod)
                {
                    State = PrayerTimeState.EnteredRecently;

                    if (shouldRaiseEvents)
                    {
                        Entered?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    shouldRaiseEvents |= state == PrayerTimeState.EnteredRecently;

                    State = PrayerTimeState.Entered;

                    if (shouldRaiseEvents)
                    {
                        Entered?.Invoke(this, EventArgs.Empty);
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
                State = PrayerTimeState.Coming;
            }
        }
    }

    public enum PrayerTimeState
    {
        Coming,
        Next,
        Near,
        Entered,
        EnteredRecently
    }
}
