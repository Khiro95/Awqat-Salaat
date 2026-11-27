using System;
using System.Linq;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.SalahHour;
using Xunit;
using CalculationMethod = AwqatSalaat.Services.Methods.CalculationMethod;

namespace AwqatSalaat.Settings.Tests
{
    public class SalahHourTests
    {
        private static readonly CalculationMethod Mwl = CalculationMethod.AvailableMethods.First(m => m.Id == "MWL");

        private static SalahHourRequest BuildRequest(bool entireMonth) => new SalahHourRequest
        {
            Date = new DateTime(2026, 6, 21),
            GetEntireMonth = entireMonth,
            Method = Mwl,
            Latitude = 30.04m,
            Longitude = 31.24m,
            TimeZone = "Africa/Cairo",
            UseCoordinates = true,
        };

        // Salah Hour returns the whole month when show_entire_month is present at all, even with False
        [Fact]
        public void SingleDay_DoesNotSendShowEntireMonth()
        {
            Assert.DoesNotContain("show_entire_month", BuildRequest(false).GetUrl());
        }

        [Fact]
        public void EntireMonth_SendsShowEntireMonth()
        {
            Assert.Contains("show_entire_month=True", BuildRequest(true).GetUrl());
        }

        [Fact]
        public void SingleDay_IsParsedOnTheRequestedDate()
        {
            // Today's clock is in another month and season than the requested day
            TimeStamp.SetClockForTests(() => new DateTime(2026, 12, 1, 10, 0, 0));

            try
            {
                var requested = new DateTime(2026, 6, 21);
                string localZone = TimeZoneHelper.GetIanaTimeZone();
                string json = "{\"results\":{\"Fajr\":\"04:18\",\"Duha\":\"05:54\",\"Dhuhr\":\"12:57\",\"Asr\":\"16:32\",\"Maghrib\":\"19:59\",\"Isha\":\"21:36\"},"
                    + "\"settings\":{\"timezone\":\"" + localZone + "\"},\"success\":true}";

                var response = SalahHourClient.Deserialize<SingleDayResponse>(json, requested);

                Assert.Equal(requested.AddHours(4).AddMinutes(18), response.Times.Fajr);
                Assert.Equal(requested.AddHours(5).AddMinutes(54), response.Times.Shuruq);
                Assert.Equal(requested.AddHours(21).AddMinutes(36), response.Times.Isha);
            }
            finally
            {
                TimeStamp.SetClockForTests(null);
            }
        }
    }
}
