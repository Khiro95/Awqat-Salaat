using AwqatSalaat.Data;
using AwqatSalaat.Extensions;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;
using AwqatSalaat.Services.Local;
using AwqatSalaat.Services.QCH;
using AwqatSalaat.Services.WaktuSolat;
using AwqatSalaat.Services.CSV;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

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
                OnPropertyChanged(nameof(IsShowingTimes));
            }
        }
        public string ErrorMessage
        {
            get => error;
            private set
            {
                SetProperty(ref error, value);
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(IsShowingTimes));
            }
        }
        public bool HasError => !string.IsNullOrEmpty(error);
        public bool IsShowingTimes => !isRefreshing && !HasError;
        public DateTime DisplayedDate { get => displayedDate; private set => SetProperty(ref displayedDate, value); }
        public string Country { get => country; private set => SetProperty(ref country, value); }
        public string City { get => city; private set => SetProperty(ref city, value); }
        public WidgetSettingsViewModel WidgetSettings { get; } = new WidgetSettingsViewModel();
        public ObservableCollection<PrayerTimeViewModel> Items { get; } = new ObservableCollection<PrayerTimeViewModel>()
        {
            new PrayerTimeViewModel(nameof(PrayerTimes.Fajr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Shuruq)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Dhuhr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Asr)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Maghrib)),
            new PrayerTimeViewModel(nameof(PrayerTimes.Isha)),
        };
        public RelayCommand Refresh { get; }
        public RelayCommand OpenSettings { get; }

        public event Action NearNotificationStarted;
        public event Action NearNotificationStopped;
        public event Action<PrayerTimeViewModel> PrayerTimeEntered;

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
            OpenSettings = new RelayCommand(o => OpenSettingsExecute(), o => !isRefreshing);
            WidgetSettings.Updated += SettingsUpdated;

            if (WidgetSettings.Settings.IsConfigured)
            {
                SettingsUpdated(false);
                UpdateServiceClient();

                var cached = JsonConvert.DeserializeObject<ServiceData>(Properties.PersistentCache.Default.ApiData ?? "");

                if (cached != null)
                {
                    Log.Debug("Loading data from cache");
                    OnDataLoaded(cached);
                }

                RefreshData();
            }

            TimeStamp.DateChanged += TimeStamp_DateChanged;
        }

        private void TimeStamp_DateChanged()
        {
            if (WidgetSettings.Settings.IsConfigured && WidgetSettings.Settings.Service == PrayerTimesService.QCH)
            {
                RefreshData();
            }
        }

        private void OpenSettingsExecute()
        {
            Log.Information("Opening settings");
            WidgetSettings.IsOpen = true;
        }

        private void SettingsUpdated(bool hasServiceSettingsChanged)
        {
            Log.Information("Settings updated" + (hasServiceSettingsChanged ? " (service settings changed)" : ""));

            foreach (var time in Items)
            {
                time.InvalidateConfig();
                time.RaiseTimePropertyChanged();
            }

            if (hasServiceSettingsChanged)
            {
                UpdateServiceClient();

                latestData = null;
                Log.Debug("Clearing cache");
                Properties.PersistentCache.Default.ApiData = null;
                Properties.PersistentCache.Default.Save();
                RefreshData();
            }

            OnPropertyChanged(nameof(DisplayedDate));
        }

        private void TimeEntered(object sender, EventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;
            Log.Information($"Time entered: {prayerTime.Key}");

            if (prayerTime.Key != nameof(PrayerTimes.Isha))
            {
                UpdateNext();
            }

            // Make sure there is no time jump
            if (!prayerTime.IsShuruq
                && prayerTime.Time.Date == TimeStamp.Date
                && prayerTime.Time.Hour == TimeStamp.Now.Hour
                && prayerTime.Time.Minute == TimeStamp.Now.Minute)
            {
                PrayerTimeEntered?.Invoke(prayerTime);
            }
        }

        private void TimeEnteredNotificationDone(object sender, EventArgs e)
        {
            PrayerTimeViewModel prayerTime = (PrayerTimeViewModel)sender;
            Log.Information($"Time entered and elapsed time done: {prayerTime.Key}");

            // time jumps happen when PC enter sleep/hibernate mode then wakeup after hours
            bool timeJumped = displayedDate != TimeStamp.Date;
            var nextDate = timeJumped ? TimeStamp.Date : TimeStamp.NextDate;
            Log.Information($"Time jumped? {timeJumped}");

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
                Log.Debug($"Time state changed: Key={prayerTime.Key}, State={prayerTime.State}");

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
            Log.Information("Start updating times");

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

            Log.Information("Updating times failed. Missing data");
            return false;
        }

        private void UpdateNext()
        {
            Log.Information($"Updating Next from: {next?.Key}");

            if (next != null)
            {
                next.IsNext = false;
            }

            // Sorting doesn't hurt here, we only have 5 items :)
            Next = Items.Where(i => !i.IsEntered && !i.IsShuruq && i.IsVisible).OrderBy(i => i.Countdown.TotalSeconds).FirstOrDefault();

            if (next != null)
            {
                next.IsNext = true;
            }

            Log.Information($"Updated Next time to: {next?.Key}");
        }

        private void UpdateDisplayedTime(bool skipLookup)
        {
            Log.Information($"Updating DisplayedTime from: {displayedTime?.Key}");

            if (displayedTime != null)
            {
                displayedTime.IsActive = false;
            }

            var time = skipLookup ? null : Items.SingleOrDefault(i => i.State == PrayerTimeState.EnteredRecently);

            // Check if we should display Shuruq time since it cannot be marked as "Next"
            if (time is null && next?.Key == nameof(PrayerTimes.Dhuhr))
            {
                DisplayedTime = Items.SingleOrDefault(i => i.State == PrayerTimeState.ShuruqComing && i.IsVisible) ?? next;
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

            Log.Information($"Updated DisplayedTime to: {displayedTime?.Key}");

            FixTimeJumpSideEffect();
        }

        private void FixTimeJumpSideEffect()
        {
            var needFix = Items.Where(i => i.IsEntered && i.State != PrayerTimeState.Entered && i != displayedTime);

            foreach (var item in needFix)
            {
                Log.Debug($"Fixing time jump effect for: {item.Key}");

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
            Log.Information("Data loaded");

            UpdateDisplayedLocation(response?.Location);

            return Update();
        }

        private bool SwitchToNextDay(Dictionary<DateTime, PrayerTimes> data)
        {
            if (DisplayedDate == TimeStamp.Date)
            {
                bool shouldSwitch = false;

                if (data.ContainsKey(displayedDate))
                {
                    var ishaConfig = WidgetSettings.Settings.GetPrayerConfig(nameof(PrayerTimes.Isha));
                    var ishaTime = data[displayedDate].Max(t => t.Value).AddMinutes(ishaConfig.Adjustment + ishaConfig.EffectiveElapsedTime());
                    // If Isha has already entered, then we look for the next day
                    shouldSwitch = TimeStamp.Now > ishaTime;
                }
                else
                {
                    shouldSwitch = data.ContainsKey(TimeStamp.NextDate);
                }
                
                if (shouldSwitch)
                {
                    Log.Information("Switching to next day");
                    DisplayedDate = TimeStamp.NextDate;

                    return true;
                }
            }

            return false;
        }

        private async Task RefreshData()
        {
            if (isRefreshing)
            {
                return;
            }

            try
            {
                Log.Information("Start refreshing data");
                ErrorMessage = null;
                IsRefreshing = true;

                IRequest request = BuildRequest(DisplayedDate, true);

                var apiResponse = await serviceClient.GetDataAsync(request);

                // We switch to next day when Isha has entered
                if (SwitchToNextDay(apiResponse.Times))
                {
                    if (TimeStamp.Date.Month != TimeStamp.NextDate.Month && serviceClient.SupportMonthlyData)
                    {
                        Log.Information("Fetching data for next month");
                        request = BuildRequest(TimeStamp.NextDate, true);
                        apiResponse = await serviceClient.GetDataAsync(request);
                    }
                }

                if (!serviceClient.SupportMonthlyData && apiResponse?.Times?.Count == 1)
                {
                    var availableDataTime = apiResponse.Times.Keys.Single();

                    // QCH service can't fetch fresh data until the QCH website update its content at midnight,
                    // so during the period between Isha and midnight we recycle the data of the actual day as if it was fresh.
                    if (WidgetSettings.Settings.Service == PrayerTimesService.QCH
                        && displayedDate == TimeStamp.NextDate
                        && availableDataTime == TimeStamp.Date)
                    {
                        var data = apiResponse.Times.Values.Single();
                        data.Adjust(displayedDate);
                        apiResponse.Times.Clear();
                        apiResponse.Times[displayedDate] = data;
                    }
                    else
                    {
                        DisplayedDate = availableDataTime;
                    }
                }

                OnDataLoaded(apiResponse);

                if (WidgetSettings.Settings.Service != PrayerTimesService.Local)
                {
                    // Cache the result for offline use, just in case
                    Log.Debug("Caching api response");
                    Properties.PersistentCache.Default.ApiData = JsonConvert.SerializeObject(apiResponse);
                    Properties.PersistentCache.Default.Save();
                }
            }
            catch (NetworkException nex)
            {
                Log.Error(nex, $"Refreshing data failed: {nex.Message}");

                if (!WidgetSettings.Settings.IsConfigured || latestData is null)
                {
                    ErrorMessage = nex.Message;
                }
                else if (!(latestData is null))
                {
                    // If the widget failed to get data for the next day, then we try to use the cache.
                    // If the next day is the start of a new month, then the cache won't help, so we show the error.
                    try
                    {
                        SwitchToNextDay(latestData);

                        if (!Update())
                        {
                            // Let's show the error message of the original exception
                            throw new Exception();
                        }
                    }
                    catch
                    {
                        ErrorMessage = nex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Refreshing data failed: {ex.Message}");

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
            finally
            {
                IsRefreshing = false;
                Log.Information("End refreshing data");
            }
        }

        private void OnNearNotificationStarted()
        {
            if (!isNotificationActive)
            {
                Log.Information($"Start notification for coming time: {next?.Key}");
                isNotificationActive = true;
                NearNotificationStarted?.Invoke();
            }
        }

        private void OnNearNotificationStopped()
        {
            if (isNotificationActive)
            {
                Log.Information($"Stop notification for coming time: {next?.Key}");
                isNotificationActive = false;
                NearNotificationStopped?.Invoke();
            }
        }

        private void UpdateDisplayedLocation(Location responseLocation)
        {
            string country = null;
            string city = null;

            if (WidgetSettings.Settings.Service == PrayerTimesService.SalahHour
                || WidgetSettings.Settings.Service == PrayerTimesService.QCH
                || WidgetSettings.Settings.Service == PrayerTimesService.WaktuSolat)
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
            Log.Debug($"Creating client for service: {WidgetSettings.Settings.Service}");

            switch (WidgetSettings.Settings.Service)
            {
                case PrayerTimesService.SalahHour:
                    serviceClient = new SalahHourClient();
                    break;
                case PrayerTimesService.AlAdhan:
                    serviceClient = new AlAdhanClient();
                    break;
                case PrayerTimesService.Local:
                    serviceClient = new LocalClient();
                    break;
                case PrayerTimesService.QCH:
                    serviceClient = new QchClient();
                    break;
                case PrayerTimesService.CSV:
                    serviceClient = new CsvClient();
                    break;
                case PrayerTimesService.WaktuSolat:
                    serviceClient = new WaktuSolatClient();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IRequest BuildRequest(DateTime date, bool getEntireMonth)
        {
            Log.Debug($"Building request for service: {WidgetSettings.Settings.Service}");
            RequestBase request;
            var settings = WidgetSettings.Settings;

            switch (settings.Service)
            {
                case PrayerTimesService.SalahHour:
                    request = new SalahHourRequest
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
                case PrayerTimesService.Local:
                    request = new LocalRequest();
                    break;
                case PrayerTimesService.QCH:
                    request = new QchRequest()
                    {
                        CityId = settings.QchCity,
                    };
                    break;
                case PrayerTimesService.CSV:
                    request = BuildCsvRequest();
                    break;
                case PrayerTimesService.WaktuSolat:
                    request = new WaktuSolatRequest();
                    break;
                default:
                    return null;
            }

            request.Date = date;
            request.GetEntireMonth = getEntireMonth && settings.Service != PrayerTimesService.QCH;
            request.Method = settings.CalculationMethod;
            request.JuristicSchool = settings.School;

            if (settings.LocationDetection != LocationDetectionMode.ByCountryCode)
            {
                request.Latitude = settings.Latitude;
                request.Longitude = settings.Longitude;
                request.UseCoordinates = true;
            }

            Log.Debug("Request built: {@request}", request);
            return request;
        }

        private CsvRequest BuildCsvRequest()
        {
            var settings = WidgetSettings.Settings;
            var map = new Dictionary<string, int>
            {
                [nameof(CsvTimes.Fajr)] = settings.CSV_Map_Fajr,
                [nameof(CsvTimes.Shuruq)] = settings.CSV_Map_Shuruq,
                [nameof(CsvTimes.Dhuhr)] = settings.CSV_Map_Dhuhr,
                [nameof(CsvTimes.Asr)] = settings.CSV_Map_Asr,
                [nameof(CsvTimes.Maghrib)] = settings.CSV_Map_Maghrib,
                [nameof(CsvTimes.Isha)] = settings.CSV_Map_Isha,
            };

            if (settings.CSV_HasDateColumn)
            {
                if (settings.CSV_DateColumnSchema == Configurations.CsvImportDateColumnSchema.Single)
                {
                    map.Add(nameof(CsvTimes.Date), settings.CSV_Map_Date);
                }
                else
                {
                    map.Add(nameof(CsvTimes.Day), settings.CSV_Map_Day);
                    map.Add(nameof(CsvTimes.Month), settings.CSV_Map_Month);
                }
            }

            return new CsvRequest
            {
                FilePath = settings.CSV_FilePath,
                HasHeader = settings.CSV_HasHeader,
                HasDateColumn = settings.CSV_HasDateColumn,
                Range = settings.CSV_Range,
                ColumnsMap = map,
            };
        }
    }
}