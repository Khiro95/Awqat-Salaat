using System.Globalization;
using System.Web;

namespace AwqatSalaat.Services.SalahHour
{
    public class SalahHourRequest : WebRequestBase
    {
        public string CountryCode { get; set; }
        public string ZipCode { get; set; }
        public string TimeZone { get; set; }

        public override string GetUrl()
        {
            // HttpUtility.ParseQueryString method actually returns an internal HttpValueCollection object
            // rather than a regular NameValueCollection
            var query = HttpUtility.ParseQueryString("");
            // Salah Hour returns the whole month whenever show_entire_month is present, even when it is False
            if (GetEntireMonth)
            {
                query["show_entire_month"] = "True";
            }

            query["date"] = Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            query["time_format"] = "0";
            query["juristic"] = JuristicSchool.ToString("D");

            if (HighLatitudeMethod != Data.HighLatitudeMethod.Default)
            {
                // Salah Hour's high_latitude values match the enum (1 midnight, 2 one seventh, 3 angle)
                query["high_latitude"] = HighLatitudeMethod.ToString("D");
            }

            if (UseCoordinates)
            {
                query["latitude"] = Latitude.ToString(CultureInfo.InvariantCulture);
                query["longitude"] = Longitude.ToString(CultureInfo.InvariantCulture);
                query["timezone"] = TimeZone;
            }
            else
            {
                query["country"] = CountryCode;
                query["zipcode"] = ZipCode;
            }

            if (Method is ISalahHourMethod method)
            {
                query["method"] = method.SalahHourMethod.ToString("D");
            }
            else
            {
                query["method"] = "6";
                query["fajir_angle"] = Method.Fajr.Value.ToString("F1", CultureInfo.InvariantCulture);
                query["maghrib_rule"] = Method.Maghrib.Type.ToString("D");
                query["maghrib_value"] = Method.Maghrib.Value.ToString("F1", CultureInfo.InvariantCulture);
                query["isha_rule"] = Method.Isha.Type.ToString("D");
                query["isha_value"] = Method.Isha.Value.ToString("F1", CultureInfo.InvariantCulture);
            }

            return $"https://www.salahhour.com/api/prayer_times?{query}";
        }
    }
}
