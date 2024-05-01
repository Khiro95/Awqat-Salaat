using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.ViewModels
{
    public class WidgetViewModel : ObservableObject
    {
        private PrayerTimeViewModel next;
        private bool isRefreshing;
        private DateTime displayedDate = TimeStamp.Date;
        private string error, city, country;
        private Dictionary<DateTime, PrayerTimes> latestData;
        private IServiceClient serviceClient;

        public PrayerTimeViewModel Next { get => next; private set => SetProperty(ref next, value); }
        public bool IsRefreshing
        {
            get => isRefreshing;
            private set
            {
                SetProperty(ref isRefreshing, value);
                Refresh.RaiseCanExecuteChanged();
                OpenSettings.RaiseCanExecuteChanged();
            }
        }
        public string ErrorMessage { get => error; private set { SetProperty(ref error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(error);
        public DateTime DisplayedDate { get => displayedDate; private set => SetProperty(ref displayedDate, value); }
        public string Country { get => country; private set => SetProperty(ref country, value); }
        public string City { get => city; private set => SetProperty(ref city, value); }
        public ObservableCollection<PrayerTimeViewModel> Items { get; } = new ObservableCollection<PrayerTimeViewModel>()
        {
            new PrayerTimeViewModel(nameof(PrayerTimes.Fajr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Dhuhr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Asr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Maghrib)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Isha)),
        };
        public WidgetSettingsViewModel WidgetSettings { get; } = new WidgetSettingsViewModel();
        public RelayCommand Refresh { get; }
        public RelayCommand OpenSettings { get; }

        public WidgetViewModel()
        {
            foreach (var item in Items)
            {
                item.Elapsed += TimeElapsed;
            }

            Refresh = new RelayCommand(o => RefreshData(), o => !isRefreshing);
            OpenSettings = new RelayCommand(o => WidgetSettings.IsOpen = true, o => !isRefreshing);
            WidgetSettings.Updated += SettingsUpdated;

            if (WidgetSettings.Settings.IsConfigured)
            {
                SettingsUpdated(false);
                UpdateServiceClient();

                var cached = JsonConvert.DeserializeObject<ServiceData>(WidgetSettings.Settings.ApiCache);

                if (cached != null)
                {
                    OnDataLoaded(cached);
                }

                RefreshData();
            }
        }

        private void SettingsUpdated(bool hasApiSettingsChanged)
        {
            foreach (var time in Items)
            {
                time.Distance = WidgetSettings.Settings.NotificationDistance;
            }

            if (hasApiSettingsChanged)
            {
                UpdateServiceClient();

                latestData = null;
                WidgetSettings.Settings.ApiCache = null;
                WidgetSettings.Settings.Save();
                RefreshData();
            }
        }

        private void TimeElapsed(object sender, EventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;

            if (prayerTime.Key == nameof(PrayerTimes.Isha))
            {
                DisplayedDate = TimeStamp.NextDate;

                if (!Update())
                {
                    RefreshData();
                }
            }
            else
            {
                UpdateNext();
            }
        }

        private bool Update()
        {
            if (latestData?.Count > 0 && latestData.ContainsKey(displayedDate))
            {
                foreach (var time in latestData[displayedDate])
                {
                    Items.Single(i => i.Key == time.Key).SetTime(time.Value);
                }

                UpdateNext();
                return true;
            }

            return false;
        }

        private void UpdateNext()
        {
            if (next != null)
            {
                next.IsNext = false;
            }

            // Sorting doesn't hurt here, we only have 5 items :)
            Next = Items.Where(i => !i.IsElapsed).OrderBy(i => i.Countdown.TotalSeconds).FirstOrDefault();

            if (next != null)
            {
                next.IsNext = true;
            }
        }

        private bool OnDataLoaded(ServiceData response)
        {
            latestData = response.Times;
            Country = response.Location?.Country;
            City = response.Location?.City;
            return Update();
        }

        private async Task RefreshData()
        {
            try
            {
                ErrorMessage = null;
                IsRefreshing = true;

                IRequest request = BuildRequest(DisplayedDate, true);

                var apiResponse = await serviceClient.GetDataAsync(request);

                // If Isha has passed, then we look for the next day
                if (DisplayedDate == TimeStamp.Date && TimeStamp.Now > apiResponse.Times[TimeStamp.Date].Max(t => t.Value))
                {
                    DisplayedDate = TimeStamp.NextDate;

                    if (TimeStamp.Date.Month != TimeStamp.NextDate.Month)
                    {
                        request = BuildRequest(TimeStamp.NextDate, true);
                        apiResponse = await serviceClient.GetDataAsync(request);
                    }
                }

                OnDataLoaded(apiResponse);

                // Cache the result for offline use, just in case
                WidgetSettings.Settings.ApiCache = JsonConvert.SerializeObject(apiResponse);
                WidgetSettings.Settings.Save();
            }
            catch (NetworkException nex)
            {
                if (!WidgetSettings.Settings.IsConfigured || latestData is null)
                {
                    ErrorMessage = nex.Message;
                }
            }
            catch (Exception ex)
            {
                if (next != null)
                {
                    next.IsNext = false;
                    Next = null;
                }

                ErrorMessage = ex.Message;
            }

            IsRefreshing = false;
        }

        private void UpdateServiceClient()
        {
            switch (WidgetSettings.Settings.Service)
            {
                case PrayerTimesService.IslamicFinder:
                    serviceClient = new IslamicFinderClient();
                    break;
                case PrayerTimesService.AlAdhan:
                    serviceClient = new AlAdhanClient();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IRequest BuildRequest(DateTime date, bool getEntireMonth)
        {
            switch (WidgetSettings.Settings.Service)
            {
                case PrayerTimesService.IslamicFinder:
                    return new IslamicFinderRequest
                    {
                        CountryCode = WidgetSettings.Settings.CountryCode,
                        ZipCode = WidgetSettings.Settings.ZipCode,
                        Method = WidgetSettings.Settings.CalculationMethod,
                        Date = date,
                        GetEntireMonth = getEntireMonth
                    };
                case PrayerTimesService.AlAdhan:
                    return new AlAdhanRequest
                    {
                        Country = WidgetSettings.Settings.CountryCode,
                        City = WidgetSettings.Settings.City,
                        Method = WidgetSettings.Settings.CalculationMethod,
                        Date = date,
                        GetEntireMonth = getEntireMonth
                    };
                default:
                    return null;
            }
        }
    }
}