namespace AwqatSalaat.Services.Diyanet
{
    /// <summary>
    /// Request for the official Diyanet (Turkey) prayer times, served without authentication by
    /// https://ezanvakti.emushaf.net. The location is a Diyanet district (ilçe) id, e.g. 11600 for Graz.
    /// </summary>
    public class DiyanetRequest : WebRequestBase
    {
        public const string BaseUrl = "https://ezanvakti.emushaf.net";

        public int DistrictId { get; set; }

        // Only used for the displayed location, the API doesn't return names
        public string CountryName { get; set; }
        public string DistrictName { get; set; }

        // The API always returns about a month of days starting a few days before today
        public override string GetUrl() => $"{BaseUrl}/vakitler/{DistrictId}";
    }
}
