using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class IsnaMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 15);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.ISNA;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.ISNA;

        public IsnaMethod() : base("ISNA", "Islamic Society of North America (ISNA)") { }
    }
}
