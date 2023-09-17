using AwqatSalaat.DataModel.IslamicFinderApi;
using AwqatSalaat.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.UI.ViewModels
{
    public class WidgetViewModel : ObservableObject
    {
        private PrayerTimeViewModel next;
        private bool isRefreshing;
        private DateTime displayedDate = TimeStamp.Date;
        private string error, city, country;
        private Dictionary<DateTime, PrayerTimes> latestData;

        public PrayerTimeViewModel Next { get => next; private set => SetProperty(ref next, value); }
        public bool IsRefreshing { get => isRefreshing; private set => SetProperty(ref isRefreshing, value); }
        public string ErrorMessage { get => error; private set { SetProperty(ref error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(error);
        public DateTime DisplayedDate { get => displayedDate; private set => SetProperty(ref displayedDate, value); }
        public string Country { get => country; private set => SetProperty(ref country, value); }
        public string City { get => city; private set => SetProperty(ref city, value); }
        public ObservableCollection<PrayerTimeViewModel> Items { get; } = new ObservableCollection<PrayerTimeViewModel>()
        {
            new PrayerTimeViewModel(nameof(PrayerTimes.Fajr), "الفجر"),
            new PrayerTimeViewModel(nameof(PrayerTimes.Dhuhr), "الظهر"),
            new PrayerTimeViewModel(nameof(PrayerTimes.Asr), "العصر"),
            new PrayerTimeViewModel(nameof(PrayerTimes.Maghrib), "المغرب"),
            new PrayerTimeViewModel(nameof(PrayerTimes.Isha), "العشاء"),
        };
        public WidgetSettingsViewModel WidgetSettings { get; } = new WidgetSettingsViewModel();
        public ICommand Refresh { get; }
        public ICommand OpenSettings { get; }

        public WidgetViewModel()
        {
            foreach (var item in Items)
            {
                item.Elapsed += TimeElapsed;
            }
            Refresh = new RelayCommand(o => RefreshData(), o => !isRefreshing);
            OpenSettings = new RelayCommand(o => WidgetSettings.IsOpen = true);
            WidgetSettings.Updated += SettingsUpdated;

            if (WidgetSettings.Settings.IsConfigured)
            {
                SettingsUpdated(false);
                var cached = JsonConvert.DeserializeObject<EntireMonthResponse>(WidgetSettings.Settings.ApiCache);
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
            Next = Items.Where(i => !i.IsElapsed).OrderBy(i => i.Countdown.TotalSeconds).First();
            Next.IsNext = true;
        }

        private bool OnDataLoaded(EntireMonthResponse response)
        {
            latestData = response.Times;
            Country = response.Settings.Location.Country;
            City = response.Settings.Location.City;
            return Update();
        }

        private async Task RefreshData()
        {
            try
            {
                ErrorMessage = null;
                IsRefreshing = true;
                var apiResponse = await Client.GetEntireMonthDataAsync(
                    WidgetSettings.Settings.CountryCode,
                    WidgetSettings.Settings.ZipCode,
                    WidgetSettings.Settings.Method,
                    DisplayedDate);
                // If Isha has passed, then we look for the next day
                if (DisplayedDate == TimeStamp.Date && TimeStamp.Now > apiResponse.Times[TimeStamp.Date].Max(t => t.Value))
                {
                    DisplayedDate = TimeStamp.NextDate;
                    if (TimeStamp.Date.Month != TimeStamp.NextDate.Month)
                    {
                        latestData = null;
                        apiResponse = await Client.GetEntireMonthDataAsync(
                            WidgetSettings.Settings.CountryCode,
                            WidgetSettings.Settings.ZipCode,
                            WidgetSettings.Settings.Method,
                            TimeStamp.NextDate);
                    }
                }
                OnDataLoaded(apiResponse);

                // Cache the result for offline use, just in case
                var cache = new
                {
                    apiResponse.Results,
                    apiResponse.Settings,
                    apiResponse.Success
                };
                WidgetSettings.Settings.ApiCache = JsonConvert.SerializeObject(cache);
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
                    next = null;
                }
                ErrorMessage = ex.Message;
            }
            IsRefreshing = false;
        }
    }
}
