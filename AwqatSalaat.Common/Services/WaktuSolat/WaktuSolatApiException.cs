using System;

namespace AwqatSalaat.Services.WaktuSolat
{
    public class WaktuSolatApiException : Exception
    {
        public WaktuSolatApiException() : base() { }
        public WaktuSolatApiException(string message) : base(message) { }
    }
}
