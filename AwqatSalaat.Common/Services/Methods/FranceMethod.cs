using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class FranceMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 12);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 12);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.UOIF;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.UOIF;

        public FranceMethod() : base("FRANCE", "Union Des Organisations Islamiques De France") { }
    }
}
