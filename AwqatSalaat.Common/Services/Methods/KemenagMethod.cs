using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class KemenagMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 20);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.SIHRI;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.KEMENAG;

        public KemenagMethod() : base("KEMENAG", "Kementerian Agama Republik Indonesia") { }
    }
}
