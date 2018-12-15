using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using Microsoft.VisualBasic.FileIO;

namespace DataLabelingHelper
{
	/// <summary>
	/// TagPAWindow.xaml 的互動邏輯
	/// </summary>
	public partial class TagPAWindow : Window
	{
		private struct Item
		{
			public readonly string Question;
			public readonly string[] DocumentNames;
			public readonly int AnswerID;
			public readonly Dictionary<int, string> Options;

			public Item(string[] data) {
				this.Question = data[1];
				this.DocumentNames = new string[10];
				Array.ConstrainedCopy(data, 2, this.DocumentNames, 0, 10);
				this.AnswerID = int.Parse(data[12]);
				this.Options = new Dictionary<int, string>();
				for (int i = 13; i < 29; i += 2) {
					if (data[i] != string.Empty) this.Options.Add(int.Parse(data[i]), data[i + 1]);
				}
			}
		}

		private string dataFile;
		private string questionID;
		private Dictionary<string, Item> data;
		private DocumentItem focusedDocumentItem = null;
		private string selectedText = string.Empty;
		private readonly Dictionary<string, string> Modes = new Dictionary<string, string> {
			{"標　　　　記", string.Empty },
			{"問題無法辨識", "QuestionIsUnrecognizable" },
			{"答案無法辨識", "AnswerIsUnrecognizable" },
			{"答案可能錯誤", "AnswerIsProbablyWrong" },
		};

		public TagPAWindow() {
			this.InitializeComponent();

			if (Directory.Exists(@"data\tagpa\")) {
				this.InputComboBox.Items.Clear();
				Directory.GetFiles(@"data\tagpa\", "*.csv").ToList().ForEach(filePath => {
					this.InputComboBox.Items.Add(Path.GetFileNameWithoutExtension(filePath));
				});
				this.GetSelectedFile();
			}
		}

		private void SaveSettings() {
			var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var Settings = ConfigFile.AppSettings.Settings;
			if (Settings["TagPA.DataFile"] == null) Settings.Add("TagPA.DataFile", this.dataFile);
			else Settings["TagPA.DataFile"].Value = this.dataFile;
			if (Settings["TagPA.QuestionID"] == null) Settings.Add("TagPA.QuestionID", this.questionID);
			else Settings["TagPA.QuestionID"].Value = this.questionID;
			ConfigFile.Save(ConfigurationSaveMode.Modified);
		}

		private void GetSelectedFile() {
			this.dataFile = ConfigurationManager.AppSettings["TagPA.DataFile"] ?? this.dataFile;
			if (this.InputComboBox.Items.Count > 0) {
				this.InputComboBox.IsEnabled = true;
				if (this.dataFile == string.Empty) {
					this.InputComboBox.SelectedIndex = 0;
				} else {
					var list = this.InputComboBox.Items.Cast<string>().Where(Item => Item == this.dataFile);
					if (list.Count() != 1) this.InputComboBox.SelectedIndex = -1;
					else this.InputComboBox.SelectedIndex = this.InputComboBox.Items.IndexOf(list.First());
				}
			} else {
				this.InputComboBox.IsEnabled = false;
				this.InputComboBox.SelectedIndex = -1;
			}
		}

		private void ParseData() {
			string filePath = $@"data\tagpa\{this.InputComboBox.SelectedItem as string}.csv";
			TextFieldParser parser = new TextFieldParser(filePath) {
				TextFieldType = FieldType.Delimited,
			};
			parser.SetDelimiters(",");
			var lines = new Dictionary<string, string>();
			this.data = new Dictionary<string, Item>();
			string[] title = parser.ReadFields();
			while (!parser.EndOfData) {
				string[] row = parser.ReadFields();
				string line = string.Join(",", row.Skip(1));
				if (lines.ContainsValue(line)) continue;
				lines.Add(row[0], string.Join(",", row.Skip(1)));
				this.data.Add(row[0], new Item(row));
			}
		}

		private void InputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			this.dataFile = this.InputComboBox.SelectedItem as string;
			this.SaveSettings();
			this.ParseData();
			this.CurrentComboBox.Items.Clear();
			foreach (var key in this.data.Keys) this.CurrentComboBox.Items.Add(key);
			this.GetQuestion();
		}

		private void GetQuestion() {
			this.questionID = ConfigurationManager.AppSettings["TagPA.QuestionID"] ?? this.questionID;
			if (this.CurrentComboBox.Items.Count > 0) {
				this.CurrentComboBox.IsEnabled = true;
				if (this.questionID == string.Empty) {
					this.CurrentComboBox.SelectedIndex = 0;
				} else {
					var list = this.CurrentComboBox.Items.Cast<string>().Where(Item => Item == this.questionID);
					if (list.Count() != 1) this.CurrentComboBox.SelectedIndex = -1;
					else this.CurrentComboBox.SelectedIndex = this.CurrentComboBox.Items.IndexOf(list.First());
				}
			} else {
				this.CurrentComboBox.SelectedIndex = -1;
				this.CurrentComboBox.IsEnabled = false;
			}
		}

		private void PreviousButton_Click(object sender, RoutedEventArgs e) =>
			this.CurrentComboBox.SelectedIndex -= 1;

		private void NextButton_Click(object sender, RoutedEventArgs e) =>
			this.CurrentComboBox.SelectedIndex += 1;

		private void CurrentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			this.questionID = this.CurrentComboBox.SelectedItem as string;
			this.SaveSettings();

			this.QuestionTextBox.Text = string.Empty;
			this.AnswerTextBox.Text = string.Empty;
			this.ContextWrapPanel.Children.Clear();

			this.ModeButton.Content = this.Modes.Keys.First();
			this.ModeButton.IsEnabled = this.SaveButton.IsEnabled =
				this.CurrentComboBox.SelectedIndex != -1;
			if (this.CurrentComboBox.SelectedIndex <= 0) {
				this.PreviousButton.IsEnabled = false;
				this.NextButton.IsEnabled = true;
			} else if (this.CurrentComboBox.SelectedIndex > 0 &&
				this.CurrentComboBox.SelectedIndex <
				this.CurrentComboBox.Items.Count - 1) {
				this.PreviousButton.IsEnabled = true;
				this.NextButton.IsEnabled = true;
			} else {
				this.PreviousButton.IsEnabled = true;
				this.NextButton.IsEnabled = false;
			}

			this.QuestionTextBox.Text = this.data[this.questionID].Question;
			this.AnswerTextBox.Text = this.data[this.questionID].Options[this.data[this.questionID].AnswerID];
			this.OptionsWrapPanel.Children.Clear();
			foreach (string option in this.data[this.questionID].Options.Values) {
				if (option == this.AnswerTextBox.Text) continue;
				TextBox textBox = XamlReader.Load(XmlReader.Create(
					new StringReader(XamlWriter.Save(this.AnswerTextBox)))) as TextBox;
				textBox.Text = option;
				textBox.SelectionChanged += this.QATextBox_SelectionChanged;
				textBox.LostFocus += this.QATextBox_LostFocus;
				this.OptionsWrapPanel.Children.Add(textBox);
			}


			foreach (string documentName in this.data[this.questionID].DocumentNames) {
				string text = WebUtility.HtmlDecode(Regex.Replace(
					File.ReadAllText($@"data\tagpa\documents\{documentName}.txt"), @"&＃(\d+)；", @"&#$1;"));
				var documentItem = new DocumentItem() {
					Line = Array.IndexOf(this.data[this.questionID].DocumentNames, documentName) + 1,
					Context = text,
				};
				documentItem.Width = 382D;
				documentItem.GotFocus += this.ContextDocumentItem_GotFocus;
				documentItem.LostFocus += this.ContextDocumentItem_LostFocus;
				this.ContextWrapPanel.Children.Add(documentItem);
			}
			this.ContextScrollViewer.ScrollToLeftEnd();
		}

		private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (this.ContextWrapPanel is null) return;
			foreach (var child in this.OptionsWrapPanel.Children) {
				TextBox textBox = child as TextBox;
				double fontSize = textBox.FontSize;
				try { fontSize = double.Parse(this.FontSizeTextBox.Text); } catch { }
				textBox.FontSize = fontSize;
			}
			foreach (var child in this.ContextWrapPanel.Children) {
				DocumentItem documentItem = child as DocumentItem;
				double fontSize = documentItem.ContextFlowDocument.FontSize;
				try { fontSize = double.Parse(this.FontSizeTextBox.Text) / 3D * 2D; } catch { }
				documentItem.ContextFlowDocument.FontSize = fontSize;
			}
		}

		private void QATextBox_SelectionChanged(object sender, RoutedEventArgs e) {
			this.selectedText = (sender as TextBox).SelectedText;
			if (this.selectedText == string.Empty) this.UnmarkDocument();
			else this.MarkDocument();
		}

		private void MarkDocument() {
			if (this.focusedDocumentItem is null) return;
			int none = -1;
			Mark.MarkAnswer(this.focusedDocumentItem.ContextFlowDocument,
				this.focusedDocumentItem.Context, this.selectedText, ref none);
		}

		private void UnmarkDocument() {
			if (this.focusedDocumentItem is null) return;
			Mark.UnmarkAnswer(this.focusedDocumentItem.ContextFlowDocument,
				this.focusedDocumentItem.Context);
		}

		private void QATextBox_LostFocus(object sender, RoutedEventArgs e) =>
			(sender as TextBox).Select(0, 0);

		private void ContextDocumentItem_GotFocus(object sender, RoutedEventArgs e) {
			this.focusedDocumentItem = (sender as DocumentItem);
			if (this.selectedText != string.Empty) this.MarkDocument();
		}

		private void ContextDocumentItem_LostFocus(object sender, RoutedEventArgs e) {
			this.UnmarkDocument();
			this.focusedDocumentItem = null;
		}

		private void ModeButton_Click(object sender, RoutedEventArgs e) {
			int index = this.Modes.Keys.ToList().IndexOf(this.ModeButton.Content as string);
			index = index == this.Modes.Count - 1 ? 0 : index + 1;
			this.ModeButton.Content = this.Modes.Keys.ToArray()[index];
		}

		private void SaveFile(string line) {
			string filename = DateTime.UtcNow.ToString("yyyy-MM-dd") + ".csv";
			string filePath = $@"work\tagpa\{filename}";
			if (!Directory.Exists(@"work\tagpa\"))
				Directory.CreateDirectory(@"work\tagpa\");
			List<string> lines = new List<string>();
			if (!File.Exists(filePath))
				lines.Add("QuestionID,DocumentsHaveAnswerNo.");
			lines.Add(line);
			File.AppendAllLines(filePath, lines);
			this.CurrentComboBox.SelectedIndex =
				this.CurrentComboBox.SelectedIndex <
				this.CurrentComboBox.Items.Count - 1 ?
				this.CurrentComboBox.SelectedIndex + 1 : -1;
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e) {
			string line = $"{this.questionID},";
			string modeValue = this.Modes[this.ModeButton.Content as string];
			if (modeValue == string.Empty) {
				List<int> taggedIDs = new List<int>();
				List<int> unlockedIDs = new List<int>();
				foreach (var child in this.ContextWrapPanel.Children) {
					DocumentItem documentItem = child as DocumentItem;
					if (documentItem.LockToggleButton.IsChecked == true) {
						if (documentItem.TagButton.Content is "正確")
							taggedIDs.Add(documentItem.Line);
					} else unlockedIDs.Add(documentItem.Line);
				}
				if (unlockedIDs.Count > 0) {
					MessageBox.Show($"第{string.Join("、", unlockedIDs)}篇文章未確定。");
					return;
				}
				line += string.Join(" ", taggedIDs);
			} else line += modeValue;
			this.SaveFile(line);
		}
	}
}
