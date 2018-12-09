using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DataLabelingHelper
{
    public static class Mark
    {
        public static SolidColorBrush InlineBackgroundBrush = new SolidColorBrush(Color.FromArgb(0xBF, 0xFF, 0x00, 0x00));
        public static SolidColorBrush AnswerForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x1E, 0x1E, 0x1E));
        public static SolidColorBrush AnswerBackgroundBrush = new SolidColorBrush(Color.FromArgb(0x9F, 0xDC, 0xDC, 0xDC));
        public static SolidColorBrush TaggedForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0xDC, 0xDC));

        public static bool CheckAnswer(string Context, string Question, string Answer, int AnswerNumber, int Number, ref string Answers, ref int AnswerCount)
        {
            if (Question != string.Empty)
            {
                if (Answer == string.Empty) { MessageBox.Show($"答案{Number}為空。"); return false; }
                else
                {
                    int Count = Regex.Matches(Context, Regex.Escape(Answer)).Count;

                    if (Count == 0)
                    { MessageBox.Show($"答案{Number}錯誤。"); return false; }
                    else if (Count > 1)
                    {
                        if (AnswerNumber <= 0)
                        { MessageBox.Show($"答案{Number}模糊。"); return false; }
                        else if (AnswerNumber > Count)
                        { MessageBox.Show($"答案{Number}超出。"); return false; }
                        Answer += $" {AnswerNumber}";
                    }
                    Answers += Question + Environment.NewLine + Answer + Environment.NewLine;
                    AnswerCount += 1;
                }
            }
            return true;
        }

        public static void MarkAnswer(FlowDocument ContextFlowDocument, string Context, string Answer, ref int AnswerNumber)
        {
            int AnswerNumberNow = AnswerNumber;
            ContextFlowDocument.Blocks.Clear();

            Context.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList().ForEach(Line =>
            {
                Paragraph ContextParagraph = new Paragraph();
                Regex.Split(Line, $"({Regex.Escape(Answer)})").ToList().ForEach(Text =>
                {
                    Run Run = new Run(Text);
                    ContextParagraph.Inlines.Add(Run);
                    if (Text == Answer)
                    {
                        Run.Foreground = AnswerForegroundBrush;
                        Run.Background = AnswerBackgroundBrush;
                        TextBlock Inline = new TextBlock()
                        {
                            Text = (++AnswerNumberNow).ToString(),
                            Foreground = TaggedForegroundBrush,
                            Background = InlineBackgroundBrush,
                            FontSize = ContextFlowDocument.FontSize / 1.5
                        };
                        Inline.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
                        Inline.Arrange(new Rect(Inline.DesiredSize));
                        Inline.Margin = new Thickness(Inline.ActualWidth * -1D, 0, 0, 0);
                        ContextParagraph.Inlines.Add(Inline);
                    }
                });
                ContextFlowDocument.Blocks.Add(ContextParagraph);
            });

            AnswerNumber = AnswerNumberNow;
        }
        public static void UnmarkAnswer(FlowDocument ContextFlowDocument, string Context)
        {
            ContextFlowDocument.Blocks.Clear();
                Context.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList().ForEach(Line =>
                    ContextFlowDocument.Blocks.Add(new Paragraph(new Run(Line)))
                );
        }
    }
}
