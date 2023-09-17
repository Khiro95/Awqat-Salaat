using AwqatSalaat.UI.Views;
using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AwqatSalaat
{
    [ComVisible(true)]
    [Guid("5F3E38A1-34C1-4A48-9B53-C15241BF1C6F")]
    [CSDeskBandRegistration(Name = WidgetName, ShowDeskBand = true)]
    public class AwqatSalaatWidget : CSDeskBandWpf
    {
        private const string WidgetName = "Awqat Salaat";
        public static AwqatSalaatWidget Instance { get; private set; }
        public AwqatSalaatWidget()
        {
            Options.MinHorizontalSize = new Size(100, 40);
            Instance = this;
            System.Windows.Threading.Dispatcher.CurrentDispatcher.UnhandledException += (s, e) =>
            {
                try
                {
                    MessageBox.Show(e.Exception.Message + '\n' + e.Exception.InnerException?.Message, WidgetName);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    throw;
                }
            };
        }

        protected override UIElement UIElement => new WidgetSummary(); // Return the main wpf control
    }
}
