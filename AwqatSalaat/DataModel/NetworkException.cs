using System;

namespace AwqatSalaat.DataModel
{
    public class NetworkException : Exception
    {
        public NetworkException() : base() { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

}
