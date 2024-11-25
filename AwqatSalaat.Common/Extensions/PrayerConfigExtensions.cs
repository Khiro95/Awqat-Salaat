using AwqatSalaat.Configurations;

namespace AwqatSalaat.Extensions
{
    internal static class PrayerConfigExtensions
    {
        public static ushort EffectiveReminderOffset(this PrayerConfig config)
        {
            return config.GlobalReminderOffset
                ? Properties.Settings.Default.NotificationDistance
                : config.ReminderOffset;
        }

        public static byte EffectiveElapsedTime(this PrayerConfig config)
        {
            return config.GlobalElapsedTime
                ? Properties.Settings.Default.NotificationDistanceElapsed
                : config.ElapsedTime;
        }
    }
}
