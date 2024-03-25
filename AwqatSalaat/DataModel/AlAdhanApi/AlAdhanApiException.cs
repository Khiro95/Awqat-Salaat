using System;

namespace AwqatSalaat.DataModel.AlAdhanApi
{
    internal class AlAdhanApiException : Exception
    {
        public AlAdhanApiException(string message) : base(message) { }
    }
}