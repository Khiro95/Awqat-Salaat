using System;

namespace AwqatSalaat.Services.IslamicFinder
{
    public class IslamicFinderRequest : IRequest
    {
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }
        public IslamicFinderMethod Method { get; set; }
        public bool GetEntireMonth { get; set; }
        public DateTime Date { get; set; }

        public string GetUrl()
            => "http://www.islamicfinder.us/index.php/api/prayer_times"
            + $"?country={CountryCode}"
            + $"&zipcode={ZipCode}"
            + $"&method={(byte)Method}"
            + $"&show_entire_month={GetEntireMonth}"
            + $"&date={Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}"
            + $"&time_format=0";
    }
}
