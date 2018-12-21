using System;
using System.Globalization;
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

		private FormattedText formattedText;
		
		public DocumentItem() => this.InitializeComponent();

		private void UserControl_Loaded(object sender, RoutedEventArgs e) {
			this.LineRun.Text = $"{this.Line:00}";
			this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
			this.formattedText = new FormattedText(this.Context,
				CultureInfo.CurrentCulture,
				this.ContextFlowDocument.FlowDirection,
				new Typeface(this.ContextFlowDocument.FontFamily,
					this.ContextFlowDocument.FontStyle,
					this.ContextFlowDocument.FontWeight,
					this.ContextFlowDocument.FontStretch),
				this.ContextFlowDocument.FontSize,
				this.ContextFlowDocument.Foreground,
				VisualTreeHelper.GetDpi(this).PixelsPerDip);
			this.AdjustWidth();
		}

		private void TagButton_Click(object sender, RoutedEventArgs e) {
			if (this.LockToggleButton.IsChecked == false)
				this.TagButton.Content = this.TagButton.Content is "有幫助" ? "無幫助" : "有幫助";
		}

		public void AdjustWidth() {
			this.formattedText.SetFontSize(this.ContextFlowDocument.FontSize);
			this.Width = this.formattedText.Width + 40D;
		}
	}
}
