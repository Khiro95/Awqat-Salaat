using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class MwlMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.MWL;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.MWL;

        public MwlMethod() : base("MWL", "Muslim World League") { }
    }
}
