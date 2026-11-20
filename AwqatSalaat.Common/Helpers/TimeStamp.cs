using Serilog;
using System;
using System.Threading;

namespace AwqatSalaat.Helpers
{
    public static class TimeStamp
    {
        private static readonly Timer s_timer = new Timer(InternalTimerTick, null, 0, 1000);

        private static DateTime s_currentDate = DateTime.MinValue;
        private static Func<DateTime> s_testClock;

        public static DateTime Now
        {
            get
            {
                if (s_testClock != null)
                {
                    return s_testClock();
                }
#if DEBUG
                return new DateTime(2023, 9, 13, 16, 00, 00);
#else
                return DateTime.Now;
#endif
            }
        }

        public static DateTime Date => Now.Date;
        public static DateTime NextDate => Date.AddDays(1);

        public static event Action DateChanged;
        public static event Action TimerTick;

        /// <summary>
        /// Tests only: replaces the clock and stops the timer, so the test drives ticks with <see cref="TickForTests"/>.
        /// Pass null to restore the real clock and timer.
        /// </summary>
        internal static void SetClockForTests(Func<DateTime> clock)
        {
            s_testClock = clock;
            s_currentDate = clock is null ? DateTime.MinValue : clock().Date;
            s_timer.Change(clock is null ? 0 : Timeout.Infinite, clock is null ? 1000 : Timeout.Infinite);
        }

        internal static void TickForTests() => InternalTimerTick(null);

        private static void InternalTimerTick(object state)
        {
            TimerTick?.Invoke();

            var today = Now.Date;

            if (today != s_currentDate)
            {
                bool skipEvent = s_currentDate == DateTime.MinValue;
                s_currentDate = today;

                if (!skipEvent)
                {
                    Log.Information($"Date changed to {today:dd/MM/yyyy}");
                    DateChanged?.Invoke();
                }
            }
        }
    }
}