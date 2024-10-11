using AwqatSalaat.Data;
using AwqatSalaat.Services.Methods;
using System;

namespace AwqatSalaat.Services
{
    public interface IRequest
    {
        CalculationMethod Method { get; }
        School JuristicSchool { get; }
        DateTime Date { get; }
        decimal Latitude { get; }
        decimal Longitude { get; }
        bool UseCoordinates { get; }
        bool GetEntireMonth { get; }
    }
}