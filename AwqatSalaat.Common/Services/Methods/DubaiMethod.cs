using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class DubaiMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18.2f);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18.2f);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Dubai;

        public DubaiMethod() : base("DUBAI", "Dubai") { }
    }
}
