using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class TunisiaMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.TUNISIA;

        public TunisiaMethod() : base("TUNISIA", "Tunisia") { }
    }
}
