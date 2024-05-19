using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.IslamicFinder;

namespace AwqatSalaat.Services.Methods
{
    internal class MakkahMethod : CalculationMethod, IIslamicFinderMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18.5f);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 90);

        public IslamicFinderMethod IslamicFinderMethod => IslamicFinderMethod.UAQ;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Makkah;

        public MakkahMethod() : base("MAKKAH", "Umm Al-Qura University, Makkah") { }
    }
}
