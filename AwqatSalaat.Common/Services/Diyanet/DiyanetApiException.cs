using System;

namespace AwqatSalaat.Services.Diyanet
{
    public class DiyanetApiException : Exception
    {
        public DiyanetApiException() : base() { }
        public DiyanetApiException(string message) : base(message) { }
    }
}
