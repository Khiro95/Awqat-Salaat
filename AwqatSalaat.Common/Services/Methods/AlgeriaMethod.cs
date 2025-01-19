using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class AlgeriaMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 17);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.AMRAW;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.ALGERIA;

        public AlgeriaMethod() : base("ALGERIA", "Algerian Minister of Religious Affairs and Wakfs") { }
    }
}
