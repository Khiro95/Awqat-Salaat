using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AwqatSalaat.Configurations;
using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.CSV;
using Newtonsoft.Json;
using Xunit;

namespace AwqatSalaat.Settings.Tests
{
    // Bugs found by reviewing the code (see the PR description)
    public sealed class CodeReviewFixTests : IDisposable
    {
        private readonly List<string> files = new List<string>();

        public void Dispose()
        {
            foreach (var f in files) File.Delete(f);
        }

        [Fact]
        public void AlAdhan_IshaAfterMidnight_BelongsToTheNextDay()
        {
            // Stockholm, 21 June 2026, MWL with "middle of the night": AlAdhan gives Isha 00:50 on the same date.
            // The response time zone is the machine's own, so no conversion happens.
            var json = JsonConvert.SerializeObject(new
            {
                code = 200,
                status = "OK",
                data = new[]
                {
                    new
                    {
                        timings = new { Fajr = "00:50 (CEST)", Sunrise = "03:31 (CEST)", Dhuhr = "13:00 (CEST)", Asr = "17:23 (CEST)", Maghrib = "22:08 (CEST)", Isha = "00:50 (CEST)" },
                        date = new { gregorian = new { date = "21-06-2026", format = "DD-MM-YYYY" } },
                        meta = new { timezone = TimeZoneHelper.GetIanaTimeZone() },
                    },
                },
            });

            var day = JsonConvert.DeserializeObject<MonthResponse>(json).Times[new DateTime(2026, 6, 21)];

            Assert.Equal(new DateTime(2026, 6, 21, 0, 50, 0), day.Fajr);
            Assert.Equal(new DateTime(2026, 6, 22, 0, 50, 0), day.Isha);
            Assert.True(day.Isha > day.Maghrib);
        }

        [Fact]
        public void Csv_DayAndMonthColumns_UseTheRequestedYear()
        {
            // On 31 December after Isha the widget asks for 1 January of the next year
            var path = WriteCsv("31,12,05:30,07:00,12:00,14:30,17:00,18:30", "1,1,05:31,07:01,12:01,14:31,17:01,18:31");
            var request = Request(path, new DateTime(2027, 1, 1), hasDateColumn: true);
            request.ColumnsMap["Day"] = 0;
            request.ColumnsMap["Month"] = 1;

            var data = new CsvClient().GetDataAsync(request).GetAwaiter().GetResult();

            Assert.True(data.Times.ContainsKey(new DateTime(2027, 1, 1)), "no data for 1 January 2027");
        }

        [Fact]
        public void Csv_YearFileWithoutDates_UsesTheRequestedYear()
        {
            var rows = Enumerable.Range(0, 365).Select(_ => ",,05:30,07:00,12:00,14:30,17:00,18:30").ToArray();
            var path = WriteCsv(rows);
            var request = Request(path, new DateTime(2027, 1, 1), hasDateColumn: false);

            var data = new CsvClient().GetDataAsync(request).GetAwaiter().GetResult();

            Assert.True(data.Times.ContainsKey(new DateTime(2027, 1, 1)), "no data for 1 January 2027");
        }

        [Fact]
        public void Csv_IshaAfterMidnight_BelongsToTheNextDay()
        {
            var path = WriteCsv("21,6,00:50,03:31,13:00,17:23,22:08,00:50");
            var request = Request(path, new DateTime(2026, 6, 21), hasDateColumn: true);
            request.ColumnsMap["Day"] = 0;
            request.ColumnsMap["Month"] = 1;

            var day = new CsvClient().GetDataAsync(request).GetAwaiter().GetResult().Times[new DateTime(2026, 6, 21)];

            Assert.Equal(new DateTime(2026, 6, 22), day.Isha.Date);
        }

        private string WriteCsv(params string[] rows)
        {
            var path = Path.Combine(Path.GetTempPath(), $"awqat-test-{Guid.NewGuid():N}.csv");
            File.WriteAllText(path, string.Join("\n", rows), Encoding.UTF8);
            files.Add(path);
            return path;
        }

        private static CsvRequest Request(string path, DateTime date, bool hasDateColumn) => new CsvRequest
        {
            FilePath = path,
            HasHeader = false,
            HasDateColumn = hasDateColumn,
            Range = CsvImportRange.Year,
            Date = date,
            GetEntireMonth = false,
            ColumnsMap = new Dictionary<string, int>
            {
                ["Fajr"] = 2, ["Shuruq"] = 3, ["Dhuhr"] = 4, ["Asr"] = 5, ["Maghrib"] = 6, ["Isha"] = 7,
            },
        };
    }
}
