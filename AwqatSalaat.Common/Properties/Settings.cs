using AwqatSalaat.Configurations;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;
using AwqatSalaat.Services.Methods;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;

namespace AwqatSalaat.Properties
{
    public partial class Settings
    {
        private static readonly string s_assemblyDirectory;

        private CalculationMethod _calculationMethod;
        private string _notificationSoundFilePath;
        private string _adhanSoundFilePath;
        private string _adhanFajrSoundFilePath;
        private bool _isLoaded;
        private bool _ignorePropertyChanged;
        private readonly Dictionary<string, PrayerConfig> _prayerConfigs = new Dictionary<string, PrayerConfig>();

        static Settings()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            s_assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        }

        public PrayerConfig GetPrayerConfig(string key) => _prayerConfigs[key];

        public CalculationMethod CalculationMethod
        {
            get => _calculationMethod;
            set
            {
                if (_calculationMethod != value)
                {
                    _calculationMethod = value;
                    base.OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(CalculationMethod)));
                }

                if (MethodString != value?.Id)
                {
                    MethodString = value?.Id;
                }
            }
        }

        public string NotificationSoundFilePath
        {
            get => _notificationSoundFilePath;
            private set
            {
                if (!string.Equals(_notificationSoundFilePath, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    _notificationSoundFilePath = value;
                    base.OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(NotificationSoundFilePath)));
                }
            }
        }

        public string AdhanSoundFilePath
        {
            get => _adhanSoundFilePath;
            private set
            {
                if (!string.Equals(_adhanSoundFilePath, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    _adhanSoundFilePath = value;
                    base.OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(AdhanSoundFilePath)));
                }
            }
        }

        public string AdhanFajrSoundFilePath
        {
            get => _adhanFajrSoundFilePath;
            private set
            {
                if (!string.Equals(_adhanFajrSoundFilePath, value, StringComparison.InvariantCultureIgnoreCase))
                {
                    _adhanFajrSoundFilePath = value;
                    base.OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(AdhanFajrSoundFilePath)));
                }
            }
        }

        protected override void OnSettingChanging(object sender, SettingChangingEventArgs e)
        {
            e.Cancel = object.Equals(this[e.SettingName], e.NewValue);

            base.OnSettingChanging(sender, e);
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(MethodString) && _calculationMethod?.Id != MethodString)
            {
                CalculationMethod = CalculationMethod.AvailableMethods.SingleOrDefault(m => m.Id == MethodString);
            }
            else if (e.PropertyName == nameof(NotificationSoundFile) || e.PropertyName == nameof(EnableNotificationSound))
            {
                UpdateNotificationSoundFilePath();
            }
            else if (e.PropertyName == nameof(AdhanSound))
            {
                InvalidateAdhanFiles();
            }
            else if (e.PropertyName == nameof(AdhanSoundFile))
            {
                UpdateAdhanSoundFilePath();
            }
            else if (e.PropertyName == nameof(AdhanFajrSoundFile))
            {
                UpdateAdhanFajrSoundFilePath();
            }
            else if (e.PropertyName.StartsWith("Config_") && !_ignorePropertyChanged)
            {
                UpdateSinglePrayerConfig(e.PropertyName);
            }
        }

        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            // CalculationMethod is added in v3.1, so we have to migrate previous settings
            if (string.IsNullOrEmpty(MethodString))
            {
                if (Service == Data.PrayerTimesService.IslamicFinder)
                {
                    MigrateToCalculationMethod(Method);
                }
                else if (Service == Data.PrayerTimesService.AlAdhan)
                {
                    MigrateToCalculationMethod(Method2);
                }
                else
                {
                    MethodString = "MWL";
                }
            }
            else
            {
                CalculationMethod = CalculationMethod.AvailableMethods.SingleOrDefault(m => m.Id == MethodString);
            }

            // Countries codes preset is added in v3.1, so we have to make sure the old value is uppercase to avoid confusion
            if (!string.IsNullOrEmpty(CountryCode))
            {
                CountryCode = CountryCode.ToUpper();
            }

            InvalidateAdhanFiles();

            UpdateNotificationSoundFilePath();
            UpdateAdhanSoundFilePath();
            UpdateAdhanFajrSoundFilePath();

            UpdatePrayerConfigs();

            base.OnSettingsLoaded(sender, e);
            _isLoaded = true;
        }

        protected override void OnSettingsSaving(object sender, CancelEventArgs e)
        {
            if (_calculationMethod?.Id != MethodString)
            {
                MethodString = _calculationMethod?.Id;
            }

            base.OnSettingsSaving(sender, e);
        }

        private void MigrateToCalculationMethod(IslamicFinderMethod islamicFinderMethod)
        {
            var method = CalculationMethod.AvailableMethods
                .OfType<IIslamicFinderMethod>()
                .Single(m => m.IslamicFinderMethod == islamicFinderMethod);

            var calculationMethod = (CalculationMethod)method;
            MethodString = calculationMethod.Id;
        }

        private void MigrateToCalculationMethod(AlAdhanMethod alAdhanMethod)
        {
            var method = CalculationMethod.AvailableMethods
                .OfType<IAlAdhanMethod>()
                .Single(m => m.AlAdhanMethod == alAdhanMethod);

            var calculationMethod = (CalculationMethod)method;
            MethodString = calculationMethod.Id;
        }

        private void UpdateNotificationSoundFilePath()
        {
            if (!EnableNotificationSound || string.IsNullOrEmpty(NotificationSoundFile))
            {
                NotificationSoundFilePath = null;
            }
            else
            {
                TrySetSoundFilePath(NotificationSoundFile, (s, p) => s.NotificationSoundFilePath = p);
            }
        }

        private void UpdateAdhanSoundFilePath()
        {
            if (AdhanSound == Data.AdhanSound.None || AdhanSound == Data.AdhanSound.Custom && string.IsNullOrEmpty(AdhanSoundFile))
            {
                AdhanSoundFilePath = null;
            }
            else
            {
                TrySetSoundFilePath(AdhanSoundFile, (s, p) => s.AdhanSoundFilePath = p);
            }
        }

        private void UpdateAdhanFajrSoundFilePath()
        {
            if (AdhanSound == Data.AdhanSound.None || AdhanSound == Data.AdhanSound.Custom && string.IsNullOrEmpty(AdhanFajrSoundFile))
            {
                AdhanFajrSoundFilePath = null;
            }
            else
            {
                TrySetSoundFilePath(AdhanFajrSoundFile, (s, p) => s.AdhanFajrSoundFilePath = p);
            }
        }

        private void TrySetSoundFilePath(string file, Action<Settings, string> pathSetter)
        {
            try
            {
                string path = null;

                if (Path.IsPathRooted(file))
                {
                    // Use Path.GetFullPath to make sure we always have a drive letter
                    path = Path.GetFullPath(file);
                }
                else
                {
                    // The path is relative
                    path = Path.Combine(s_assemblyDirectory, file);
                }

                pathSetter(this, File.Exists(path) ? path : null);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
        }

        private void InvalidateAdhanFiles()
        {
            switch (AdhanSound)
            {
                case Data.AdhanSound.None:
                    AdhanSoundFile = null;
                    AdhanFajrSoundFile = null;
                    break;
                case Data.AdhanSound.Adhan1:
                    AdhanSoundFile = @"Sounds\kholafa_13041446.mp3";
                    AdhanFajrSoundFile = @"Sounds\kholafa_13041446_full.mp3";
                    break;
                case Data.AdhanSound.Adhan2:
                    AdhanSoundFile = @"Sounds\kholafa_08041446.mp3";
                    AdhanFajrSoundFile = @"Sounds\kholafa_08041446_full.mp3";
                    break;
            }
        }

        private void UpdatePrayerConfigs()
        {
            foreach (var prop in Properties.OfType<SettingsProperty>())
            {
                if (prop.Name.StartsWith("Config_"))
                {
                    UpdateSinglePrayerConfig(prop.Name);
                }
            }
        }

        private void UpdateSinglePrayerConfig(string propertyName)
        {
            string key = propertyName.Split('_')[1];

            if (!_prayerConfigs.TryGetValue(key, out var config))
            {
                config = new PrayerConfig(key);
                config.PropertyChanged += PrayerConfig_PropertyChanged;
                _prayerConfigs[key] = config;
            }

            config.OnSettingsPropertyChanged(propertyName, this[propertyName]);
        }

        private void PrayerConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isLoaded)
            {
                var config = (PrayerConfig)sender;
                var setting = $"Config_{config.Key}_{e.PropertyName}";
                _ignorePropertyChanged = true;

                if (e.PropertyName == nameof(PrayerConfig.Adjustment))
                {
                    this[setting] = (sbyte)config.Adjustment;
                }
                else if (config.IsPrincipal)
                {
                    switch (e.PropertyName)
                    {
                        case nameof(PrayerConfig.ReminderOffset):
                            this[setting] = config.ReminderOffset;
                            break;
                        case nameof(PrayerConfig.ElapsedTime):
                            this[setting] = config.ElapsedTime;
                            break;
                        case nameof(PrayerConfig.GlobalReminderOffset):
                            this[setting] = config.GlobalReminderOffset;
                            break;
                        case nameof(PrayerConfig.GlobalElapsedTime):
                            this[setting] = config.GlobalElapsedTime;
                            break;
                    }
                }
                else if (e.PropertyName == nameof(PrayerConfig.IsVisible))
                {
                    this[setting] = config.IsVisible;
                }

                _ignorePropertyChanged = false;
            }
        }
    }
}
