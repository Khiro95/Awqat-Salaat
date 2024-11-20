using System.IO;
using System.Reflection;

namespace AwqatSalaat.Helpers
{
    public static class CommonResources
    {
        public static Stream Get(string file)
        {
            Assembly assembly = typeof(CommonResources).Assembly;
            var name = "AwqatSalaat.Assets." + file;
            return assembly.GetManifestResourceStream(name);
        }
    }
}
