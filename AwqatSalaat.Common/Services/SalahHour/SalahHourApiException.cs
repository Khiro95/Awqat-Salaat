using System;

namespace AwqatSalaat.Services.SalahHour
{
    public class SalahHourApiException : Exception
    {
        public SalahHourApiException() : base() { }
        public SalahHourApiException(string message) : base(message) { }
    }
}