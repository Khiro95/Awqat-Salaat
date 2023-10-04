using AwqatSalaat.Helpers;
using AwqatSalaat.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.UI.ViewModels
{
    public class WidgetSettingsViewModel : ObservableObject
    {
        private bool isOpen = !Settings.Default.IsConfigured;
        private (string countryCode, string zipCode, DataModel.IslamicFinderApi.Method method)? _backup;

        public bool IsOpen { get => isOpen; set => Open(value); }
        public bool UseArabic { get => Settings.DisplayLanguage == "ar"; set => SetLanguage("ar"); }
        public bool UseEnglish { get => Settings.DisplayLanguage == "en"; set => SetLanguage("en"); }
        public Settings Settings => Settings.Default;
        public ICommand Save { get; }
        public ICommand Cancel { get; }

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
            bool apiSettingsChanged = (Settings.CountryCode, Settings.ZipCode, Settings.Method) != _backup;
            Settings.IsConfigured = true;
            Settings.Save();
            IsOpen = false;
            Updated?.Invoke(apiSettingsChanged);
        }

        private void CancelExecute(object obj)
        {
            Settings.Reload();
            SetLanguage(Settings.DisplayLanguage);
            IsOpen = false;
        }

        private void Open(bool value)
        {
            SetProperty(ref isOpen, value, nameof(IsOpen));
            if (value)
            {
                _backup = (Settings.CountryCode, Settings.ZipCode, Settings.Method);
            }
            else
            {
                _backup = null;
            }
        }

        private void SetLanguage(string lang)
        {
            LocaleManager.Current = lang;
        }
    }
}
