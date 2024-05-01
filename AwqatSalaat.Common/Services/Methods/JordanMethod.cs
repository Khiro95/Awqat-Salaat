using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class JordanMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Maghrib { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 5);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.JORDAN;

        public JordanMethod() : base("JORDAN", "Ministry of Awqaf, Islamic Affairs and Holy Places, Jordan") { }
    }
}
