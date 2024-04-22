using System;

namespace AwqatSalaat.Services.AlAdhan
{
    internal class AlAdhanApiException : Exception
    {
        public AlAdhanApiException(string message) : base(message) { }
    }
}