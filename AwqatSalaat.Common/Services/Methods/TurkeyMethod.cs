using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class TurkeyMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.DIB;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.DIB;

        public TurkeyMethod() : base("TURKEY", "Diyanet İşleri Başkanlığı, Turkey") { }
    }
}
