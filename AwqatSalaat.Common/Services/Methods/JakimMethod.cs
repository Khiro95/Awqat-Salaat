using AwqatSalaat.Services.AlAdhan;

namespace AwqatSalaat.Services.Methods
{
    internal class JakimMethod : CalculationMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 20);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.JAKIM;

        public JakimMethod() : base("JAKIM", "Jabatan Kemajuan Islam Malaysia (JAKIM)") { }
    }
}
