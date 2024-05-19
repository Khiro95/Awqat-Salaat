using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class PortugalMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Maghrib { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 3);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 77);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.PORTUGAL;

        public PortugalMethod() : base("PORTUGAL", "Comunidade Islamica de Lisboa") { }
    }
}
