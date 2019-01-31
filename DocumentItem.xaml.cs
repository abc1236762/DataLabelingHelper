using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Microsoft.VisualBasic;

using NetDiff;

using static DataLabelingHelper.MainWindow;

using Word = Microsoft.Office.Interop.Word;

namespace DataLabelingHelper
{
	/// <summary>
	/// ParagraphItem.xaml 的互動邏輯
	/// </summary>
	public partial class DocumentItem : UserControl
	{
		public List<int> Lines { get; set; }
		public string Context { get; set; }

		private FormattedText formattedText;
		
		public DocumentItem() => this.InitializeComponent();

		private void UserControl_Loaded(object sender, RoutedEventArgs e) {
			UpdateLineRunText();
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

		private void LockToggleButton_Checked(object sender, RoutedEventArgs e) {			
			double scrollOffset = TagPA.ContextScrollViewer.ScrollableWidth *
				(TagPA.ContextWrapPanel.Children.IndexOf(this as UIElement) + 1) /
				(TagPA.ContextWrapPanel.Children.Count - 1);
			if (TagPA.ContextScrollViewer.HorizontalOffset < scrollOffset)
				TagPA.ContextScrollViewer.ScrollToHorizontalOffset(scrollOffset);
		}

		public void AdjustWidth() {
			this.formattedText.SetFontSize(this.ContextFlowDocument.FontSize);
			this.Width = this.formattedText.Width + 40D;
		}

		public void UpdateLineRunText() {
			this.Lines.Sort();
			this.LineRun.Text = this.Lines.Select(x => x.ToString("D2")).Aggregate((x, y) => x + "、" + y);
		}

		private void ScToTcButton_Click(object sender, RoutedEventArgs e) {
			string message = "是否要將簡體轉換成正體？";
			var result = MessageBoxResult.None;
			while (result != MessageBoxResult.Yes && result != MessageBoxResult.No)
				result = MessageBox.Show(message, "轉換確認", MessageBoxButton.YesNo,
					MessageBoxImage.Question, MessageBoxResult.None);
			if (result == MessageBoxResult.Yes) {
				var document = new Word.Document();
				document.Content.Text = this.Context;
				document.Content.TCSCConverter(Word.WdTCSCConverterDirection.wdTCSCConverterDirectionSCTC, true, true);
				this.Context = document.Content.Text.Trim();
				document.Close(false);
				this.ContextFlowDocument.Blocks.Clear();
				this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
			}
			this.ScToTcButton.IsEnabled = false;
		}

		private void MergeButton_Click(object sender, RoutedEventArgs e) {
			if (int.TryParse(Interaction.InputBox("要併入第幾篇文章？", "合併文章"), out int id) &&
				(id >= 1 && id <= 10 && this.Lines.IndexOf(id) < 0)) {
				var children = TagPA.ContextWrapPanel.Children;
				DocumentItem item = children.Cast<DocumentItem>().Where(x => x.Lines.IndexOf(id) >= 0).First();
				string context1 = Regex.Replace(this.Context, @"[，。？：（）,.?:()\s]", "").ToLower();
				string context2 = Regex.Replace(item.Context, @"[，。？：（）,.?:()\s]", "").ToLower();
				IEnumerable<DiffResult<char>> diff = DiffUtil.Diff(context1, context2);
				int equal = diff.Where(x => x.Status == DiffStatus.Equal).Count();
				var inserted = diff.Where(x => x.Status == DiffStatus.Inserted);
				var deleted = diff.Where(x => x.Status == DiffStatus.Deleted);
				string message = $"相同{equal}字元、移除{deleted.Count()}字元、插入{inserted.Count()}字元。";
				if (deleted.Count() <= 30) {
					message += $"\n移除字元：{deleted.Select(x => x.Obj1.ToString()).Aggregate((x, y) => x + "、" + y)}。";
				} else message += $"\n移除字元：（因超過30個不顯示）。";
				if (inserted.Count() <= 30) {
					message += $"\n插入字元：{inserted.Select(x => x.Obj2.ToString()).Aggregate((x, y) => x + "、" + y)}。";
				} else message += $"\n插入字元：（因超過30個不顯示）。";
				message += "\n是否合併？";
				var result = MessageBoxResult.None;
				while (result != MessageBoxResult.Yes && result != MessageBoxResult.No)
					result = MessageBox.Show(message, "合併確認", MessageBoxButton.YesNo,
						MessageBoxImage.Question, MessageBoxResult.None);
				if (result == MessageBoxResult.Yes) {
					item.Lines = item.Lines.Concat(this.Lines).ToList();
					item.UpdateLineRunText();
					children.Remove(this as UIElement);
				}
			} else MessageBox.Show("合併失敗。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
