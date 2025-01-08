using AwqatSalaat.Helpers;

namespace AwqatSalaat.ViewModels
{
    public class CalendarExportViewModel : ObservableObject
    {
        private CalendarResult calendarResult;
        private CalendarDocumentPalette palette = CalendarPalettes.Palettes[0];
        private string location;

        public CalendarResult CalendarResult { get => calendarResult; set => SetProperty(ref calendarResult, value); }
        public CalendarDocumentPalette Palette { get => palette; set => SetProperty(ref palette, value); }
        public string Location { get => location; set => SetProperty(ref location, value); }
    }
}
