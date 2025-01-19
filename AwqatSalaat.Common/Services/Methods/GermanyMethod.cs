using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class GermanyMethod : CalculationMethod, ISalahHourMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 18);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 16.5f);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.GerCustom;

        public GermanyMethod() : base("GERMANY", "Germany") { }
    }
}
