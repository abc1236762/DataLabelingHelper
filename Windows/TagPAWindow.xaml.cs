using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;

using NetDiff;

using VBFileIO = Microsoft.VisualBasic.FileIO;

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
			public readonly bool IsDuplicate;

			public Item(string[] data, bool isDuplicate) {
				for (int i = 0; i < data.Length; i++)
					data[i] = data[i].Normalize(NormalizationForm.FormKC);

				this.Question = Regex.Replace(data[1], @"^[\w\p{Pd}\s]+\.",
					"", RegexOptions.ECMAScript).Trim();
				this.DocumentNames = new string[10];
				Array.ConstrainedCopy(data, 2, this.DocumentNames, 0, 10);
				this.AnswerID = Int32.Parse(data[12]);
				this.Options = new Dictionary<int, string>();
				for (int i = 13; i < 29; i += 2) {
					string option = data[i + 1].Trim().TrimEnd('。', '.');
					if (data[i] != String.Empty) this.Options.Add(Int32.Parse(data[i]), option);
				}
				this.IsDuplicate = isDuplicate;
			}
		}

		private static readonly Configuration configuration =
			ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
		private static readonly KeyValueConfigurationCollection settings =
			configuration.AppSettings.Settings;
		private static readonly char[] invalidPathChars =
			Path.GetInvalidPathChars().Where(x => x != '/' && x != '\\').ToArray();
		private int lastNotesTextBoxCaretCaretIndex;
		private string dataFile;
		private string questionID;
		private string resultFile;
		private string resultFilePath;
		private Dictionary<string, Item> data;
		private readonly List<string> duplicateQuestionIDs = new List<string>();
		private DocumentItem focusedDocumentItem = null;
		private string selectedText = String.Empty;
		private Regex[] selectedRegexs = null;
		private readonly Dictionary<string, string> Modes = new Dictionary<string, string> {
			{"標　　　　記", String.Empty },
			{"問題可能錯誤", "QuestionIsProbablyWrong" },
			{"答案無法辨識", "AnswerIsUnrecognizable" },
			{"問題無法辨識", "QuestionIsUnrecognizable" },
			{"答案可能錯誤", "AnswerIsProbablyWrong" },
		};
		private double fontSize = 24D;

		public TagPAWindow() {
			this.InitializeComponent();

			if (Directory.Exists(@"data\tagpa\")) {
				this.InputComboBox.Items.Clear();
				foreach (string directoryPath in Directory.GetDirectories(@"data\tagpa\")) {
					Directory.GetFiles(directoryPath, "*.csv").ToList().ForEach(filePath => {
						string directoryName = Path.GetFileName(directoryPath);
						string fileName = Path.GetFileNameWithoutExtension(filePath);
						if (directoryName != fileName) throw new FileNotFoundException();
						this.InputComboBox.Items.Add(fileName);
					});
				}
				this.GetSelectedFile();
			}

			if (String.IsNullOrEmpty(settings["TagPA.ResultFile"].Value))
				this.PathTextBox.Text = DateTime.UtcNow.ToString("yyyy-MM-dd");
			else
				this.PathTextBox.Text = settings["TagPA.ResultFile"].Value;
			try {
				this.NotesTextBox.Text = File.ReadAllText(@"work\tagpa\Notes.txt", Encoding.UTF8);
			} catch (Exception e) {
				if (e is DirectoryNotFoundException) Directory.CreateDirectory(@"work\tagpa");
				else if (!(e is FileNotFoundException)) throw e;
			}
		}

		private void SaveSettings() {
			if (settings["TagPA.DataFile"] is null) settings.Add("TagPA.DataFile", this.dataFile);
			else settings["TagPA.DataFile"].Value = this.dataFile;
			if (settings["TagPA.QuestionID"] is null) settings.Add("TagPA.QuestionID", this.questionID);
			else settings["TagPA.QuestionID"].Value = this.questionID;
			configuration.Save(ConfigurationSaveMode.Modified);
		}

		private void GetSelectedFile() {
			this.dataFile = settings["TagPA.DataFile"].Value ?? this.dataFile;
			if (this.InputComboBox.Items.Count > 0) {
				this.InputComboBox.IsEnabled = true;
				if (this.dataFile == String.Empty) {
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

		private bool IsDataItemDuplicate() {
			this.duplicateQuestionIDs.Clear();
			var result = MessageBoxResult.None;
			string message = String.Empty;
			Item newItem = this.data[this.questionID];
			var untaggedDocuments = newItem.DocumentNames.ToHashSet();
			foreach (KeyValuePair<string, Item> pair in this.data) {
				if (pair.Key == this.questionID) break;
				Item item = pair.Value;
				if (Regex.Replace(item.Question, @"[，。？：（）,.?:()\s]", "").ToLower() !=
					Regex.Replace(newItem.Question, @"[，。？：（）,.?:()\s]", "").ToLower()) continue;
				var options = item.Options.Where(x => x.Key != item.AnswerID)
					.Select(x => Regex.Replace(x.Value, @"^\s*\(\S\)\s*", ""));
				var newOptions = newItem.Options.Where(x => x.Key != newItem.AnswerID)
					.Select(x => Regex.Replace(x.Value, @"^\s*\(\S\)\s*", ""));
				if (String.IsNullOrEmpty(message)) {
					message += $"Question ID {this.questionID}「{newItem.Question}」";
					message += $"\n{this.questionID}的\t選項：答案「{newItem.Options[newItem.AnswerID]}」；其他選項「";
					message += String.Join("／", newOptions) + "」。";
					message += $"\n\t文章：";
					message += String.Join("、", newItem.DocumentNames) +
						"。\n與以下的Question ID的問題重複！\n";
				}

				this.duplicateQuestionIDs.Add(pair.Key);
				string answer = Regex.Replace(item.Options[item.AnswerID], @"^\s*\(\S\)\s*", "");
				answer = answer == Regex.Replace(newItem.Options[newItem.AnswerID], @"^\s*\(\S\)\s*", "") ?
					"一致" : $"「{answer}」";
				message += $"\n{pair.Key}的\t選項：答案{answer}；其他選項";
				var excepedOptions = options.Except(newOptions);
				if (excepedOptions.Count() == 0) message += "完全一致";
				else {
					message += $"不一致「{String.Join("／", excepedOptions)}」";
					if (excepedOptions.Count() < options.Count()) message +=
							$"、一致「{String.Join("／", options.Except(excepedOptions))}」";
				}
				message += $"。\n\t文章：";
				var excepedDocumentNames = newItem.DocumentNames.Except(item.DocumentNames);
				if (excepedDocumentNames.Count() == 0) message += "完全一致。";
				else message += $"沒有「{String.Join("、", excepedDocumentNames)}」。";
				foreach (var name in item.DocumentNames)
					if (untaggedDocuments.Contains(name)) untaggedDocuments.Remove(name);
			}
			if (!String.IsNullOrEmpty(message)) {
				if (untaggedDocuments.Count > 0) {
					message += $"\n\nQuestion ID {this.questionID}還有未標記過的文章「";
					message += String.Join("、", untaggedDocuments) + "」，";
				} else message += $"\n\nQuestion ID {this.questionID}已無未標記過的文章，";
				message += "是否跳過？";
				while (result != MessageBoxResult.Yes && result != MessageBoxResult.No)
					result = MessageBox.Show(message, "重複警告", MessageBoxButton.YesNo,
						MessageBoxImage.Warning, MessageBoxResult.None);
			}
			return result == MessageBoxResult.Yes;
		}

		private void ParseData() {
			string filePath = $@"data\tagpa\{this.dataFile}\{this.dataFile}.csv";
			VBFileIO.TextFieldParser parser = new VBFileIO.TextFieldParser(filePath) {
				TextFieldType = VBFileIO.FieldType.Delimited,
			};
			parser.SetDelimiters(",");
			var lines = new Dictionary<string, string>();
			this.data = new Dictionary<string, Item>();
			parser.ReadFields();
			while (!parser.EndOfData) {
				string[] row = parser.ReadFields();
				string line = String.Join(",", row.Skip(1));
				this.data.Add(row[0], new Item(row.Clone() as string[], lines.ContainsValue(line)));
				lines.Add(row[0], String.Join(",", row.Skip(1)));
			}
		}

		private void InputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			this.dataFile = this.InputComboBox.SelectedItem as string;
			this.ParseData();
			this.CurrentComboBox.Items.Clear();
			foreach (var pair in this.data)
				if (!pair.Value.IsDuplicate) this.CurrentComboBox.Items.Add(pair.Key);
			this.GetQuestion();
			this.SaveSettings();
		}

		private void GetQuestion() {
			this.questionID = settings["TagPA.QuestionID"].Value ?? this.questionID;
			if (this.CurrentComboBox.Items.Count > 0) {
				this.CurrentComboBox.IsEnabled = true;
				if (String.IsNullOrEmpty(this.questionID)) {
					this.CurrentComboBox.SelectedIndex = 0;
					this.questionID = this.CurrentComboBox.Items[0] as string;
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

		private Dictionary<string, bool> IsTaggedInAlreadyChecked() {
			var checkTable =
				new Dictionary<string, Dictionary<bool, List<(string, int)>>>();
			if (Directory.Exists(@"work\tagpa\")) {
				string[] csvFiles = Directory.GetFiles(@"work\tagpa\",
					"*.csv", SearchOption.AllDirectories);
				foreach (string csvFile in csvFiles) {
					VBFileIO.TextFieldParser parser = new VBFileIO.TextFieldParser(csvFile) {
						TextFieldType = VBFileIO.FieldType.Delimited,
					};
					parser.SetDelimiters(",");
					parser.ReadFields();
					while (!parser.EndOfData) {
						string[] row = parser.ReadFields();
						if (row[0] != this.dataFile) continue;
						if (this.duplicateQuestionIDs.IndexOf(row[1]) < 0) continue;
						var documentNames = this.data[row[1]].DocumentNames;
						int[] ids;
						if (row[2] != String.Empty) {
							try {
								string[] idsString = row[2].Split(' ');
								ids = new int[idsString.Length];
								for (int i = 0; i < idsString.Length; i++)
									ids[i] = Int32.Parse(idsString[i]) - 1;
							} catch (FormatException) { continue; }
						} else ids = new int[0];

						for (int i = 0; i < documentNames.Length; i++) {
							if (!checkTable.ContainsKey(documentNames[i]))
								checkTable[documentNames[i]] =
									new Dictionary<bool, List<(string, int)>>();
							bool isTagged = Array.IndexOf(ids, i) >= 0;
							if (!checkTable[documentNames[i]].ContainsKey(isTagged))
								checkTable[documentNames[i]][isTagged] =
									new List<(string, int)>();
							checkTable[documentNames[i]][isTagged].Add((row[1], i));
						}
					}
				}
			}

			var isTaggedInAlreadyChecked = new Dictionary<string, bool>();
			string message = String.Empty;
			foreach (var data in checkTable) {
				bool isFailed = false;
				if (data.Value.Keys.Count != 1) {
					isFailed = true;
					if (message == String.Empty) message += "發生標記結果不相符的狀況：";
					message += $"\n將文章{data.Key}標記為";
					string result(bool value) =>
						String.Join("、", data.Value[value].Select(
							x => $"問題編號{x.Item1}的第{x.Item2}篇"));
					message += $"\n　有幫助⸺{result(true)}";
					message += $"\n　無幫助⸺{result(false)}";
				}
				if (!isFailed) isTaggedInAlreadyChecked[data.Key] = data.Value.Keys.First();
			}

			if (message != String.Empty)
				MessageBox.Show(message + "\n\n建議先解決。", "標記結果衝突警告",
					 MessageBoxButton.OK, MessageBoxImage.Warning);

			return isTaggedInAlreadyChecked;
		}

		private void PreviousButton_Click(object sender, RoutedEventArgs e) =>
			this.CurrentComboBox.SelectedIndex -= 1;

		private void NextButton_Click(object sender, RoutedEventArgs e) =>
			this.CurrentComboBox.SelectedIndex += 1;

		private void CurrentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (this.CurrentComboBox.Items.Count == 0) {
				this.CurrentComboBox.IsEnabled = false;
				this.PreviousButton.IsEnabled = false;
				this.NextButton.IsEnabled = false;
				return;
			}

			this.questionID = this.CurrentComboBox.SelectedItem as string;
			if (this.questionID is null) return;
			this.SaveSettings();
			if (this.IsDataItemDuplicate()) this.CurrentComboBox.SelectedIndex += 1;

			this.QuestionTextBox.Text = String.Empty;
			this.AnswerTextBox.Text = String.Empty;
			this.ContextWrapPanel.Children.Clear();
			this.MatchesTextBox.Text = String.Empty;

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

			this.ProcessTextBlock.Text =
				$"{(this.data.Keys.ToList().IndexOf(this.questionID) + 1) * 100D / this.data.Count:F2}%";
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
				textBox.PreviewKeyDown += this.QATextBox_PreviewKeyDown;
				this.OptionsWrapPanel.Children.Add(textBox);
			}

			var isTaggedInAlreadyChecked = this.IsTaggedInAlreadyChecked();

			for (int i = 1; i <= this.data[this.questionID].DocumentNames.Length; i++) {
				string documentName = this.data[this.questionID].DocumentNames[i - 1];
				string text = WebUtility.HtmlDecode(Regex.Replace(
					File.ReadAllText($@"data\tagpa\{this.dataFile
					}\documents\{documentName}.txt"), @"&＃(\d+)；", @"&#$1;")).Trim();
				DocumentItem documentItem = null;
				string context1 = Regex.Replace(text, @"[，。？：（）,.?:()\s]", "").ToLower();
				foreach (var item in this.ContextWrapPanel.Children.Cast<DocumentItem>()) {
					string context2 = Regex.Replace(item.Context, @"[，。？：（）,.?:()\s]", "").ToLower();
					IEnumerable<DiffResult<char>> diff = DiffUtil.Diff(context1, context2);
					int equal = diff.Where(x => x.Status == DiffStatus.Equal).Count();
					var inserted = diff.Where(x => x.Status == DiffStatus.Inserted);
					var deleted = diff.Where(x => x.Status == DiffStatus.Deleted);
					if (context1 == context2 || (inserted.Count() <= 10 && deleted.Count() <= 10)) {
						if (context1 != context2) {
							item.UpdateLineRunText();
							string message = $"第{i:D2}篇文章與第{item.LineRun.Text}篇相比，相同{equal}字元" +
								$"、移除{deleted.Count()}字元、插入{inserted.Count()}字元。";
							if (deleted.Count() > 0)
								message += $"\n移除字元：「{String.Join("」、「", deleted.Select(x => x.Obj1.ToString()))}」。";
							if (inserted.Count() > 0)
								message += $"\n插入字元：「{String.Join("」、「", inserted.Select(x => x.Obj2.ToString()))}」。";
							message += "\n\n是否合併？";
							var result = MessageBoxResult.None;
							while (result != MessageBoxResult.Yes && result != MessageBoxResult.No)
								result = MessageBox.Show(message, "合併確認", MessageBoxButton.YesNo,
									MessageBoxImage.Question, MessageBoxResult.None);
							if (result == MessageBoxResult.No) break;
						}
						documentItem = item;
						break;
					}
				}
				if (documentItem is null) {
					documentItem = new DocumentItem() {
						Lines = new List<int> { i },
						Context = text,
					};
					documentItem.GotFocus += this.ContextDocumentItem_GotFocus;
					documentItem.LostFocus += this.ContextDocumentItem_LostFocus;
					documentItem.ContextFlowDocument.FontSize = this.fontSize / 3D * 2D;
					this.ContextWrapPanel.Children.Add(documentItem);
				} else {
					documentItem.Lines.Add(i);
					documentItem.UpdateLineRunText();
				}
				if (isTaggedInAlreadyChecked.ContainsKey(documentName)) {
					documentItem.TagButton.Content = isTaggedInAlreadyChecked[documentName] ? "有幫助" : "無幫助";
					documentItem.LockToggleButton.IsChecked = true;
				}
				documentItem.CanScroll = true;
			}
			this.ContextScrollViewer.ScrollToLeftEnd();
		}

		private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			if (this.ContextWrapPanel is null) return;
			if (Double.TryParse(this.FontSizeTextBox.Text, out double newFontSize)) {
				this.fontSize = newFontSize;
				foreach (var child in this.OptionsWrapPanel.Children)
					(child as TextBox).FontSize = newFontSize;
				foreach (var child in this.ContextWrapPanel.Children) {
					DocumentItem documentItem = child as DocumentItem;
					documentItem.ContextFlowDocument.FontSize = newFontSize / 3D * 2D;
					documentItem.AdjustWidth();
				}
				this.NotesTextBox.FontSize = this.AddMatchButton.FontSize =
					this.MatchesTextBox.FontSize = newFontSize / 3D * 2D;
			}
		}

		private void QATextBox_SelectionChanged(object sender, RoutedEventArgs e) =>
			this.selectedText = (sender as TextBox).SelectedText;

		private void MarkDocument() {
			if (this.focusedDocumentItem is null) return;
			Mark.MarkAnswerWithRegex(this.focusedDocumentItem.ContextFlowDocument,
				this.focusedDocumentItem.Context, this.selectedRegexs);
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
			if (this.selectedRegexs != null) this.MarkDocument();
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
			if (!Directory.Exists(@"work\tagpa\"))
				Directory.CreateDirectory(@"work\tagpa\");
			List<string> lines = new List<string>();
			if (!File.Exists(this.resultFilePath)) {
				if (!Directory.GetParent(this.resultFilePath).Exists)
					Directory.CreateDirectory(Directory.GetParent(this.resultFilePath).FullName);
				lines.Add("Dataset,QuestionID,Result");
			}
			lines.Add(line);
			File.AppendAllLines(this.resultFilePath, lines);
			this.CurrentComboBox.SelectedIndex =
				this.CurrentComboBox.SelectedIndex <
				this.CurrentComboBox.Items.Count - 1 ?
				this.CurrentComboBox.SelectedIndex + 1 : -1;
			this.UpdateResultCount();
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e) {
			string line = $"{this.dataFile},{this.questionID},";
			string modeValue = this.Modes[this.ModeButton.Content as string];
			if (modeValue == String.Empty) {
				List<int> taggedIDs = new List<int>();
				List<int> unlockedIDs = new List<int>();
				List<int> taggedButUnlockedIDs = new List<int>();
				foreach (var child in this.ContextWrapPanel.Children) {
					DocumentItem documentItem = child as DocumentItem;
					if (documentItem.LockToggleButton.IsChecked == true) {
						if (documentItem.TagButton.Content is "有幫助")
							taggedIDs = taggedIDs.Concat(documentItem.Lines).ToList();
					} else {
						if (documentItem.TagButton.Content is "有幫助")
							taggedButUnlockedIDs = taggedButUnlockedIDs.Concat(documentItem.Lines).ToList();
						unlockedIDs = unlockedIDs.Concat(documentItem.Lines).ToList();
					}
				}
				if (unlockedIDs.Count > 0) {
					unlockedIDs.Sort();
					string message = $"第{String.Join("、", unlockedIDs)}篇文章未確定。";
					if (taggedButUnlockedIDs.Count > 0)
						message += $"\n有幫助：{String.Join("、", taggedButUnlockedIDs)}。";
					if (taggedButUnlockedIDs.Count != unlockedIDs.Count)
						message += $"\n無幫助：{String.Join("、", unlockedIDs.Except(taggedButUnlockedIDs))}。";
					message += "\n\n是否視為已確定？";
					var result = MessageBoxResult.None;
					while (result != MessageBoxResult.Yes && result != MessageBoxResult.No)
						result = MessageBox.Show(message, "儲存警告", MessageBoxButton.YesNo,
							MessageBoxImage.Warning, MessageBoxResult.None);
					if (result == MessageBoxResult.No) return;
					taggedIDs = taggedIDs.Concat(taggedButUnlockedIDs).ToList();
				}
				taggedIDs.Sort();
				line += String.Join(" ", taggedIDs);
			} else line += modeValue;
			this.SaveFile(line);
		}

		private void MatchesTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			string[] strings = this.MatchesTextBox.Text.Split(
				new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			try {
				this.selectedRegexs = strings.Select(x =>
				new Regex(x, RegexOptions.IgnoreCase)).ToArray();
			} catch (ArgumentException) {
				this.selectedRegexs = null;
			}
			if (this.selectedRegexs is null) this.UnmarkDocument();
			else this.MarkDocument();
		}

		private void AddMatchButton_Click(object sender, RoutedEventArgs e) {
			this.MatchesTextBox.Text = (this.MatchesTextBox.Text +
				" " + Regex.Escape(this.selectedText.Replace(" ", ""))).Trim();
		}

		private void QATextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter || e.Key == Key.Space)
				this.MatchesTextBox.Text = (this.MatchesTextBox.Text +
					" " + Regex.Escape(this.selectedText.Replace(" ", ""))).Trim();
		}

		private void UpdateResultCount() {
			if (File.Exists(this.resultFilePath)) {
				VBFileIO.TextFieldParser parser = new VBFileIO.TextFieldParser(this.resultFilePath) {
					TextFieldType = VBFileIO.FieldType.Delimited,
				};
				parser.SetDelimiters(",");
				parser.ReadFields();
				int count = 0;
				while (!parser.EndOfData) {
					parser.ReadFields();
					count += 1;
				}
				this.CountTextBlock.Text = $": {count}";
			} else this.CountTextBlock.Text = ": 0";
		}

		private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e) {
			this.resultFile = String.Join("\\", this.PathTextBox.Text.Split(
				new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries));
			if (this.resultFile == String.Empty) {
				this.CountTextBlock.Text = ": 0";
				this.SaveButton.IsEnabled = false;
				return;
			} else this.SaveButton.IsEnabled = this.CurrentComboBox.SelectedIndex >= 0;
			this.resultFile = String.Join("_", this.PathTextBox.Text.Split(invalidPathChars));
			if (settings["TagPA.ResultFile"] is null) settings.Add("TagPA.ResultFile", this.resultFile);
			else settings["TagPA.ResultFile"].Value = this.resultFile;
			configuration.Save(ConfigurationSaveMode.Modified);
			this.resultFilePath = Path.Combine(@"work\tagpa\", this.resultFile + ".csv");
			this.UpdateResultCount();
		}

		private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
			File.WriteAllBytes(@"work\tagpa\Notes.txt", Encoding.UTF8.GetBytes(
				this.NotesTextBox.Text.Replace("\r\n", "\n").Replace("\r", "\n")));

		private void NotesTextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.IsRepeat && this.lastNotesTextBoxCaretCaretIndex == this.NotesTextBox.CaretIndex) {
				if (e.Key == Key.Up) this.NotesTextBox.CaretIndex = 0;
				else if (e.Key == Key.Down) this.NotesTextBox.CaretIndex = this.NotesTextBox.Text.Length;
			}
			this.lastNotesTextBoxCaretCaretIndex = this.NotesTextBox.CaretIndex;
		}

		private void NotesTextBox_KeyUp(object sender, KeyEventArgs e) {
			if (this.lastNotesTextBoxCaretCaretIndex == this.NotesTextBox.CaretIndex) {
				if (e.Key == Key.Up) this.NotesTextBox.CaretIndex = 0;
				else if (e.Key == Key.Down) this.NotesTextBox.CaretIndex = this.NotesTextBox.Text.Length;
			}
		}

		private void ContextScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e) {
			if (!(this.focusedDocumentItem is null)) return;
			if (e.Delta < 0) this.ContextScrollViewer.LineRight();
			else this.ContextScrollViewer.LineLeft();
			e.Handled = true;
		}
	}
}
