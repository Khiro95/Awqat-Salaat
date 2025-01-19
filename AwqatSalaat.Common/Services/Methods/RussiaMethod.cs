using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class RussiaMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 16);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.RusCustom;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.SAMR;

        public RussiaMethod() : base("RUSSIA", "Spiritual Administration of Muslims of Russia") { }
    }
}
