using AwqatSalaat.Data;

namespace AwqatSalaat.Services.IslamicFinder
{
    internal class Settings
    {
        public IslamicFinderMethod Method { get; set; }
        public string TimeZone { get; set; }
        public Location Location { get; set; }
    }
}
