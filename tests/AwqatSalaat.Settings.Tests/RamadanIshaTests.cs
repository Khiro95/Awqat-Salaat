using System;
using System.Linq;
using AwqatSalaat.Services.Local;
using Xunit;
using CalculationMethod = AwqatSalaat.Services.Methods.CalculationMethod;

namespace AwqatSalaat.Settings.Tests
{
    // Umm Al-Qura: Isha is 90 minutes after Maghrib, 120 in Ramadan (Al-Adhan does the same).
    // Ramadan 1447 is 18 February - 19 March 2026 in the Umm Al-Qura calendar.
    public class RamadanIshaTests
    {
        [Theory]
        [InlineData("MAKKAH", "2026-03-05", 120)] // Ramadan
        [InlineData("MAKKAH", "2026-03-19", 120)] // last day of Ramadan
        [InlineData("MAKKAH", "2026-03-20", 90)]  // Eid
        [InlineData("MAKKAH", "2026-02-17", 90)]  // day before Ramadan
        public void Makkah_IshaAfterMaghrib(string method, string date, int minutes)
        {
            var day = DateTime.Parse(date, System.Globalization.CultureInfo.InvariantCulture);
            var times = GetTimes(method, day);

            Assert.Equal(minutes, (times.Isha - times.Maghrib).TotalMinutes, 0.5);
        }

        [Fact]
        public void OtherMethods_UnchangedInRamadan()
        {
            var ramadan = GetTimes("MWL", new DateTime(2026, 3, 5));
            var before = GetTimes("MWL", new DateTime(2026, 2, 15));

            // MWL uses an angle; the gap only drifts by seconds per day, not by 30 minutes
            Assert.InRange((ramadan.Isha - ramadan.Maghrib) - (before.Isha - before.Maghrib), TimeSpan.FromMinutes(-5), TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Makkah_WholeMonth_RamadanDaysOnly()
        {
            var request = BuildRequest("MAKKAH", new DateTime(2026, 3, 1));
            request.GetEntireMonth = true;
            var data = new LocalClient().GetDataAsync(request).GetAwaiter().GetResult();

            var with120 = data.Times.Where(d => (d.Value.Isha - d.Value.Maghrib).TotalMinutes > 105).Select(d => d.Key.Day).ToList();

            Assert.Equal(Enumerable.Range(1, 19), with120);
        }

        private static Data.PrayerTimes GetTimes(string method, DateTime day)
        {
            var data = new LocalClient().GetDataAsync(BuildRequest(method, day)).GetAwaiter().GetResult();

            return data.Times[day];
        }

        private static LocalRequest BuildRequest(string method, DateTime day) => new LocalRequest
        {
            Date = day,
            Method = CalculationMethod.AvailableMethods.First(m => m.Id == method),
            Latitude = 21.4225m,
            Longitude = 39.8262m,
            UseCoordinates = true,
        };
    }
}
