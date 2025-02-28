using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class GulfMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 19.5f);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.FixedMinutes, 90);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.Gulf90;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.Gulf;

        public GulfMethod() : base("GULF", "Gulf Region") { }
    }
}
