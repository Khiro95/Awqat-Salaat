using System;

namespace AwqatSalaat.Services.IslamicFinder
{
    public class IslamicFinderApiException : Exception
    {
        public IslamicFinderApiException() : base() { }
        public IslamicFinderApiException(string message) : base(message) { }
    }
}