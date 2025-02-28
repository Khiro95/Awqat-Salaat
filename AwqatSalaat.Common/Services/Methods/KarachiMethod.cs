using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class KarachiMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.Karachi;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Karachi;

        public KarachiMethod() : base("KARACHI", "University of Islamic Sciences, Karachi") { }
    }
}
