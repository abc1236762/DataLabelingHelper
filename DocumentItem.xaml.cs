using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DataLabelingHelper
{
	/// <summary>
	/// ParagraphItem.xaml 的互動邏輯
	/// </summary>
	public partial class DocumentItem : UserControl
	{
		public int Line { get; set; }
		public string Context { get; set; }
		public DocumentItem() { this.InitializeComponent(); }

		
		private void UserControl_Loaded(object sender, RoutedEventArgs e) {
			this.LineRun.Text = $"{this.Line:00}";
			this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
		}


		private void ContextRichTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			//if (this.ContextRichTextBox.IsFocused) {
			//	this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines => {
			//		Inlines.ToList().ForEach(Inline => {
			//			(Inline as Run).FontSize = this.ContextFlowDocument.FontSize;
			//			(Inline as Run).Background = this.BackgroundBrush;
			//			if (this.IsTagged) {
			//				(Inline as Run).Foreground = this.TaggedForegroundBrush;
			//			} else { (Inline as Run).Foreground = this.UntaggedForegroundBrush; }
			//		});
			//	});
			//}
		}

		private void ContextRichTextBox_LostFocus(object sender, RoutedEventArgs e) {

			//this.Context = string.Empty;
			//this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines => {
			//	Inlines.ToList().ForEach(Inline => this.Context += (Inline as Run).Text);
			//	this.Context += Environment.NewLine;
			//});
			//this.Context = this.Context.TrimEnd(Environment.NewLine.ToCharArray());
			//this.ContextFlowDocument?.Blocks.Clear();
			//this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
		}

		private void TagButton_Click(object sender, RoutedEventArgs e) {
			if (this.LockToggleButton.IsChecked == false)
				this.TagButton.Content = this.TagButton.Content is "正確" ? "錯誤" : "正確";
		}
	}
}
