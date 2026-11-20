using System.Globalization;

namespace AwqatSalaat.Services.WaktuSolat
{
    /// <summary>
    /// Request for the Waktu Solat API (https://api.waktusolat.app), which serves the official
    /// JAKIM prayer times for Malaysia. The zone is resolved by the API from the coordinates.
    /// </summary>
    public class WaktuSolatRequest : WebRequestBase
    {
        public const string BaseUrl = "https://api.waktusolat.app";

        public override string GetUrl()
        {
            var lat = Latitude.ToString(CultureInfo.InvariantCulture);
            var lng = Longitude.ToString(CultureInfo.InvariantCulture);

            // The API always returns a whole Gregorian month
            return $"{BaseUrl}/v2/solat/gps/{lat}/{lng}?year={Date.Year}&month={Date.Month}";
        }
    }
}
