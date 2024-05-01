using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class MwlMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.MWL;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.MWL;

        public MwlMethod() : base("MWL", "Muslim World League") { }
    }
}
