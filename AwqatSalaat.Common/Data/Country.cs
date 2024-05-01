using Newtonsoft.Json;

namespace AwqatSalaat.Data
{
    public class Country
    {
        public string Name { get; }
        public string Code { get; }

        [JsonConstructor]
        private Country(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }
}
