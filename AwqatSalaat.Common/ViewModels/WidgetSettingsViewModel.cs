using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.GitHub;
using System;
using System.Linq;
using System.Threading.Tasks;
using Settings = AwqatSalaat.Properties.Settings;

namespace AwqatSalaat.ViewModels
{
    public class WidgetSettingsViewModel : ObservableObject
    {
        private bool isOpen = !Settings.Default.IsConfigured;
        private bool isCheckingNewVersion;
        private (
            PrayerTimesService service,
            School school,
            string method,
            string countryCode,
            string city,
            string zipCode,
            decimal latitude,
            decimal longitude,
            LocationDetectionMode locationDetection)? _serviceSettingsBackup;

        public static Country[] AvailableCountries => CountriesProvider.GetCountries();

        public bool IsOpen { get => isOpen; set => Open(value); }
        public bool IsCheckingNewVersion { get => isCheckingNewVersion; set => SetProperty(ref isCheckingNewVersion, value); }
        public bool UseArabic
        {
            get => Settings.DisplayLanguage == "ar";
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
            get => Settings.DisplayLanguage == "en";
            set
            {
                if (value)
                {
                    SetLanguage("en");
                }
            }
        }
        public string CountdownFormat => Settings.ShowSeconds ? "{0:hh\\:mm\\:ss}" : "{0:hh\\:mm}";
        public Settings Settings => Settings.Default;
        public RelayCommand Save { get; }
        public RelayCommand Cancel { get; }
        public LocatorViewModel Locator { get; } = new LocatorViewModel();

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

            Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.DisplayLanguage))
                {
                    OnPropertyChanged(nameof(UseArabic));
                    OnPropertyChanged(nameof(UseEnglish));
                }
                else if (e.PropertyName == nameof(Settings.ShowSeconds))
                {
                    OnPropertyChanged(nameof(CountdownFormat));
                }
            };
        }

        public async Task<Release> CheckForNewVersion(Version currentVersion)
        {
            try
            {
                IsCheckingNewVersion = true;

                var latest = await GitHubClient.GetLatestRelease();

                if (latest is null)
                {
                    return null;
                }

                if (latest.IsDraft || latest.IsPreRelease)
                {
                    var allReleases = await GitHubClient.GetReleases();

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

        private void SaveExecute(object obj)
        {
            var currentServiceSettings = (
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
            bool serviceSettingsChanged = _serviceSettingsBackup != currentServiceSettings;
            Settings.IsConfigured = true;
            Settings.Save();
            IsOpen = false;
            Cancel.RaiseCanExecuteChanged();
            Updated?.Invoke(serviceSettingsChanged);
        }

        private void CancelExecute(object obj)
        {
            Settings.Reload();
            Locator.SearchQuery = null;
            SetLanguage(Settings.DisplayLanguage);
            IsOpen = false;
            Cancel.RaiseCanExecuteChanged();
        }

        private void Open(bool value)
        {
            SetProperty(ref isOpen, value, nameof(IsOpen));

            if (value)
            {
                _serviceSettingsBackup = (
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
            }
            else
            {
                _serviceSettingsBackup = null;
            }
        }

        private void SetLanguage(string lang)
        {
            LocaleManager.Default.Current = lang;
        }
    }
}