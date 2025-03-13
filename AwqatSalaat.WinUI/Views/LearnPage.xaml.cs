using AwqatSalaat.Helpers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LearnPage : Page, IDisposable
    {
        private static readonly Dictionary<string, FontFamily> FontsCache = new Dictionary<string, FontFamily>();

        private const string ImageToken = "!!IMG";
        private const string QuranToken = "!!QURAN";
        private const string HadithToken = "!!HADITH";

        public LearnPage()
        {
            this.InitializeComponent();
            
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            LoadRtf();
        }

        public void Dispose()
        {
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            Log.Information("Learn page disposed");
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            LoadRtf();
        }

        private void LoadRtf()
        {
            Log.Information("Loading rtf file");
            richBlock.Blocks.Clear();
            var richTB = new RichEditBox();

            using (var stream = CommonResources.Get($"prayers_times_{LocaleManager.Default.Current}.rtf"))
            {
                using (var memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);

                    using (var randAccStream = memStream.AsRandomAccessStream())
                    {
                        richTB.Document.LoadFromStream(TextSetOptions.FormatRtf | TextSetOptions.UnicodeBidi, randAccStream);
                    }
                }
            }

            MoveRtfToRichTextBlock(richTB.Document, richBlock);
            TransformParagraphs(richBlock.Blocks);
        }

        private void MoveRtfToRichTextBlock(RichEditTextDocument doc, RichTextBlock textBlock)
        {
            var range = doc.GetRange(0, 0);
            range.MoveEnd(TextRangeUnit.Paragraph, 1);

            int numIndex = -1;

            while (range.StartPosition < range.StoryLength - 1)
            {
                var format = range.ParagraphFormat;

                if (format.ListType == MarkerType.Arabic && format.ListStyle == MarkerStyle.Period)
                {
                    numIndex = numIndex == -1 ? format.ListStart : numIndex + 1;
                }
                else
                {
                    numIndex = -1;
                }

                var par = GetParagraph(range, numIndex);
                textBlock.Blocks.Add(par);
                range.MoveStart(TextRangeUnit.Paragraph, 1);
                range.MoveEnd(TextRangeUnit.Paragraph, 1);
            }
        }

        private Paragraph GetParagraph(ITextRange textRange, int numberedIndex)
        {
            int start = textRange.StartPosition;
            int end = textRange.EndPosition;
            var format = textRange.ParagraphFormat;
            var paragraph = new Paragraph()
            {
                Margin = new Thickness(
                    ToPixel(format.LeftIndent),
                    ToPixel(format.SpaceBefore),
                    ToPixel(format.RightIndent),
                    ToPixel(format.SpaceAfter)
                    )
            };
            paragraph.TextAlignment = ConvertAlignment(format.Alignment);
            paragraph.TextIndent = ToPixel(format.FirstLineIndent);

            textRange.SetRange(start, start);
            textRange.MoveEnd(TextRangeUnit.CharacterFormat, 1);

            while (textRange.StartPosition < end)
            {
                if (textRange.EndPosition == end && string.IsNullOrWhiteSpace(textRange.Text))
                {
                    if (format.ListType == MarkerType.Bullet)
                    {
                        var bulletInline = GetBulletInline(ToPixel(textRange.CharacterFormat.Size));
                        paragraph.Inlines.Insert(0, bulletInline);
                    }
                    else if (format.ListType == MarkerType.Arabic && format.ListStyle == MarkerStyle.Period)
                    {
                        var numInline = GetInline(textRange.CharacterFormat, numberedIndex + ".\t");
                        paragraph.Inlines.Insert(0, numInline);
                    }

                    break;
                }

                var inline = GetInline(textRange.CharacterFormat, textRange.Text);

                paragraph.Inlines.Add(inline);
                textRange.MoveStart(TextRangeUnit.CharacterFormat, 1);
                textRange.MoveEnd(TextRangeUnit.CharacterFormat, 1);
            }

            // reset bounds to not affect the caller
            textRange.SetRange(start, end);

            return paragraph;
        }

        private Inline GetInline(ITextCharacterFormat format, string text)
        {
            Span span = new Span
            {
                FontSize = ToPixel(format.Size),
                FontFamily = GetFontFamily(format.Name),
                FontWeight = format.Bold == FormatEffect.On ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = format.Italic == FormatEffect.On ? Windows.UI.Text.FontStyle.Italic : Windows.UI.Text.FontStyle.Normal,
            };
            Run run = new Run
            {
                Text = text,
            };

            span.Inlines.Add(run);

            return span;
        }

        private Inline GetBulletInline(double fontSize)
        {
            Span span = new Span
            {
                FontSize = fontSize,
                FontFamily = GetFontFamily("Symbol"),
            };
            Run run = new Run
            {
                Text = "\u00b7\t",
            };

            span.Inlines.Add(run);

            return span;
        }

        private async Task TransformParagraphs(BlockCollection blocks)
        {
            List<(Paragraph, Inline)> toReplace = new List<(Paragraph, Inline)>();
            List<(Paragraph, Inline)> toDelete = new List<(Paragraph, Inline)>();
            List<(Run, string)> toUpdate = new List<(Run, string)>();

            foreach (var block in blocks)
            {
                if (block is Paragraph paragraph)
                {
                    bool resetFormat = false;

                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is Span span && span.Inlines.Count > 0 && span.Inlines.First() is Run run)
                        {
                            if (run.Text.StartsWith(ImageToken))
                            {
                                var imgFile = run.Text.Split('#')[1];
                                var img = new Image()
                                {
                                    Width = 400,
                                    Margin = new Thickness(4),
                                    Source = await GetImageFromCommonResources(imgFile)
                                };
                                var container = new InlineUIContainer
                                {
                                    Child = img,
                                };
                                toReplace.Add((paragraph, container));
                            }
                            else if (run.Text.StartsWith(QuranToken))
                            {
                                paragraph.Foreground = Resources["AyahBrush"] as Brush;
                                paragraph.FontFamily = LocaleManager.Default.Current == "ar" ? Resources["UthmanicHafs"] as FontFamily : GetFontFamily("Times New Roman");
                                paragraph.FontSize = 22;
                                resetFormat = true;

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
                                paragraph.Foreground = Resources["HadithBrush"] as Brush;
                                paragraph.FontFamily = LocaleManager.Default.Current == "ar" ? Resources["UthmanTahaNaskh"] as FontFamily : run.FontFamily;
                                paragraph.FontSize = 20;
                                resetFormat = true;

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

                            if (resetFormat)
                            {
                                span.ClearValue(Span.ForegroundProperty);
                                span.ClearValue(Span.FontFamilyProperty);
                                span.ClearValue(Span.FontSizeProperty);
                            }
                        }
                    }
                }
            }

            foreach ((var paragraph, var inline) in toReplace)
            {
                paragraph.Inlines.Clear();

                if (inline is not null)
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

        private TextAlignment ConvertAlignment(ParagraphAlignment alignment)
        {
            // Sadly the alignment isn't respected when FlowDirection change, so we take left-align as a default
            return alignment switch
            {
                ParagraphAlignment.Center => TextAlignment.Center,
                ParagraphAlignment.Justify => TextAlignment.Justify,
                _ => TextAlignment.Start,
            };
        }

        private static double ToPixel(double pt) => pt * 4 / 3;

        private static async Task<ImageSource> GetImageFromCommonResources(string file)
        {
            using (var stream = CommonResources.Get(file))
            {
                using (var memStream = new MemoryStream())
                {
                    stream.CopyTo(memStream);
                    memStream.Seek(0, SeekOrigin.Begin);

                    var image = new BitmapImage();

                    using (var random = memStream.AsRandomAccessStream())
                    {
                        await image.SetSourceAsync(random);
                    }

                    return image;
                }
            }
        }

        private static FontFamily GetFontFamily(string fontFamily)
        {
            if (!FontsCache.TryGetValue(fontFamily, out var font))
            {
                font = new FontFamily(fontFamily);
                FontsCache.Add(fontFamily, font);
            }

            return font;
        }
    }
}
