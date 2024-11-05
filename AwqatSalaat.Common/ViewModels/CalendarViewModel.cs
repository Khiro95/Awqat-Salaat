using AwqatSalaat.Data;
using AwqatSalaat.Helpers;
using AwqatSalaat.Services;
using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;
using AwqatSalaat.Services.Local;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AwqatSalaat.ViewModels
{
    public class CalendarViewModel : ObservableObject
    {
        private int gregorianYear;
        private int gregorianMonth;
        private int hijriYear;
        private int hijriMonth;
        private bool useGregorianCalendar = true;
        private bool useHijriCalendar;
        private bool isBusy;

        public int GregorianYear { get => gregorianYear; set => SetProperty(ref gregorianYear, value); }
        public int GregorianMonth { get => gregorianMonth; set => SetProperty(ref gregorianMonth, value); }
        public int HijriYear { get => hijriYear; set => SetProperty(ref hijriYear, value); }
        public int HijriMonth { get => hijriMonth; set => SetProperty(ref hijriMonth, value); }
        public bool UseGregorianCalendar { get => useGregorianCalendar; set => SetProperty(ref useGregorianCalendar, value); }
        public bool UseHijriCalendar { get => useHijriCalendar; set => SetProperty(ref useHijriCalendar, value); }
        public bool IsBusy { get => isBusy; set => SetProperty(ref isBusy, value); }
        public ObservableCollection<CalendarRecord> PrayerTimes { get; } = new ObservableCollection<CalendarRecord>();
        public bool HasData => PrayerTimes?.Count > 0;
        public ICommand Refresh { get; }

        private Properties.Settings Settings => Properties.Settings.Default;

        public CalendarViewModel()
        {
            Refresh = new RelayCommand(RefreshExecute);
            gregorianYear = TimeStamp.Date.Year;
            gregorianMonth = TimeStamp.Date.Month;
            var cal = GetHijriCalendar();
            hijriYear = cal.GetYear(TimeStamp.Date);
            hijriMonth = cal.GetMonth(TimeStamp.Date);
        }

        private async void RefreshExecute(object obj)
        {
            try
            {
                IsBusy = true;

                PopulateData(null);

                IServiceClient serviceClient = GetServiceClient(Settings.Service);
                DateTime dateTime;
                Calendar calendar = null;

                if (useGregorianCalendar)
                {
                    dateTime = new DateTime(gregorianYear, gregorianMonth, 1);
                }
                else
                {
                    calendar = GetHijriCalendar();
                    dateTime = calendar.ToDateTime(hijriYear, hijriMonth, 1, 0, 0, 0, 0, calendar.Eras[0]);
                }

                var request = BuildRequest(dateTime);

                var result = await serviceClient.GetDataAsync(request);
                var times = result?.Times;

                if (UseHijriCalendar && times?.Count > 0)
                {
                    var min = times.Keys.Min();

                    if (min > dateTime)
                    {
                        await AddMissingNegative(serviceClient, times, dateTime, (min - dateTime).Days);
                    }
                    else if (dateTime > min)
                    {
                        await AddMissingPositive(serviceClient, times, dateTime);
                    }

                    times = FilterHijriRecords(times, HijriMonth, calendar);
                }

                PopulateData(times);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddMissingNegative(IServiceClient serviceClient, Dictionary<DateTime, PrayerTimes> data, DateTime dateTime, int count)
        {
            var monthOffset = HijriMonth == 1 ? 11 : -1;
            var yearOffset = HijriMonth == 1 ? -1 : 0;
            var request = BuildRequest(dateTime.AddMonths(-1), monthOffset, yearOffset);

            var result = await serviceClient.GetDataAsync(request);

            foreach (var time in result.Times.Where(kv => kv.Key <= dateTime).OrderByDescending(kv => kv.Key).Take(count))
            {
                data.Add(time.Key, time.Value);
            }
        }

        private async Task AddMissingPositive(IServiceClient serviceClient, Dictionary<DateTime, PrayerTimes> data, DateTime dateTime)
        {
            var last = data.Keys.Last();
            var monthOffset = HijriMonth == 12 ? -11 : 1;
            var yearOffset = HijriMonth == 12 ? 1 : 0;
            var request = BuildRequest(dateTime.AddMonths(1), monthOffset, yearOffset);

            var result = await serviceClient.GetDataAsync(request);

            foreach (var time in result.Times.Where(kv => kv.Key > last))
            {
                data.Add(time.Key, time.Value);
            }
        }

        private Dictionary<DateTime, PrayerTimes> FilterHijriRecords(Dictionary<DateTime, PrayerTimes> data, int month, Calendar calendar)
        {
            return data.Where(kv => calendar.GetMonth(kv.Key) == month).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private void PopulateData(Dictionary<DateTime, PrayerTimes> data)
        {
            if (HasData)
            {
                PrayerTimes.Clear();
            }

            if (data?.Count > 0)
            {
                foreach (var time in data.OrderBy(kv => kv.Key))
                {
                    var record = new CalendarRecord(time.Key, time.Value);
                    PrayerTimes.Add(record);
                }
            }

            OnPropertyChanged(nameof(HasData));
        }

        private IServiceClient GetServiceClient(PrayerTimesService service)
        {
            switch (service)
            {
                case PrayerTimesService.IslamicFinder:
                    return new IslamicFinderClient();
                case PrayerTimesService.AlAdhan:
                    return new AlAdhanClient();
                case PrayerTimesService.Local:
                    return new LocalClient();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IRequest BuildRequest(DateTime date, int hijriMonthOffset = 0, int hijriYearOffset = 0)
        {
            RequestBase request;

            switch (Settings.Service)
            {
                case PrayerTimesService.IslamicFinder:
                    request = new IslamicFinderRequest
                    {
                        CountryCode = Settings.CountryCode,
                        ZipCode = Settings.ZipCode,
                        TimeZone = Settings.LocationDetection != LocationDetectionMode.ByCountryCode ? TimeZoneHelper.GetIanaTimeZone() : null,
                    };
                    break;
                case PrayerTimesService.AlAdhan:
                    request = new AlAdhanRequest
                    {
                        Country = Settings.CountryCode,
                        City = Settings.City,
                    };
                    break;
                case PrayerTimesService.Local:
                    request = new LocalRequest()
                    {
                        HijriCalendar = GetHijriCalendar()
                    };
                    break;
                default:
                    return null;
            }

            request.Date = date;
            request.GetEntireMonth = true;
            request.UseHijri = UseHijriCalendar;
            request.HijriYear = HijriYear + hijriYearOffset;
            request.HijriMonth = HijriMonth + hijriMonthOffset;
            request.Method = Settings.CalculationMethod;
            request.JuristicSchool = Settings.School;

            if (Settings.LocationDetection != LocationDetectionMode.ByCountryCode)
            {
                request.Latitude = Settings.Latitude;
                request.Longitude = Settings.Longitude;
                request.UseCoordinates = true;
            }

            return request;
        }

        public Calendar GetHijriCalendar()
        {
            if (Settings.CalendarType == CalendarType.UmAlQura)
            {
                return Calendar.ReadOnly(new UmAlQuraCalendar());
            }
            else
            {
                HijriCalendar hijriCalendar = new HijriCalendar();
                hijriCalendar.HijriAdjustment = Settings.HijriAdjustment;
                return Calendar.ReadOnly(hijriCalendar);
            }
        }
    }

    public class CalendarRecord
    {
        public DateTime Date { get; }
        public PrayerTimes Times { get; }

        public CalendarRecord(DateTime date, PrayerTimes times)
        {
            Date = date;
            Times = times;
        }
    }
}
