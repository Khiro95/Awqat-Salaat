using System;

namespace AwqatSalaat.Services.Nominatim
{
    public class NominatimException : Exception
    {
        public NominatimException(string message) : base(message) { }
    }
}
