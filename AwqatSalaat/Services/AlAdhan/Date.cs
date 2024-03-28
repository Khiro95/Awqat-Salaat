using System;

namespace AwqatSalaat.Services.AlAdhan
{
    internal class Date
    {
        public string Readable { get; set; }
        public string Timestamp { get; set; }
        public DateInfo Gregorian { get; set; }
        public DateInfo Hijri { get; set; }

        public DateTime ToDateTime()
            => DateTime.ParseExact(
                Gregorian.Date,
                Gregorian.Format.Replace('D', 'd').Replace('Y', 'y'),
                System.Globalization.CultureInfo.InvariantCulture);
    }

    internal class DateInfo
    {
        public string Date { get; set; }
        public string Format { get; set; }
    }
}
