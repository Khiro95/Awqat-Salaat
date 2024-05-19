using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class EgyptMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 19.5f);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17.5f);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.EGAS;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.EGAS;

        public EgyptMethod() : base("EGYPT", "Egyptian General Authority of Survey") { }
    }
}
