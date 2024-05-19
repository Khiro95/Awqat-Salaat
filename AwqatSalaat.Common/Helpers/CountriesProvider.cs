using AwqatSalaat.Data;
using Newtonsoft.Json;
using System.IO;

namespace AwqatSalaat.Helpers
{
    public static class CountriesProvider
    {
        private static Country[] s_countries;

        public static Country[] GetCountries()
        {
            if (s_countries is null)
            {
                var asm = typeof(CountriesProvider).Assembly;
                using (Stream stream = asm.GetManifestResourceStream("AwqatSalaat.Assets.countries.json"))
                {
                    s_countries = DeserializeFromStream<Country[]>(stream);
                }
            }

            return s_countries;
        }

        private static T DeserializeFromStream<T>(Stream stream)
        {
            var serializer = new JsonSerializer();

            using (var reader = new StreamReader(stream))
            {
                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }
    }
}
