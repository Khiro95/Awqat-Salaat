using System;

namespace AwqatSalaat.DataModel.IslamicFinderApi
{
    public class NetworkException : Exception
    {
        public NetworkException() : base() { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class IslamicFinderApiException : Exception
    {
        public IslamicFinderApiException() : base() { }
        public IslamicFinderApiException(string message) : base(message) { }
    }
}
