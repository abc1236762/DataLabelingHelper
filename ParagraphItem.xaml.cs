using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static Wikipedia_Question_Helper.MainWindow;

namespace Wikipedia_Question_Helper
{
    /// <summary>
    /// ParagraphItem.xaml 的互動邏輯
    /// </summary>
    public partial class ParagraphItem : UserControl
    {
        public int Line { get; set; }
        public string Context { get; set; }
        public bool IsTagged { get; set; } = false;
        public ParagraphItem() { InitializeComponent(); }

        private SolidColorBrush BackgroundBrush = Brushes.Transparent;
        private SolidColorBrush TaggedForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0xDC, 0xDC));
        private SolidColorBrush UntaggedForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x9B, 0x9B, 0x9B));

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LineRun.Text = $"{Line:0000}";

            try
            { ContextFlowDocument.FontSize = double.Parse(MakeQA.FontSizeTextBox.Text); }
            catch { }
            ContextFlowDocument.Blocks.Add(new Paragraph(new Run(Context)));
            int WordTotal = 0;
            ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
            );
            CountRun.Text = $"{WordTotal}";
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsTagged)
            {
                TagButton.Content = "已標記";
                IsTagged = true;
                ContextFlowDocument.Foreground = TaggedForegroundBrush;
            }
            else
            {
                TagButton.Content = "標記";
                IsTagged = false;
                ContextFlowDocument.Foreground = UntaggedForegroundBrush;
            }
            ContextFlowDocument.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
            {
                Inlines.ToList().ForEach(Inline =>
                {
                    if (IsTagged)
                    { (Inline as Run).Foreground = TaggedForegroundBrush; }
                    else
                    { (Inline as Run).Foreground = UntaggedForegroundBrush; }
                });
            });
            MakeQA.CountParagraph();
        }

        public void ReplaceParagraph(string FromText,string ToText)
        {
            Context = Context.Replace(FromText, ToText);
            ContextFlowDocument.Blocks.Clear();
            ContextFlowDocument.Blocks.Add(new Paragraph(new Run(Context)));
            int WordTotal = 0;
            ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
            );
            CountRun.Text = $"{WordTotal}";
        }


        private void ContextRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ContextRichTextBox.IsFocused)
            {
                int WordTotal = 0;
                ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                    Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
                );
                CountRun.Text = $"{WordTotal}";
                ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                {
                    Inlines.ToList().ForEach(Inline =>
                    {
                        (Inline as Run).FontSize = ContextFlowDocument.FontSize;
                        (Inline as Run).Background = BackgroundBrush;
                        if (IsTagged)
                        {
                            (Inline as Run).Foreground = TaggedForegroundBrush;
                            MakeQA.CountParagraph();
                        }
                        else
                        { (Inline as Run).Foreground = UntaggedForegroundBrush; }
                    });
                });
            }
        }

        private void ContextRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

            Context = string.Empty;
            ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
            {
                Inlines.ToList().ForEach(Inline => Context += (Inline as Run).Text);
                Context += Environment.NewLine;
            });
            Context = Context.TrimEnd(Environment.NewLine.ToCharArray());
            ContextFlowDocument?.Blocks.Clear();
            ContextFlowDocument.Blocks.Add(new Paragraph(new Run(Context)));
        }
    }
}
