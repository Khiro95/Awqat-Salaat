using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AwqatSalaat.Helpers
{
    internal static class CommonResourcesHelper
    {
        public static ImageSource GetImage(string file)
        {
            using (var stream = CommonResources.Get(file))
            {
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }
    }
}
