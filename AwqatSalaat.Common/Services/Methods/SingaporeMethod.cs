using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class SingaporeMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 20);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Singapore;

        public SingaporeMethod() : base("SINGAPORE", "Majlis Ugama Islam Singapura, Singapore") { }
    }
}
