using AwqatSalaat.Services.Methods;
using System;

namespace AwqatSalaat.Services
{
    public interface IRequest
    {
        CalculationMethod Method { get; }
        DateTime Date { get; }
        bool GetEntireMonth { get; }

        string GetUrl();
    }
}