using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static DataLabelingHelper.MainWindow;

namespace DataLabelingHelper
{
    /// <summary>
    /// ParagraphItem.xaml 的互動邏輯
    /// </summary>
    public partial class ParagraphItem : UserControl
    {
        public int Line { get; set; }
        public string Context { get; set; }
        public bool IsTagged { get; set; } = false;
        public ParagraphItem() { this.InitializeComponent(); }

        private SolidColorBrush BackgroundBrush = Brushes.Transparent;
        private SolidColorBrush TaggedForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0xDC, 0xDC));
        private SolidColorBrush UntaggedForegroundBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x9B, 0x9B, 0x9B));

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
			this.LineRun.Text = $"{this.Line:0000}";

            try
            { this.ContextFlowDocument.FontSize = double.Parse(MakeQA.FontSizeTextBox.Text); }
            catch { }
			this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
            int WordTotal = 0;
			this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
            );
			this.CountRun.Text = $"{WordTotal}";
        }

        private void TagButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsTagged)
            {
				this.TagButton.Content = "已標記";
				this.IsTagged = true;
				this.ContextFlowDocument.Foreground = this.TaggedForegroundBrush;
            }
            else
            {
				this.TagButton.Content = "標記";
				this.IsTagged = false;
				this.ContextFlowDocument.Foreground = this.UntaggedForegroundBrush;
            }
			this.ContextFlowDocument.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
            {
                Inlines.ToList().ForEach(Inline =>
                {
                    if (this.IsTagged)
                    { (Inline as Run).Foreground = this.TaggedForegroundBrush; }
                    else
                    { (Inline as Run).Foreground = this.UntaggedForegroundBrush; }
                });
            });
            MakeQA.CountParagraph();
        }

        public void ReplaceParagraph(string FromText,string ToText)
        {
			this.Context = this.Context.Replace(FromText, ToText);
			this.ContextFlowDocument.Blocks.Clear();
			this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
            int WordTotal = 0;
			this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
            );
			this.CountRun.Text = $"{WordTotal}";
        }


        private void ContextRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.ContextRichTextBox.IsFocused)
            {
                int WordTotal = 0;
				this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                    Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
                );
				this.CountRun.Text = $"{WordTotal}";
				this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                {
                    Inlines.ToList().ForEach(Inline =>
                    {
                        (Inline as Run).FontSize = this.ContextFlowDocument.FontSize;
                        (Inline as Run).Background = this.BackgroundBrush;
                        if (this.IsTagged)
                        {
                            (Inline as Run).Foreground = this.TaggedForegroundBrush;
                            MakeQA.CountParagraph();
                        }
                        else
                        { (Inline as Run).Foreground = this.UntaggedForegroundBrush; }
                    });
                });
            }
        }

        private void ContextRichTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

			this.Context = string.Empty;
			this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
            {
                Inlines.ToList().ForEach(Inline => this.Context += (Inline as Run).Text);
				this.Context += Environment.NewLine;
            });
			this.Context = this.Context.TrimEnd(Environment.NewLine.ToCharArray());
			this.ContextFlowDocument?.Blocks.Clear();
			this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
        }
    }
}
