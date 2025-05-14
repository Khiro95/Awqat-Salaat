using AwqatSalaat.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for LearnView.xaml
    /// </summary>
    public partial class LearnView : UserControl
    {
        private static readonly FontFamily UthmanicHafs;
        private static readonly FontFamily UthmanTahaNaskh;
        private static readonly FontFamily TimesNewRoman;

        private static readonly IReadOnlyList<string> SupportedLocales = new List<string> { "ar", "en" }.AsReadOnly();

        private const string ImageToken = "!!IMG";
        private const string QuranToken = "!!QURAN";
        private const string HadithToken = "!!HADITH";

        static LearnView()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var baseDir = Path.GetDirectoryName(assemblyPath) + "\\";

            UthmanicHafs = new FontFamily(new Uri(baseDir), "Fonts/#KFGQPC HAFS Uthmanic Script");
            UthmanTahaNaskh = new FontFamily(new Uri(baseDir), "Fonts/#KFGQPC Uthman Taha Naskh");
            TimesNewRoman = new FontFamily("Times New Roman");
        }

        private string lastLocale;

        public LearnView()
        {
            InitializeComponent();

            Loaded += LearnView_Loaded;
            Unloaded += LearnView_Unloaded;
        }

        private void LearnView_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Learn page loaded");
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            if (lastLocale != LocaleManager.Default.Current)
            {
                LoadRtf();
            }
        }

        private void LearnView_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Learn page unloaded");
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            LoadRtf();
        }

        private void LoadRtf()
        {
            Log.Information("Loading rtf file");
            lastLocale = LocaleManager.Default.Current;
            flowDoc.Blocks.Clear();

            using (var stream = CommonResources.Get($"prayers_times_{CoerceLocale()}.rtf"))
            {
                var range = new TextRange(flowDoc.ContentStart, flowDoc.ContentStart);
                range.Load(stream, DataFormats.Rtf);
            }

            TransformParagraphs();
        }

        private static string CoerceLocale()
        {
            string locale = LocaleManager.Default.Current;

            if (SupportedLocales.Contains(locale))
            {
                return locale;
            }

            // We have unsupported locale, so we fallback to either English or Arabic
            return LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft ? "ar" : "en";
        }

        private void TransformParagraphs()
        {
            List<(Paragraph, Inline)> toReplace = new List<(Paragraph, Inline)>();
            List<(Paragraph, Inline)> toDelete = new List<(Paragraph, Inline)>();
            List<(Run, string)> toUpdate = new List<(Run, string)>();

            foreach (var block in flowDoc.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is Span span && span.Inlines.Count > 0 && span.Inlines.FirstInline is Run run)
                        {
                            if (run.Text.StartsWith(ImageToken))
                            {
                                string imgFile = run.Text.Split('#')[1];

                                var img = new Image()
                                {
                                    Width = 400,
                                    Margin = new Thickness(4),
                                    Source = CommonResourcesHelper.GetImage(imgFile),
                                };
                                var container = new InlineUIContainer
                                {
                                    Child = img,
                                };
                                toReplace.Add((paragraph, container));
                            }
                            else if (run.Text.StartsWith(QuranToken))
                            {
                                paragraph.ClearValue(Paragraph.FontSizeProperty);
                                paragraph.Style = FindResource("QuranAyah") as Style;
                                paragraph.FontFamily = LocaleManager.Default.Current == "ar" ? UthmanicHafs : TimesNewRoman;

                                var newText = run.Text.Split('#')[1];

                                if (string.IsNullOrWhiteSpace(newText))
                                {
                                    toDelete.Add((paragraph, inline));
                                }
                                else
                                {
                                    toUpdate.Add((run, newText));
                                }
                            }
                            else if (run.Text.StartsWith(HadithToken))
                            {
                                paragraph.ClearValue(Paragraph.FontSizeProperty);
                                paragraph.Style = FindResource("Hadith") as Style;
                                paragraph.FontFamily = LocaleManager.Default.Current == "ar" ? UthmanTahaNaskh : run.FontFamily;

                                var newText = run.Text.Split('#')[1];

                                if (string.IsNullOrWhiteSpace(newText))
                                {
                                    toDelete.Add((paragraph, inline));
                                }
                                else
                                {
                                    toUpdate.Add((run, newText));
                                }
                            }
                        }
                    }
                }
            }

            foreach ((var paragraph, var inline) in toReplace)
            {
                paragraph.Inlines.Clear();

                if (inline != null)
                {
                    paragraph.Inlines.Add(inline);
                }
            }

            foreach ((var paragraph, var inline) in toDelete)
            {
                paragraph.Inlines.Remove(inline);
            }

            foreach ((var run, var newText) in toUpdate)
            {
                run.Text = newText;
            }
        }
    }
}
