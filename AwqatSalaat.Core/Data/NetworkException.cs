using System;

namespace AwqatSalaat.Data
{
    public class NetworkException : Exception
    {
        public NetworkException() : base() { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

}
