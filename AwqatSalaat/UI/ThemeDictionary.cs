using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AwqatSalaat.UI
{
    public class ThemeDictionary : ResourceDictionary
    {
        private static readonly Dictionary<ThemeKey, Uri> Sources = new Dictionary<ThemeKey, Uri>()
        {
            [ThemeKey.Dark] = new Uri("/AwqatSalaat;component/UI/Themes/Dark.xaml", UriKind.RelativeOrAbsolute),
            [ThemeKey.Light] = new Uri("/AwqatSalaat;component/UI/Themes/Light.xaml", UriKind.RelativeOrAbsolute)
        };
        private static readonly Uri StylesUri = new Uri("/AwqatSalaat;component/UI/Themes/Styles.xaml", UriKind.RelativeOrAbsolute);

        public ThemeDictionary() : base()
        {
            Source = Sources[ThemeManager.Current];
            MergedDictionaries.Add(new ResourceDictionary() { Source = StylesUri });
            ThemeManager.Changed += ThemeSource_Changed;
        }

        private void ThemeSource_Changed()
        {
            Source = Sources[ThemeManager.Current];
            MergedDictionaries.Add(new ResourceDictionary() { Source = StylesUri });
        }

        ~ThemeDictionary()
        {
            ThemeManager.Changed -= ThemeSource_Changed;
        }
    }
}
