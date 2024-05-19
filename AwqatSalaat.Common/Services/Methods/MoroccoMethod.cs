using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class MoroccoMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 19);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.MOROCCO;

        public MoroccoMethod() : base("MOROCCO", "Morocco") { }
    }
}
