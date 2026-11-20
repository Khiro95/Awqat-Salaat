using System;
using System.Globalization;
using AwqatSalaat.Data;
using AwqatSalaat.Properties;
using AwqatSalaat.ViewModels;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AwqatSalaat.Settings.Tests
{
    // ApplicationSettingsBase uses the test host's configuration, separate from the app.
    public sealed class TrayIconSettingsTests : IDisposable
    {
        private readonly Properties.Settings settings = Properties.Settings.Default;

        public TrayIconSettingsTests()
        {
            settings.Reset();
            settings.IsConfigured = true;
            settings.Service = PrayerTimesService.AlAdhan;
            settings.City = "Test city";
            settings.EnableLogs = false;
        }

        public void Dispose()
        {
            settings.Reset();
        }

        [Fact]
        public void TrayIconIsVisibleByDefault()
        {
            Assert.True(settings.ShowTrayIcon);
            Assert.Equal("True", settings.Properties[nameof(settings.ShowTrayIcon)].DefaultValue);
        }

        [Fact]
        public void CancelRestoresTheSavedTrayPreference()
        {
            var viewModel = new WidgetSettingsViewModel();
            viewModel.Realtime.ShowTrayIcon = false;

            Assert.True(settings.ShowTrayIcon);
            viewModel.Cancel.Execute(null);

            Assert.True(viewModel.Realtime.ShowTrayIcon);
            Assert.True(settings.ShowTrayIcon);
        }

        [Fact]
        public void SavePersistsHiddenPreferenceAndCancelRestoresIt()
        {
            var viewModel = new WidgetSettingsViewModel();
            bool? serviceChanged = null;
            viewModel.Updated += changed => serviceChanged = changed;
            viewModel.Realtime.ShowTrayIcon = false;
            viewModel.Save.Execute(null);

            Assert.False(settings.ShowTrayIcon);
            Assert.Equal(false, serviceChanged);
            Assert.False(new Properties.Settings().ShowTrayIcon);

            viewModel.Realtime.ShowTrayIcon = true;
            viewModel.Cancel.Execute(null);
            Assert.False(viewModel.Realtime.ShowTrayIcon);

            viewModel.Realtime.ShowTrayIcon = true;
            viewModel.Save.Execute(null);
            Assert.True(new Properties.Settings().ShowTrayIcon);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("ar")]
        [InlineData("tr")]
        [InlineData("ku")]
        public void TraySettingHasLocalizedLabelAndRecoveryHelp(string language)
        {
            var resources = Resources.ResourceManager.GetResourceSet(new CultureInfo(language), true, true);
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString("UI.Settings.ShowTrayIcon")));
            Assert.False(string.IsNullOrWhiteSpace(resources.GetString("UI.Settings.ShowTrayIconDescription")));
        }
    }
}
