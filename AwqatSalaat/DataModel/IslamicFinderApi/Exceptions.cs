using System;

namespace AwqatSalaat.DataModel.IslamicFinderApi
{
    public class IslamicFinderApiException : Exception
    {
        public IslamicFinderApiException() : base() { }
        public IslamicFinderApiException(string message) : base(message) { }
    }
}
