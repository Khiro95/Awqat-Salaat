using AwqatSalaat.Configurations;
using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.GitHub;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Settings = AwqatSalaat.Properties.Settings;

namespace AwqatSalaat.ViewModels
{
    public class WidgetSettingsViewModel : ObservableObject
    {
        private bool isOpen = !Settings.Default.IsConfigured;
        private bool isCheckingNewVersion;

        public static Country[] AvailableCountries => CountriesProvider.GetCountries();

        public bool IsOpen { get => isOpen; set => SetProperty(ref isOpen, value); }
        public bool IsCheckingNewVersion { get => isCheckingNewVersion; set => SetProperty(ref isCheckingNewVersion, value); }
        public bool UseArabic
        {
            get => Realtime.DisplayLanguage == "ar";
            set
            {
                if (value)
                {
                    SetLanguage("ar");
                }
            }
        }
        public bool UseEnglish
        {
            get => Realtime.DisplayLanguage == "en";
            set
            {
                if (value)
                {
                    SetLanguage("en");
                }
            }
        }
        public string CountdownFormat => Realtime.ShowSeconds ? "{0:hh\\:mm\\:ss}" : "{0:hh\\:mm}";
        public Settings Settings => Settings.Default;

        // realtime settings are binded to settings UI so changes are reflected immediately
        public Settings Realtime { get; } = new Settings();
        public RelayCommand Save { get; }
        public RelayCommand Cancel { get; }
        public LocatorViewModel Locator { get; }
        public ObservableCollection<PrayerConfig> PrayerConfigs { get; }

        public event Action<bool> Updated;

        public WidgetSettingsViewModel()
        {
            Save = new RelayCommand(SaveExecute);
            Cancel = new RelayCommand(CancelExecute, o => Settings.IsConfigured);

            if (!Settings.IsConfigured)
            {
                Settings.Upgrade();
            }

            if (string.IsNullOrEmpty(Settings.DisplayLanguage))
            {
                Settings.DisplayLanguage = LocaleManager.Default.Current;
            }

            CopySettings(fromOriginal: true);

            Locator = new LocatorViewModel(Realtime);

            Realtime.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Realtime.DisplayLanguage))
                {
                    OnPropertyChanged(nameof(UseArabic));
                    OnPropertyChanged(nameof(UseEnglish));
                }
                else if (e.PropertyName == nameof(Realtime.ShowSeconds))
                {
                    OnPropertyChanged(nameof(CountdownFormat));
                }
            };

            PrayerConfigs = new ObservableCollection<PrayerConfig>
            {
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Fajr)),
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Shuruq)),
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Dhuhr)),
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Asr)),
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Maghrib)),
                Realtime.GetPrayerConfig(nameof(PrayerTimes.Isha)),
            };
        }

        public async Task<Release> CheckForNewVersion(Version currentVersion, CancellationToken cancellationToken)
        {
            try
            {
                IsCheckingNewVersion = true;

                var latest = await GitHubClient.GetLatestRelease(cancellationToken);

                if (cancellationToken.IsCancellationRequested || latest is null)
                {
                    return null;
                }

                if (latest.IsDraft || latest.IsPreRelease)
                {
                    var allReleases = await GitHubClient.GetReleases(cancellationToken);

                    if (allReleases?.Length > 1)
                    {
                        latest = allReleases
                            .Where(r => !r.IsDraft && !r.IsPreRelease)
                            .DefaultIfEmpty(new Release { Tag = "0.0" })
                            .OrderByDescending(r => r.GetVersion())
                            .First();
                    }
                    else
                    {
                        return null;
                    }
                }

                return latest.GetVersion() > currentVersion ? latest : null;
            }
            finally
            {
                IsCheckingNewVersion = false;
            }
        }

        private void CopySettings(bool fromOriginal)
        {
            Settings source = fromOriginal ? Settings : Realtime;
            Settings destination = fromOriginal ? Realtime : Settings;

            foreach (SettingsProperty prop in Settings.Properties)
            {
                if (prop.Name == nameof(Settings.CustomPosition))
                {
                    // we are not interested in custom position here and we should not affect it
                    continue;
                }

                destination[prop.Name] = source[prop.Name];
            }
        }

        private void SaveExecute(object obj)
        {
            Log.Information("[Settings] Save invoked");
            var currentServiceSettings = (
                    Realtime.Service,
                    Realtime.School,
                    Realtime.MethodString,
                    Realtime.CountryCode,
                    Realtime.City,
                    Realtime.ZipCode,
                    Realtime.Latitude,
                    Realtime.Longitude,
                    Realtime.LocationDetection
                    );
            var previousServiceSettings = (
                    Settings.Service,
                    Settings.School,
                    Settings.MethodString,
                    Settings.CountryCode,
                    Settings.City,
                    Settings.ZipCode,
                    Settings.Latitude,
                    Settings.Longitude,
                    Settings.LocationDetection
                    );
            bool serviceSettingsChanged = previousServiceSettings != currentServiceSettings;
            Realtime.IsConfigured = true;
            CopySettings(fromOriginal: false);
            Settings.Save();
            IsOpen = false;
            LogManager.InvalidateLogger();
            Cancel.RaiseCanExecuteChanged();
            Updated?.Invoke(serviceSettingsChanged);
        }

        private void CancelExecute(object obj)
        {
            Log.Information("[Settings] Cancel invoked");
            CopySettings(fromOriginal: true);
            Locator.SearchQuery = null;
            SetLanguage(Settings.DisplayLanguage);
            IsOpen = false;
            Cancel.RaiseCanExecuteChanged();
        }

        private void SetLanguage(string lang)
        {
            LocaleManager.Default.Current = lang;
            Realtime.DisplayLanguage = lang;
        }
    }
}