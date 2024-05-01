using AwqatSalaat.Services.Methods;
using System;
using System.Web;

namespace AwqatSalaat.Services.IslamicFinder
{
    public class IslamicFinderRequest : IRequest
    {
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }
        public CalculationMethod Method { get; set; }
        public bool GetEntireMonth { get; set; }
        public DateTime Date { get; set; }

        public string GetUrl()
        {
            // HttpUtility.ParseQueryString method actually returns an internal HttpValueCollection object
            // rather than a regular NameValueCollection
            var query = HttpUtility.ParseQueryString("");
            query["country"] = CountryCode;
            query["zipcode"] = ZipCode;
            query["show_entire_month"] = GetEntireMonth.ToString();
            query["date"] = Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            query["time_format"] = "0";

            if (Method is IIslamicFinderMethod method)
            {
                query["method"] = method.IslamicFinderMethod.ToString("D");
            }
            else
            {
                query["method"] = "6";
                query["fajir_angle"] = Method.Fajr.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                query["maghrib_rule"] = Method.Maghrib.Type.ToString("D");
                query["maghrib_value"] = Method.Maghrib.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                query["isha_rule"] = Method.Isha.Type.ToString("D");
                query["isha_value"] = Method.Isha.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            }

            return $"http://www.islamicfinder.us/index.php/api/prayer_times?{query}";
        }
    }
}
