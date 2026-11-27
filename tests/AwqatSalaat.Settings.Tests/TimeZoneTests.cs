using System;
using System.Globalization;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.AlAdhan;
using Newtonsoft.Json;
using TimeZoneConverter;
using Xunit;

namespace AwqatSalaat.Settings.Tests
{
    // Issue #56: prayer times in Kazakhstan were shown one hour early with Al-Adhan service.
    public class TimeZoneTests
    {
        private static readonly DateTime Day = new DateTime(2026, 11, 20);

        [Fact]
        public void OffsetTime_SameOffsetAsLocal_KeepsClockTime()
        {
            var astana = TimeZoneInfo.FindSystemTimeZoneById("Qyzylorda Standard Time"); // UTC+05:00 Astana
            var dhuhr = DateTimeOffset.Parse("2026-11-20T11:38:00+05:00", CultureInfo.InvariantCulture);

            var local = TimeZoneHelper.ConvertDateTimeOffsetToLocal(dhuhr, astana);

            Assert.Equal(Day.AddHours(11).AddMinutes(38), local);
            Assert.Equal(DateTimeKind.Unspecified, local.Kind);
        }

        [Fact]
        public void OffsetTime_DifferentLocalZone_IsConverted()
        {
            var bishkek = TimeZoneInfo.FindSystemTimeZoneById("Central Asia Standard Time"); // UTC+06:00
            var dhuhr = DateTimeOffset.Parse("2026-11-20T11:38:00+05:00", CultureInfo.InvariantCulture);

            var local = TimeZoneHelper.ConvertDateTimeOffsetToLocal(dhuhr, bishkek);

            Assert.Equal(Day.AddHours(12).AddMinutes(38), local);
        }

        // Salah Hour by zip code answers in the place's zone. A PC in Lagos (UTC+1, no daylight saving) showing Berlin
        // (UTC+1, UTC+2 in summer) got Berlin's summer times one hour late, because only the base offsets were compared.
        [Fact]
        public void ClockTime_SameBaseOffsetOtherDaylightSaving_IsConverted()
        {
            var lagos = TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
            var berlinSummerDhuhr = new DateTime(2026, 6, 21, 13, 10, 0);
            var berlinWinterDhuhr = new DateTime(2026, 12, 21, 12, 10, 0);

            Assert.Equal(new DateTime(2026, 6, 21, 12, 10, 0), TimeZoneHelper.ConvertDateTimeToLocal(berlinSummerDhuhr, "Europe/Berlin", lagos));
            Assert.Equal(berlinWinterDhuhr, TimeZoneHelper.ConvertDateTimeToLocal(berlinWinterDhuhr, "Europe/Berlin", lagos));
        }

        [Fact]
        public void ClockTime_SameZone_IsKept()
        {
            var berlin = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var dhuhr = new DateTime(2026, 6, 21, 13, 10, 0);

            Assert.Equal(dhuhr, TimeZoneHelper.ConvertDateTimeToLocal(dhuhr, "Europe/Berlin", berlin));
        }

        [Fact]
        public void AlmatyMapsToZoneWithCurrentKazakhstanOffset()
        {
            // Kazakhstan moved to UTC+05:00 in March 2024; TimeZoneConverter 6.1.0 still mapped
            // Asia/Almaty to "Central Asia Standard Time" (UTC+06:00).
            var windowsId = TZConvert.IanaToWindows("Asia/Almaty");
            var zone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);

            Assert.Equal(TimeSpan.FromHours(5), zone.GetUtcOffset(Day));
        }

        [Fact]
        public void MonthResponse_Iso8601Times_UseTheirOffset()
        {
            var response = JsonConvert.DeserializeObject<MonthResponse>(BuildMonthJson(
                "Asia/Almaty",
                "2026-11-20T05:12:00+05:00", "2026-11-20T06:51:00+05:00", "2026-11-20T11:38:00+05:00",
                "2026-11-20T14:05:00+05:00", "2026-11-20T16:24:00+05:00", "2026-11-20T17:58:00+05:00"));

            var expected = TimeZoneInfo.ConvertTime(
                DateTimeOffset.Parse("2026-11-20T11:38:00+05:00", CultureInfo.InvariantCulture),
                TimeZoneInfo.Local).DateTime;

            Assert.Equal(expected, response.Times[Day].Dhuhr);
        }

        [Fact]
        public void MonthResponse_PlainTimes_StillParsed()
        {
            // Old response format, in the machine's own time zone, is left unchanged.
            var response = JsonConvert.DeserializeObject<MonthResponse>(BuildMonthJson(
                TimeZoneHelper.GetIanaTimeZone(),
                "05:12 (+05)", "06:51 (+05)", "11:38 (+05)", "14:05 (+05)", "16:24 (+05)", "17:58 (+05)"));

            Assert.Equal(Day.AddHours(11).AddMinutes(38), response.Times[Day].Dhuhr);
        }

        private static string BuildMonthJson(string timeZone, string fajr, string sunrise, string dhuhr, string asr, string maghrib, string isha)
        {
            var data = new
            {
                code = 200,
                status = "OK",
                data = new[]
                {
                    new
                    {
                        timings = new { Fajr = fajr, Sunrise = sunrise, Dhuhr = dhuhr, Asr = asr, Maghrib = maghrib, Isha = isha },
                        date = new { gregorian = new { date = "20-11-2026", format = "DD-MM-YYYY" } },
                        meta = new { timezone = timeZone },
                    },
                },
            };

            return JsonConvert.SerializeObject(data);
        }
    }
}
