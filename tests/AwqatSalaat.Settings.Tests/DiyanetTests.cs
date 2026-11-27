using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AwqatSalaat.Data;
using AwqatSalaat.Services.Diyanet;
using AwqatSalaat.ViewModels;
using Newtonsoft.Json;
using Xunit;

namespace AwqatSalaat.Settings.Tests
{
    // Issue #54: official Diyanet times, from https://ezanvakti.emushaf.net (no network used in these tests).
    public sealed class DiyanetTests : IDisposable
    {
        // Two days of a real response for Graz, Austria (district 11600), trimmed to the fields we read
        private const string GrazJson = @"[
 {""MiladiTarihKisa"":""15.11.2026"",""MiladiTarihUzunIso8601"":""2026-11-15T00:00:00.0000000+01:00"",
  ""Imsak"":""05:15"",""Gunes"":""06:54"",""Ogle"":""11:48"",""Ikindi"":""14:06"",""Aksam"":""16:31"",""Yatsi"":""17:59""},
 {""MiladiTarihKisa"":""16.11.2026"",""MiladiTarihUzunIso8601"":""2026-11-16T00:00:00.0000000+01:00"",
  ""Imsak"":""05:17"",""Gunes"":""06:56"",""Ogle"":""11:48"",""Ikindi"":""14:05"",""Aksam"":""16:30"",""Yatsi"":""17:58""}
]";

        private static readonly TimeZoneInfo CentralEurope = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        private static readonly TimeZoneInfo Turkey = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        private readonly Properties.Settings settings = Properties.Settings.Default;

        public DiyanetTests()
        {
            settings.Reset();
            settings.EnableLogs = false;
        }

        public void Dispose() => settings.Reset();

        [Fact]
        public void Times_InSameTimeZone_AreKeptAsTheyAre()
        {
            var times = DiyanetClient.BuildTimes(Days(GrazJson), CentralEurope);
            var day = times[new DateTime(2026, 11, 15)];

            Assert.Equal(2, times.Count);
            Assert.Equal(new DateTime(2026, 11, 15, 5, 15, 0), day.Fajr);
            Assert.Equal(new DateTime(2026, 11, 15, 6, 54, 0), day.Shuruq);
            Assert.Equal(new DateTime(2026, 11, 15, 11, 48, 0), day.Dhuhr);
            Assert.Equal(new DateTime(2026, 11, 15, 14, 6, 0), day.Asr);
            Assert.Equal(new DateTime(2026, 11, 15, 16, 31, 0), day.Maghrib);
            Assert.Equal(new DateTime(2026, 11, 15, 17, 59, 0), day.Isha);
        }

        [Fact]
        public void Times_InOtherTimeZone_AreConvertedWithTheDayOffset()
        {
            // Graz is UTC+1 in November, Turkey is UTC+3
            var day = DiyanetClient.BuildTimes(Days(GrazJson), Turkey)[new DateTime(2026, 11, 15)];

            Assert.Equal(new DateTime(2026, 11, 15, 7, 15, 0), day.Fajr);
            Assert.Equal(new DateTime(2026, 11, 15, 19, 59, 0), day.Isha);
        }

        [Fact]
        public void Times_OnSummerTime_AreKeptForTheSameZone()
        {
            var json = GrazJson.Replace("15.11.2026", "21.06.2026").Replace("2026-11-15T00:00:00.0000000+01:00", "2026-06-21T00:00:00.0000000+02:00");
            var day = DiyanetClient.BuildTimes(Days(json), CentralEurope)[new DateTime(2026, 6, 21)];

            Assert.Equal(new DateTime(2026, 6, 21, 5, 15, 0), day.Fajr);
        }

        [Fact]
        public void Request_UsesTheDistrict()
        {
            Assert.Equal("https://ezanvakti.emushaf.net/vakitler/11600", new DiyanetRequest { DistrictId = 11600 }.GetUrl());
        }

        [Fact]
        public async Task Client_WithoutDistrict_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => new DiyanetClient().GetDataAsync(new DiyanetRequest()));
        }

        [Fact]
        public void Save_WithoutDistrict_IsRejected()
        {
            var vm = new WidgetSettingsViewModel();
            string rejected = null;
            vm.SaveRejected += e => rejected = e;
            vm.Realtime.Service = PrayerTimesService.Diyanet;
            vm.Realtime.DiyanetDistrict = 0;

            vm.Save.Execute(null);

            Assert.Equal("MissingDiyanetDistrict", rejected);
            vm.Cancel.Execute(null);
        }

        [Fact]
        public async Task Locator_RestoresTheSavedChoice()
        {
            settings.DiyanetCountry = 35;
            settings.DiyanetCity = 657;
            settings.DiyanetDistrict = 11600;
            var vm = FakeLocator(Properties.Settings.Default);

            await vm.EnsureLoadedAsync();

            Assert.Equal("AUSTRIA", vm.SelectedCountry?.Name);
            Assert.Equal(657, vm.SelectedCity?.Id);
            Assert.Equal("GRAZ", vm.SelectedDistrict?.Name);
            Assert.Equal(11600, settings.DiyanetDistrict);
        }

        [Fact]
        public async Task Locator_PickingPlaces_WritesTheSettings()
        {
            var vm = FakeLocator(settings);
            await vm.EnsureLoadedAsync();
            Assert.Null(vm.SelectedCountry);

            vm.SelectedCountry = vm.Countries.Single(c => c.Name == "AUSTRIA");
            await WaitIdle(vm);
            // Austria has a single "city", it is selected for the user
            Assert.Equal(657, vm.SelectedCity?.Id);
            Assert.Equal(2, vm.Districts.Count);

            vm.SelectedDistrict = vm.Districts.Single(d => d.Name == "GRAZ");
            Assert.Equal(11600, settings.DiyanetDistrict);
            Assert.Equal("GRAZ", settings.DiyanetDistrictName);
            Assert.Equal("AUSTRIA", settings.DiyanetCountryName);

            // Another country clears the district until one is picked there
            vm.SelectedCountry = vm.Countries.Single(c => c.Name == "TÜRKİYE");
            await WaitIdle(vm);
            Assert.Equal(0, settings.DiyanetDistrict);
            Assert.Null(vm.SelectedCity);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("ar")]
        [InlineData("tr")]
        [InlineData("ku")]
        public void ServiceHasLocalizedTexts(string language)
        {
            var resources = Properties.Resources.ResourceManager.GetResourceSet(new CultureInfo(language), true, true);
            var keys = new[]
            {
                "Data.PrayerTimesService.Diyanet", "UI.Settings.DiyanetCountry", "UI.Settings.DiyanetCity",
                "UI.Settings.DiyanetDistrict", "UI.Settings.DiyanetLocationDescription", "Dialog.MissingDiyanetDistrict",
            };

            foreach (var key in keys)
            {
                Assert.False(string.IsNullOrWhiteSpace(resources.GetString(key)), $"{language}: {key}");
            }
        }

        private static DiyanetDay[] Days(string json) => JsonConvert.DeserializeObject<DiyanetDay[]>(json);

        private static DiyanetLocationViewModel FakeLocator(Properties.Settings target)
        {
            IList<DiyanetPlace> countries = new[] { new DiyanetPlace(35, "AUSTRIA"), new DiyanetPlace(2, "TÜRKİYE") };
            var cities = new Dictionary<int, IList<DiyanetPlace>>
            {
                [35] = new[] { new DiyanetPlace(657, "AUSTRIA") },
                [2] = new[] { new DiyanetPlace(539, "ISTANBUL"), new DiyanetPlace(506, "ANKARA") },
            };
            var districts = new Dictionary<int, IList<DiyanetPlace>>
            {
                [657] = new[] { new DiyanetPlace(11600, "GRAZ"), new DiyanetPlace(11618, "WIEN") },
            };

            return new DiyanetLocationViewModel(target)
            {
                LoadCountries = () => Task.FromResult(countries),
                LoadCities = id => Task.FromResult(cities[id]),
                LoadDistricts = id => Task.FromResult(districts.TryGetValue(id, out var d) ? d : (IList<DiyanetPlace>)new DiyanetPlace[0]),
            };
        }

        private static async Task WaitIdle(DiyanetLocationViewModel vm)
        {
            for (int i = 0; i < 50 && vm.IsBusy; i++)
            {
                await Task.Delay(10);
            }
        }
    }
}
