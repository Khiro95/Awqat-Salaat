using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class IsnaMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.ISNA;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.ISNA;

        public IsnaMethod() : base("ISNA", "Islamic Society of North America (ISNA)") { }
    }
}
