using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class TurkeyMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.DIB;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.DIB;

        public TurkeyMethod() : base("TURKEY", "Diyanet İşleri Başkanlığı, Turkey") { }
    }
}
