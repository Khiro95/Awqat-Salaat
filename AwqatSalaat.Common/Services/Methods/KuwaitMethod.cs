using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class KuwaitMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17.5f);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Kuwait;

        public KuwaitMethod() : base("KUWAIT", "Kuwait") { }
    }
}
