using System;

namespace AwqatSalaat.Services.AlAdhan
{
    public class AlAdhanRequest : IRequest
    {
        public DateTime Date { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public AlAdhanMethod Method { get; set; }
        public bool GetEntireMonth { get; set; }

        public string GetUrl()
            => (GetEntireMonth
                ? $"http://api.aladhan.com/v1/calendarByCity/{Date.Year}/{Date.Month}"
                : $"http://api.aladhan.com/v1/timingsByCity/{Date:dd-MM-yyyy}")
            + $"?country={Country}"
            + $"&city={City}"
            + $"&method={(byte)Method}";
    }
}