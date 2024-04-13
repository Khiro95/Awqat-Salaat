using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;
using System;
using System.Windows.Input;

using Settings = AwqatSalaat.Properties.Settings;

namespace AwqatSalaat.ViewModels
{
    public class WidgetSettingsViewModel : ObservableObject
    {
        private bool isOpen = !Settings.Default.IsConfigured;
        private PrayerTimesService? _serviceBackup;
        private (string countryCode, string zipCode, IslamicFinderMethod method)? _islamicFinderBackup;
        private (string countryCode, string city, AlAdhanMethod method)? _alAdhanBackup;

        public bool IsOpen { get => isOpen; set => Open(value); }
        public bool UseArabic { get => Settings.DisplayLanguage == "ar"; set => SetLanguage("ar"); }
        public bool UseEnglish { get => Settings.DisplayLanguage == "en"; set => SetLanguage("en"); }
        public Settings Settings => Settings.Default;
        public RelayCommand Save { get; }
        public RelayCommand Cancel { get; }

        public event Action<bool> Updated;

        public WidgetSettingsViewModel()
        {
            Save = new RelayCommand(SaveExecute);
            Cancel = new RelayCommand(CancelExecute, o => Settings.IsConfigured);
            Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.DisplayLanguage))
                {
                    OnPropertyChanged(nameof(UseArabic));
                    OnPropertyChanged(nameof(UseEnglish));
                }
            };
        }

        private void SaveExecute(object obj)
        {
            bool apiSettingsChanged = Settings.Service != _serviceBackup;
            apiSettingsChanged |= (Settings.CountryCode, Settings.ZipCode, Settings.Method) != _islamicFinderBackup;
            apiSettingsChanged |= (Settings.CountryCode, Settings.City, Settings.Method2) != _alAdhanBackup;
            Settings.IsConfigured = true;
            Settings.Save();
            IsOpen = false;
            Cancel.RaiseCanExecuteChanged();
            Updated?.Invoke(apiSettingsChanged);
        }

        private void CancelExecute(object obj)
        {
            Settings.Reload();
            SetLanguage(Settings.DisplayLanguage);
            IsOpen = false;
            Cancel.RaiseCanExecuteChanged();
        }

        private void Open(bool value)
        {
            SetProperty(ref isOpen, value, nameof(IsOpen));
            if (value)
            {
                _serviceBackup = Settings.Service;
                _islamicFinderBackup = (Settings.CountryCode, Settings.ZipCode, Settings.Method);
                _alAdhanBackup = (Settings.CountryCode, Settings.City, Settings.Method2);
            }
            else
            {
                _serviceBackup = null;
                _islamicFinderBackup = null;
                _alAdhanBackup = null;
            }
        }

        private void SetLanguage(string lang)
        {
            LocaleManager.Current = lang;
        }
    }
}