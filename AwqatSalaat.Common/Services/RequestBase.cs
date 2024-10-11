using AwqatSalaat.Data;
using AwqatSalaat.Services.Methods;
using System;

namespace AwqatSalaat.Services
{
    public abstract class RequestBase : IRequest
    {
        public DateTime Date { get; set; }
        public CalculationMethod Method { get; set; }
        public School JuristicSchool { get; set; }
        public bool GetEntireMonth { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool UseCoordinates { get; set; }
    }
}
