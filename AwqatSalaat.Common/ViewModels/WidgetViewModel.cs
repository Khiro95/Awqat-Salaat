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

namespace AwqatSalaat.ViewModels
{
    public class WidgetViewModel : ObservableObject
    {
        private PrayerTimeViewModel next;
        private PrayerTimeViewModel displayedTime;
        private bool isRefreshing;
        private bool isNotificationActive;
        private DateTime displayedDate = TimeStamp.Date;
        private string error, city, country;
        private Dictionary<DateTime, PrayerTimes> latestData;
        private IServiceClient serviceClient;

        public PrayerTimeViewModel Next { get => next; private set => SetProperty(ref next, value); }
        public PrayerTimeViewModel DisplayedTime { get => displayedTime; private set => SetProperty(ref displayedTime, value); }
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
            new PrayerTimeViewModel(nameof(PrayerTimes.Shuruq)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Dhuhr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Asr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Maghrib)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Isha)),
        };
        public WidgetSettingsViewModel WidgetSettings { get; } = new WidgetSettingsViewModel();
        public RelayCommand Refresh { get; }
        public RelayCommand OpenSettings { get; }

        public event Action NearNotificationStarted;
        public event Action NearNotificationStopped;

        public WidgetViewModel()
        {
            foreach (var time in Items)
            {
                time.Entered += TimeEntered;
                time.EnteredNotificationDone += TimeEnteredNotificationDone;

                if (!time.IsShuruq)
                {
                    time.PropertyChanged += TimePropertyChanged;
                }
            }

            Refresh = new RelayCommand(o => RefreshData(), o => !isRefreshing);
            OpenSettings = new RelayCommand(o => WidgetSettings.IsOpen = true, o => !isRefreshing);
            WidgetSettings.Updated += SettingsUpdated;

            if (WidgetSettings.Settings.IsConfigured)
            {
                SettingsUpdated(false);
                UpdateServiceClient();

                var cached = JsonConvert.DeserializeObject<ServiceData>(WidgetSettings.Settings.ApiCache ?? "");

                if (cached != null)
                {
                    OnDataLoaded(cached);
                }

                RefreshData();
            }
        }

        private void SettingsUpdated(bool hasServiceSettingsChanged)
        {
            foreach (var time in Items)
            {
                time.Distance = WidgetSettings.Settings.NotificationDistance;
                time.DistanceElapsed = WidgetSettings.Settings.NotificationDistanceElapsed;
            }

            if (hasServiceSettingsChanged)
            {
                UpdateServiceClient();

                latestData = null;
                WidgetSettings.Settings.ApiCache = null;
                WidgetSettings.Settings.Save();
                RefreshData();
            }

            OnPropertyChanged(nameof(DisplayedDate));
        }

        private void TimeEntered(object sender, EventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;

            if (prayerTime.Key != nameof(PrayerTimes.Isha))
            {
                UpdateNext();
            }
        }

        private void TimeEnteredNotificationDone(object sender, EventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;

            // time jumps happen when PC enter sleep/hibernate mode then wakeup after hours
            bool timeJumped = displayedDate != TimeStamp.Date;
            var nextDate = timeJumped ? TimeStamp.Date : TimeStamp.NextDate;

            if (prayerTime.Key == nameof(PrayerTimes.Isha) || timeJumped)
            {
                DisplayedDate = nextDate;

                if (!Update())
                {
                    RefreshData();
                }
            }
            else
            {
                UpdateDisplayedTime(true);
            }
        }

        private void TimePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;

            if (e.PropertyName == nameof(PrayerTimeViewModel.State))
            {
                if (prayerTime.State == PrayerTimeState.Near)
                {
                    OnNearNotificationStarted();
                }
                else if (prayerTime.State == PrayerTimeState.Next || prayerTime.State == PrayerTimeState.EnteredRecently || prayerTime.State == PrayerTimeState.Entered)
                {
                    OnNearNotificationStopped();
                }
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
                UpdateDisplayedTime(false);
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
            Next = Items.Where(i => !i.IsEntered && !i.IsShuruq).OrderBy(i => i.Countdown.TotalSeconds).FirstOrDefault();

            if (next != null)
            {
                next.IsNext = true;
            }
        }

        private void UpdateDisplayedTime(bool skipLookup)
        {
            if (displayedTime != null)
            {
                displayedTime.IsActive = false;
            }

            var time = skipLookup ? null : Items.SingleOrDefault(i => i.State == PrayerTimeState.EnteredRecently);

            // Check if we should display Shuruq time since it cannot be marked as "Next"
            if (time is null && next?.Key == nameof(PrayerTimes.Dhuhr))
            {
                DisplayedTime = Items.SingleOrDefault(i => i.State == PrayerTimeState.ShuruqComing) ?? next;
            }
            else
            {
                DisplayedTime = time ?? next; 
            }

            if (displayedTime != null)
            {
                displayedTime.IsActive = true;

                // In case we are already in notification period
                if (displayedTime.State == PrayerTimeState.Near)
                {
                    OnNearNotificationStarted();
                }
            }

            FixTimeJumpSideEffect();
        }

        private void FixTimeJumpSideEffect()
        {
            var needFix = Items.Where(i => i.IsEntered && i.State != PrayerTimeState.Entered && i != displayedTime);

            foreach (var item in needFix)
            {
                if (item.Key == nameof(PrayerTimes.Isha))
                {
                    // Isha time is special since it lead to changing the date and updating all the times
                    TimeEnteredNotificationDone(item, EventArgs.Empty);
                }
                else
                {
                    // set IsNext to false to make the item update its state
                    item.IsNext = false;
                }
            }
        }

        private bool OnDataLoaded(ServiceData response)
        {
            latestData = response.Times;

            UpdateDisplayedLocation(response?.Location);

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

                // If Isha has entered, then we look for the next day
                if (DisplayedDate == TimeStamp.Date && TimeStamp.Now > apiResponse.Times[TimeStamp.Date].Max(t => t.Value).AddMinutes(WidgetSettings.Settings.NotificationDistanceElapsed))
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

                if (displayedTime != null)
                {
                    displayedTime.IsActive = false;
                    DisplayedTime = null;

                    if (isNotificationActive)
                    {
                        OnNearNotificationStopped();
                    }
                }

                ErrorMessage = ex.Message;
            }

            IsRefreshing = false;
        }

        private void OnNearNotificationStarted()
        {
            if (!isNotificationActive)
            {
                isNotificationActive = true;
                NearNotificationStarted?.Invoke();
            }
        }

        private void OnNearNotificationStopped()
        {
            if (isNotificationActive)
            {
                isNotificationActive = false;
                NearNotificationStopped?.Invoke();
            }
        }

        private void UpdateDisplayedLocation(Location responseLocation)
        {
            string country = null;
            string city = null;

            if (WidgetSettings.Settings.Service == PrayerTimesService.IslamicFinder)
            {
                country = responseLocation?.Country;
                city = responseLocation?.City;
            }

            if (string.IsNullOrEmpty(country) && WidgetSettings.Settings.CountryCode is string countryCode)
            {
                country = CountriesProvider.GetCountries().FirstOrDefault(c => c.Code == countryCode)?.Name;
            }

            Country = country;
            City = string.IsNullOrEmpty(city) ? WidgetSettings.Settings.City : city;
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
            RequestBase request;
            var settings = WidgetSettings.Settings;

            switch (settings.Service)
            {
                case PrayerTimesService.IslamicFinder:
                    request = new IslamicFinderRequest
                    {
                        CountryCode = settings.CountryCode,
                        ZipCode = settings.ZipCode,
                        TimeZone = settings.LocationDetection != LocationDetectionMode.ByCountryCode ? TimeZoneHelper.GetIanaTimeZone() : null,
                    };
                    break;
                case PrayerTimesService.AlAdhan:
                    request = new AlAdhanRequest
                    {
                        Country = settings.CountryCode,
                        City = settings.City,
                    };
                    break;
                default:
                    return null;
            }

            request.Date = date;
            request.GetEntireMonth = getEntireMonth;
            request.Method = settings.CalculationMethod;
            request.JuristicSchool = settings.School;

            if (settings.LocationDetection != LocationDetectionMode.ByCountryCode)
            {
                request.Latitude = settings.Latitude;
                request.Longitude = settings.Longitude;
                request.UseCoordinates = true;
            }

            return request;
        }
    }
}