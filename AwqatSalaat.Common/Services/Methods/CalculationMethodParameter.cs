namespace AwqatSalaat.Services.Methods
{
    public class CalculationMethodParameter
    {
        public CalculationMethodParameterType Type { get; }
        public float Value { get; }

        public CalculationMethodParameter(CalculationMethodParameterType type, float value)
        {
            Type = type;
            Value = value;
        }
    }
}
