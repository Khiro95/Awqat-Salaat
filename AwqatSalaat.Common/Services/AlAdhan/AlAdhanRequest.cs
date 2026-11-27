using AwqatSalaat.Services.Methods;
using System.Globalization;
using System.Web;

namespace AwqatSalaat.Services.AlAdhan
{
    public class AlAdhanRequest : WebRequestBase
    {
        public string Country { get; set; }
        public string City { get; set; }

        public override string GetUrl()
        {
            string endpointSuffix = "";
            // HttpUtility.ParseQueryString method actually returns an internal HttpValueCollection object
            // rather than a regular NameValueCollection
            var query = HttpUtility.ParseQueryString("");
            query["school"] = JuristicSchool.ToString("D");
            // Times come with their UTC offset (e.g. 2026-11-20T05:12:00+05:00), so they don't depend
            // on mapping the response time zone to a Windows time zone (see issue #56)
            query["iso8601"] = "true";

            if (UseCoordinates)
            {
                query["latitude"] = Latitude.ToString(CultureInfo.InvariantCulture);
                query["longitude"] = Longitude.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                query["country"] = Country;
                query["city"] = City;
                endpointSuffix = "ByCity";
            }

            if (Method is IAlAdhanMethod method)
            {
                query["method"] = method.AlAdhanMethod.ToString("D");
            }
            else
            {
                query["method"] = "99";
                string[] parameters =
                {
                    SerializeMethodParameter(Method.Fajr),
                    SerializeMethodParameter(Method.Maghrib),
                    SerializeMethodParameter(Method.Isha),
                };
                query["methodSettings"] = string.Join(",", parameters);
            }

            var endpointParameters = GetEntireMonth
                ? UseHijri
                    ? $"hijriCalendar{endpointSuffix}/{HijriYear}/{HijriMonth}"
                    : $"calendar{endpointSuffix}/{Date.Year}/{Date.Month}"
                : $"timings{endpointSuffix}/{Date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}";

            return $"https://api.aladhan.com/v1/{endpointParameters}?{query}";
        }

        private static string SerializeMethodParameter(CalculationMethodParameter methodParameter)
        {
            string str = methodParameter.Value.ToString("F1", CultureInfo.InvariantCulture);

            if (methodParameter.Type == CalculationMethodParameterType.FixedMinutes)
            {
                str += " min";
            }

            return str;
        }
    }
}