using System;

namespace AwqatSalaat.DataModel
{
    public interface IRequest
    {
        DateTime Date { get; }
        bool GetEntireMonth { get; }

        string GetUrl();
    }
}