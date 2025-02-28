using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AwqatSalaat.Preview
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            LogManager.WidgetIdentity = $"Awqat Salaat Preview v{SettingsPanel.Version} ({SettingsPanel.Architecture})";

            LogManager.InvalidateLogger();
        }
    }
}
