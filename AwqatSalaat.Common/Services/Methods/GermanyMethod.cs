using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class GermanyMethod : CalculationMethod, IIslamicFinderMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 16.5f);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.GerCustom;

        public GermanyMethod() : base("GERMANY", "Germany") { }
    }
}
