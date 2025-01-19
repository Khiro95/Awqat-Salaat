using AwqatSalaat.Data;

namespace AwqatSalaat.Services.SalahHour
{
    public class Settings
    {
        public SalahHourMethod Method { get; set; }
        public string TimeZone { get; set; }
        public Location Location { get; set; }
    }
}
