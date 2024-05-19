using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class RussiaMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 16);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.RusCustom;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.SAMR;

        public RussiaMethod() : base("RUSSIA", "Spiritual Administration of Muslims of Russia") { }
    }
}
