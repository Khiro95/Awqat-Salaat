using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class QatarMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 90);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Qatar;

        public QatarMethod() : base("QATAR", "Qatar") { }
    }
}
