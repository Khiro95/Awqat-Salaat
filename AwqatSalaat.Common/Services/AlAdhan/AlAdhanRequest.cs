using AwqatSalaat.Services.Methods;
using System;
using System.Web;

namespace AwqatSalaat.Services.AlAdhan
{
    public class AlAdhanRequest : IRequest
    {
        public DateTime Date { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public CalculationMethod Method { get; set; }
        public bool GetEntireMonth { get; set; }

        public string GetUrl()
        {
            // HttpUtility.ParseQueryString method actually returns an internal HttpValueCollection object
            // rather than a regular NameValueCollection
            var query = HttpUtility.ParseQueryString("");
            query["country"] = Country;
            query["city"] = City;

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

            var endpoint = GetEntireMonth
                ? $"http://api.aladhan.com/v1/calendarByCity/{Date.Year}/{Date.Month}"
                : $"http://api.aladhan.com/v1/timingsByCity/{Date.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)}";

            return $"{endpoint}?{query}";
        }

        private static string SerializeMethodParameter(CalculationMethodParameter methodParameter)
        {
            string str = methodParameter.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

            if (methodParameter.Type == CalculationMethodParameterType.FixedMinutes)
            {
                str += " min";
            }

            return str;
        }
    }
}