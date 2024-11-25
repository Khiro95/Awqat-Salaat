using AwqatSalaat.Helpers;
using System.Runtime.CompilerServices;

namespace AwqatSalaat.Configurations
{
    public class PrayerConfig : ObservableObject
    {
        private bool isVisible = true;
        private bool globalReminderOffset = true;
        private bool globalElapsedTime = true;
        private short adjustment;
        private ushort reminderOffset;
        private byte elapsedTime;

        public bool IsVisible { get => IsPrincipal || isVisible; set => SetProperty(ref isVisible, value); }
        public bool GlobalReminderOffset { get => globalReminderOffset; set => SetProperty(ref globalReminderOffset, value); }
        public bool GlobalElapsedTime { get => globalElapsedTime; set => SetProperty(ref globalElapsedTime, value); }
        public short Adjustment { get => adjustment; set => SetProperty(ref adjustment, value); }
        public ushort ReminderOffset { get => reminderOffset; set => SetProperty(ref reminderOffset, value); }
        public byte ElapsedTime { get => elapsedTime; set => SetProperty(ref elapsedTime, value); }
        public string Key { get; }
        public bool IsPrincipal { get; }

        public PrayerConfig(string key)
        {
            Key = key;
            IsPrincipal = key != nameof(Data.PrayerTimes.Shuruq);
        }

        // This will be called from the main settings object (Properties.Settings)
        internal void OnSettingsPropertyChanged(string property, object newValue)
        {
            var parts = property.Split('_');

            if (parts.Length != 3 || parts[1] != Key)
            {
                return;
            }

            switch (parts[2])
            {
                case nameof(IsVisible):
                    IsVisible = (bool)newValue;
                    break;
                case nameof(ReminderOffset):
                    ReminderOffset = (ushort)newValue;
                    break;
                case nameof(ElapsedTime):
                    ElapsedTime = (byte)newValue;
                    break;
                case nameof(Adjustment):
                    Adjustment = (sbyte)newValue;
                    break;
                case nameof(GlobalReminderOffset):
                    GlobalReminderOffset = (bool)newValue;
                    break;
                case nameof(GlobalElapsedTime):
                    GlobalElapsedTime = (bool)newValue;
                    break;
            }
        }
    }
}
