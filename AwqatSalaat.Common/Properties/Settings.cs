﻿using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;
using AwqatSalaat.Services.Methods;
using System;
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

        static Settings()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            s_assemblyDirectory = Path.GetDirectoryName(assemblyPath);
        }

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
                UpdateSoundFilePath();
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

            UpdateSoundFilePath();

            base.OnSettingsLoaded(sender, e);
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

        private void UpdateSoundFilePath()
        {
            if (!EnableNotificationSound || string.IsNullOrEmpty(NotificationSoundFile))
            {
                NotificationSoundFilePath = null;
            }
            else
            {
                try
                {
                    string path = null;

                    if (Path.IsPathRooted(NotificationSoundFile))
                    {
                        // Use Path.GetFullPath to make sure we always have a drive letter
                        path = Path.GetFullPath(NotificationSoundFile);
                    }
                    else
                    {
                        path = Path.Combine(s_assemblyDirectory, NotificationSoundFile);
                    }

                    NotificationSoundFilePath = File.Exists(path) ? path : null;
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                }
            }
        }
    }
}
