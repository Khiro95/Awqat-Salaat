using System;
using System.Linq;
using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using Xunit;

namespace AwqatSalaat.Settings.Tests
{
    // Issue #55: after Isha the widget should show the next day's Fajr, also around midnight and after sleep.
    // Uses the offline (Local) service, so no network, and a test clock instead of DateTime.Now.
    public sealed class DayTransitionTests : IDisposable
    {
        private static readonly DateTime Day = new DateTime(2026, 1, 15);

        private readonly Properties.Settings settings = Properties.Settings.Default;
        private DateTime now;

        public DayTransitionTests()
        {
            settings.Reset();
            settings.IsConfigured = true;
            settings.EnableLogs = false;
            settings.Service = PrayerTimesService.Local;
            settings.MethodString = "MWL";
            settings.LocationDetection = LocationDetectionMode.ByCoordinates;
            // A place whose solar time matches this machine's time zone, so the five prayers fall inside one local day
            settings.Latitude = 30m;
            settings.Longitude = (decimal)(TimeZoneInfo.Local.GetUtcOffset(Day).TotalHours * 15);
            settings.NotificationDistance = 1;          // the reporter's reminder offset
            settings.NotificationDistanceElapsed = 5;   // the reporter's elapsed time period

            now = Day.AddHours(12);
            TimeStamp.SetClockForTests(() => now);
        }

        public void Dispose()
        {
            TimeStamp.SetClockForTests(null);
            settings.Reset();
        }

        [Fact]
        public void AfterIshaAndElapsedTime_ShowsNextDayFajr()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddSeconds(1));
            Assert.Equal(Day, vm.DisplayedDate);
            Assert.Equal(nameof(PrayerTimes.Isha), vm.DisplayedTime?.Key);

            WalkTo(isha.AddMinutes(5).AddSeconds(1));
            AssertShowsFajrOf(vm, Day.AddDays(1));

            WalkTo(Day.AddDays(1).AddSeconds(1)); // midnight
            AssertShowsFajrOf(vm, Day.AddDays(1));

            vm.Refresh.Execute(null);
            AssertShowsFajrOf(vm, Day.AddDays(1));
        }

        [Fact]
        public void RefreshAfterIsha_ShowsNextDayFajr()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddMinutes(20));
            vm.Refresh.Execute(null);

            AssertShowsFajrOf(vm, Day.AddDays(1));
        }

        [Fact]
        public void SleepDuringElapsedTime_WakeNextMorning_ShowsNextDay()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddSeconds(1));
            MoveTo(Day.AddDays(1).AddHours(10)); // wake up after Fajr and Shuruq

            Assert.Equal(Day.AddDays(1), vm.DisplayedDate);
            Assert.Equal(Day.AddDays(1), vm.Next.Time.Date);
            Assert.NotEqual(nameof(PrayerTimes.Fajr), vm.Next.Key);
        }

        [Fact]
        public void SleepBeforeIsha_WakeAfterMidnight_ShowsNextDayFajr()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddMinutes(-30));
            MoveTo(Day.AddDays(1).AddHours(1)); // wake up after midnight, before Fajr

            AssertShowsFajrOf(vm, Day.AddDays(1));
        }

        [Fact]
        public void SleepAfterIsha_WakeTwoDaysLater_ShowsThatDay()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddMinutes(6));
            AssertShowsFajrOf(vm, Day.AddDays(1));

            MoveTo(Day.AddDays(2).AddHours(1)); // long sleep, wake up before Fajr two days later

            AssertShowsFajrOf(vm, Day.AddDays(2));
        }

        [Fact]
        public void FullDayAfterTransition_SwitchesOnlyOncePerIsha()
        {
            var vm = new WidgetViewModel();
            var isha = Isha(vm);

            WalkTo(isha.AddMinutes(6));
            AssertShowsFajrOf(vm, Day.AddDays(1));

            // Walk through the next day minute by minute until its Isha has passed
            var nextIsha = Isha(vm);
            for (var t = Day.AddDays(1); t <= nextIsha.AddMinutes(4); t = t.AddMinutes(1))
            {
                WalkTo(t);
                Assert.Equal(Day.AddDays(1), vm.DisplayedDate);
            }

            WalkTo(nextIsha.AddMinutes(6));
            AssertShowsFajrOf(vm, Day.AddDays(2));
        }

        [Fact]
        public void SleepDuringDay_WakeNextEveningAfterIsha_ShowsFajrOfDayAfter()
        {
            var vm = new WidgetViewModel(); // 12:00
            var isha = Isha(vm);

            MoveTo(isha.AddDays(1).AddMinutes(10)); // wake up the next evening, after Isha and its elapsed time

            AssertShowsFajrOf(vm, Day.AddDays(2));
        }

        // The "NAME TIME After COUNTDOWN" label in #55 is the fallback text shown when DisplayedTime is null
        [Fact]
        public void SleepAndWakeAtAnyTime_AlwaysShowsAPrayer()
        {
            var failures = new System.Collections.Generic.List<string>();
            var keys = new[] { "Fajr", "Shuruq", "Dhuhr", "Asr", "Maghrib", "Isha" };
            var sleepOffsets = new[] { -10, 10 };
            var wakeOffsets = new[] { -10, 3, 10 }; // before, inside and after the 5 min elapsed time

            now = Day.AddMinutes(30);
            var reference = new WidgetViewModel();
            var times = reference.Items.ToDictionary(i => i.Key, i => i.Time);

            foreach (var sleepKey in keys)
            {
                foreach (var sleepOffset in sleepOffsets)
                {
                    foreach (var wakeDay in new[] { 0, 1 })
                    {
                        foreach (var wakeKey in keys)
                        {
                            foreach (var wakeOffset in wakeOffsets)
                            {
                                var sleep = times[sleepKey].AddMinutes(sleepOffset);
                                var wake = times[wakeKey].AddDays(wakeDay).AddMinutes(wakeOffset);

                                if (wake <= sleep.AddMinutes(1))
                                {
                                    continue;
                                }

                                now = sleep.AddMinutes(-2);
                                var vm = new WidgetViewModel();
                                WalkTo(sleep);
                                MoveTo(wake);
                                MoveTo(wake.AddSeconds(1));

                                if (vm.DisplayedTime is null || vm.ErrorMessage != null)
                                {
                                    failures.Add($"sleep {sleepKey}{sleepOffset:+0;-0} ({sleep:dd HH:mm}) -> wake {wakeKey}{wakeOffset:+0;-0} ({wake:dd HH:mm}): DisplayedTime={vm.DisplayedTime?.Key ?? "null"}, Date={vm.DisplayedDate:dd}, Error={vm.ErrorMessage}");
                                }
                            }
                        }
                    }
                }
            }

            Assert.True(failures.Count == 0, $"{failures.Count} cases left no prayer shown:\n" + string.Join("\n", failures.Take(30)));
        }

        private static DateTime Isha(WidgetViewModel vm) => vm.Items.Single(i => i.Key == nameof(PrayerTimes.Isha)).Time;

        // A jump, like waking up from sleep
        private void MoveTo(DateTime time)
        {
            now = time;
            TimeStamp.TickForTests();
        }

        // Normal running: the real timer ticks every second, 20 s steps are enough here
        private void WalkTo(DateTime time)
        {
            while (now.AddSeconds(20) < time)
            {
                MoveTo(now.AddSeconds(20));
            }

            MoveTo(time);
        }

        private static void AssertShowsFajrOf(WidgetViewModel vm, DateTime date)
        {
            Assert.Null(vm.ErrorMessage);
            Assert.Equal(date, vm.DisplayedDate);
            Assert.Equal(nameof(PrayerTimes.Fajr), vm.Next?.Key);
            Assert.Equal(date, vm.Next.Time.Date);
            Assert.Equal(nameof(PrayerTimes.Fajr), vm.DisplayedTime?.Key);
        }
    }
}
