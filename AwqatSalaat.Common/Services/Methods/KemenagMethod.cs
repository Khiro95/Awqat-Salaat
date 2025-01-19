using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class KemenagMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 20);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.SIHRI;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.KEMENAG;

        public KemenagMethod() : base("KEMENAG", "Kementerian Agama Republik Indonesia") { }
    }
}
