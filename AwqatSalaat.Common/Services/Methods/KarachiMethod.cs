using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class KarachiMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.Karachi;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Karachi;

        public KarachiMethod() : base("KARACHI", "University of Islamic Sciences, Karachi") { }
    }
}
