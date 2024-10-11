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
                ? $"calendar{endpointSuffix}/{Date.Year}/{Date.Month}"
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