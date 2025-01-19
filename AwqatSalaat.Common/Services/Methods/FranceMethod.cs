using AwqatSalaat.Services.AlAdhan;
using AwqatSalaat.Services.SalahHour;

namespace AwqatSalaat.Services.Methods
{
    internal class FranceMethod : CalculationMethod, ISalahHourMethod, IAlAdhanMethod
    {
        public override CalculationMethodParameter Fajr { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 12);
        public override CalculationMethodParameter Isha { get; } = new CalculationMethodParameter(CalculationMethodParameterType.Angle, 12);

        public SalahHourMethod SalahHourMethod => SalahHourMethod.UOIF;
        public AlAdhanMethod AlAdhanMethod => AlAdhanMethod.UOIF;

        public FranceMethod() : base("FRANCE", "Union Des Organisations Islamiques De France") { }
    }
}
