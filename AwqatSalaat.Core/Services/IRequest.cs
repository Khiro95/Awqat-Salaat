using System;

namespace AwqatSalaat.Services
{
    public interface IRequest
    {
        DateTime Date { get; }
        bool GetEntireMonth { get; }

        string GetUrl();
    }
}