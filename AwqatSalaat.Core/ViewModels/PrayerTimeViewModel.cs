using AwqatSalaat.Helpers;
using System;
using System.Threading;
using System.Windows.Input;

namespace AwqatSalaat.ViewModels
{
    public class PrayerTimeViewModel : ObservableObject
    {
        private DateTime time;
        private bool isNext, isNotificationDismissed;
        private PrayerTimeState state;
        private Timer timer;

        public string Name => LocaleManager.Default.Get($"Data.Salaat.{Key}");
        public string Key { get; }
        public DateTime Time { get => time; private set => SetProperty(ref time, value); }
        public bool IsNext { get => isNext; set { SetProperty(ref isNext, value); Activate(value); } }
        public ushort Distance { get; set; }
        public TimeSpan Countdown => time - TimeStamp.Now;
        public bool IsElapsed => time < TimeStamp.Now;
        public bool IsTimeClose => isNext && Distance > 0 && !isNotificationDismissed && !IsElapsed && Countdown.TotalMinutes <= Distance;
        public PrayerTimeState State { get => state; set => SetProperty(ref state, value); }
        public ICommand DismissNotification { get; }

        public event EventHandler Elapsed;

        public PrayerTimeViewModel(string key)
        {
            Key = key;
            DismissNotification = new RelayCommand(DismissExecute, o => IsTimeClose);
            LocaleManager.Default.CurrentChanged += (_, __) => OnPropertyChanged(nameof(Name));
        }

        public void SetTime(DateTime apiTime)
        {
            Time = apiTime;
            if (IsElapsed)
            {
                isNotificationDismissed = false;
            }
            OnPropertyChanged(nameof(IsElapsed));
            OnPropertyChanged(nameof(IsTimeClose));
            UpdateState();
        }

        private void Activate(bool active)
        {
            if (active)
            {
                timer = new Timer(TimerTick, null, 0, 1000);
            }
            else
            {
                timer.Dispose();
                timer = null;
                // Call one more time to notify about any changes, pass false to avoid recursive calls
                TimerTick(false);
            }
        }

        private void DismissExecute(object obj)
        {
            isNotificationDismissed = true;
            TimerTick(null);
        }

        private void TimerTick(object state)
        {
            if (Countdown.TotalSeconds < 0)
            {
                if (!(state is bool raiseEvent) || raiseEvent)
                {
                    Elapsed?.Invoke(this, EventArgs.Empty);
                }
                isNotificationDismissed = false;
            }
            OnPropertyChanged(nameof(Countdown));
            OnPropertyChanged(nameof(IsElapsed));
            OnPropertyChanged(nameof(IsTimeClose));
            UpdateState();
        }

        private void UpdateState()
        {
            if (IsElapsed)
            {
                State = PrayerTimeState.Elapsed;
            }
            else if (IsTimeClose)
            {
                State = PrayerTimeState.Near;
            }
            else if (IsNext)
            {
                State = PrayerTimeState.Next;
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
        Elapsed
    }
}
