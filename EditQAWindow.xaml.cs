using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static DataLabelingHelper.Mark;

namespace DataLabelingHelper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    public partial class EditQAWindow : Window
    {
        private Dictionary<string, List<string>> InputList = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> ErrorList = new Dictionary<string, List<string>>();
        private string Context = string.Empty;

        public string BeginFolder = string.Empty;
        public string BeginPage = string.Empty;

        public EditQAWindow()
        {
			this.InitializeComponent();
            for (int i = 1; i <= 5; i++) InputMethod.SetIsInputMethodEnabled((TextBox)this.FindName($"Answer{i}NumberTextBox"), false);

            if (Directory.Exists("data\\editqa\\"))
            {
				this.InputComboBox.Items.Clear();
                Directory.GetDirectories("data\\editqa\\").ToList().ForEach(DirectoryPath =>
                {
                    string PathName = Path.GetFileName(DirectoryPath);
					this.InputList.Add(PathName, new List<string>());
                    Directory.GetFiles(DirectoryPath, "*_*.txt").ToList().ForEach(FilePath =>
                    {
                        if (Path.GetFileNameWithoutExtension(FilePath).IndexOf("_QA") < 0)
							this.InputList[PathName].Add(Path.GetFileNameWithoutExtension(FilePath));
                    });
					this.InputComboBox.Items.Add(PathName);
                });

                try
                {
					this.BeginFolder = ConfigurationManager.AppSettings["EditQA.BeginFolder"] ?? this.BeginFolder;
					this.InputComboBox.SelectedIndex = this.InputComboBox.Items.IndexOf(
						this.InputComboBox.Items.Cast<string>().Where(Item => Item == this.BeginFolder).First());
                }
                catch { MessageBox.Show("取得進度錯誤。"); }

            }
        }

        private void InputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			this.ContextFlowDocument.Blocks.Clear();
			this.CurrentComboBox.Items.Clear();
			this.Context = string.Empty;
			this.InputList[(string)this.InputComboBox.SelectedItem].ForEach(Item => this.CurrentComboBox.Items.Add(Item));
			this.CurrentComboBox.IsEnabled = true;
            if (this.CurrentComboBox.Items.Count > 0) this.NextButton.IsEnabled = true;
			this.ErrorList.Clear();
            File.ReadAllLines($"data\\editqa\\{(string)this.InputComboBox.SelectedItem}\\資料錯誤.txt").ToList().ForEach(Line =>
            {
                GroupCollection Groups = Regex.Match(Line, @"(\d+_\d+)_QA\.txt \- (.*)").Groups;
                if (!this.ErrorList.ContainsKey(Groups[1].Value)) this.ErrorList.Add(Groups[1].Value, new List<string>());
				this.ErrorList[Groups[1].Value].Add(Groups[2].Value);
            });
            try
            {
				this.BeginPage = ConfigurationManager.AppSettings["EditQA.BeginPage"] ?? this.BeginPage;
                if (this.BeginPage.Length == 0) this.CurrentComboBox.SelectedIndex = -1;
                else
                {
					this.CurrentComboBox.SelectedIndex = this.CurrentComboBox.Items.IndexOf(
					   this.CurrentComboBox.Items.Cast<string>().Where(Item => Item == this.BeginPage).First());
                }
            }
            catch { }
        }

        private void CurrentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			this.Context = string.Empty;
			this.ContextFlowDocument.Blocks.Clear();
			this.OriginalContextFlowDocument.Blocks.Clear();
			this.ProblemListBox.SelectedItems.Clear();
			this.ErrorListBox.Items.Clear();
            for (int i = 1; i <= 5; i++)
            {
                (this.FindName($"Question{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).Text = "0";
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).IsEnabled = false;
                (this.FindName($"Answer{i}NumberButton") as Button).Content = "取\n消";
                (this.FindName($"Answer{i}NumberButton") as Button).IsEnabled = false;
            }
			this.ProblemListBox.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.Content = string.Empty);
			this.WordTotalRun.Text = "0";
			this.SaveButton.IsEnabled = this.CurrentComboBox.SelectedIndex != -1;
            if (this.CurrentComboBox.SelectedIndex <= 0)
            {
				this.PreviousButton.IsEnabled = false;
				this.NextButton.IsEnabled = true;
            }
            else if (this.CurrentComboBox.SelectedIndex > 0 && this.CurrentComboBox.SelectedIndex < this.CurrentComboBox.Items.Count - 1)
            {
				this.PreviousButton.IsEnabled = true;
				this.NextButton.IsEnabled = true;
            }
            else
            {
				this.PreviousButton.IsEnabled = true;
				this.NextButton.IsEnabled = false;
            }
            if (this.CurrentComboBox.SelectedIndex >= 0)
            {
				this.Context = File.ReadAllText("data\\editqa\\" + (string)this.InputComboBox.SelectedItem +
                    "\\" + (string)this.CurrentComboBox.SelectedItem + ".txt");
                try
                {
                    Regex Regex = new Regex(@"(\(.*?\))|(（.*?）)|(\[.*?\])|(［.*?］)|( )", RegexOptions.Singleline);

                    Paragraph ContextParagraph = new Paragraph();
                    var a = Regex.Split(this.Context).ToList();
                    Regex.Split(this.Context).ToList().ForEach(Word =>
                    {
                        if (Word.Length > 0)
                        {
                            Run Run = new Run(Word);
                            ContextParagraph.Inlines.Add(Run);
                            if (Regex.IsMatch(Word))
                            { Run.Foreground = AnswerForegroundBrush; Run.Background = AnswerBackgroundBrush; }
                        }
                    });
					this.OriginalContextFlowDocument.Blocks.Add(ContextParagraph);
					this.Context = Regex.Replace(this.Context, "");
					this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
                    try { this.ContextFlowDocument.FontSize = double.Parse(this.FontSizeTextBox.Text); } catch { }

					this.WordTotalRun.Text = $"{this.Context.Length}";
                    string[] QuestionAndAnswer = File.ReadAllText("data\\editqa\\" + (string)this.InputComboBox.SelectedItem +
                        "\\" + (string)this.CurrentComboBox.SelectedItem + "_QA.txt").Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < QuestionAndAnswer.Length; i++)
                    {
                        if (i % 2 == 0) ((TextBox)this.FindName($"Question{i / 2 + 1}TextBox")).Text = QuestionAndAnswer[i];
                        else
                        {
                            if (QuestionAndAnswer[i].IndexOf(' ') >= 0 &&
                                int.TryParse(QuestionAndAnswer[i].Substring(QuestionAndAnswer[i].LastIndexOf(' ') + 1,
                                    QuestionAndAnswer[i].Length - QuestionAndAnswer[i].LastIndexOf(' ') - 1), out int Number))
                            {
                                ((TextBox)this.FindName($"Answer{i / 2 + 1}TextBox")).Text = QuestionAndAnswer[i].Substring(0, QuestionAndAnswer[i].LastIndexOf(' '));
                                ((TextBox)this.FindName($"Answer{i / 2 + 1}NumberTextBox")).Text = Number.ToString();
                            }
                            else ((TextBox)this.FindName($"Answer{i / 2 + 1}TextBox")).Text = QuestionAndAnswer[i];
							this.HandleAnswerTextBox(i / 2 + 1, ((TextBox)this.FindName($"Answer{i / 2 + 1}TextBox")).Text);
                        }
                    }
                    if (this.ErrorList.TryGetValue((string)this.CurrentComboBox.SelectedItem, out List<string> Lists))
                    { Lists.ForEach(item => this.ErrorListBox.Items.Add(new ListBoxItem { Content = item })); }
                    else this.ErrorListBox.Items.Add(new ListBoxItem { Content = "（無）" });
                    UnmarkAnswer(this.ContextFlowDocument, this.Context);
                }
                catch (Exception Exception) { MessageBox.Show(Exception.Message); }
            }

            try
            {
                var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var Settings = ConfigFile.AppSettings.Settings;
                if (Settings["EditQA.BeginFolder"] == null) { Settings.Add("EditQA.BeginFolder", (string)this.InputComboBox.SelectedItem); }
                else { Settings["EditQA.BeginFolder"].Value = (string)this.InputComboBox.SelectedItem; }
				this.BeginPage = this.CurrentComboBox.SelectedItem == null ? "" : (string)this.CurrentComboBox.SelectedItem;
                if (Settings["EditQA.BeginPage"] == null) { Settings.Add("EditQA.BeginPage", this.BeginPage); }
                else { Settings["EditQA.BeginPage"].Value = this.BeginPage; }
                ConfigFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(ConfigFile.AppSettings.SectionInformation.Name);
            }
            catch { MessageBox.Show("更新進度錯誤。"); }

        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => this.CurrentComboBox.SelectedIndex -= 1;

        private void NextButton_Click(object sender, RoutedEventArgs e) => this.CurrentComboBox.SelectedIndex += 1;


        private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(this.FontSizeTextBox.Text, out double FontSize))
            {
				this.ErrorListBox?.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.FontSize = FontSize * 3 / 4);
				this.ProblemListBox?.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.FontSize = FontSize * 3 / 4);
				this.OriginalContextFlowDocument?.SetValue(FontSizeProperty, FontSize * 3 / 4);
				this.OriginalContextFlowDocument?.Blocks.ToList().ForEach(Block => (Block as Paragraph).FontSize = FontSize * 3 / 4);
                for (int i = 1; i <= 5; i++) ((Button)this.FindName($"Answer{i}NumberButton"))?.SetValue(FontSizeProperty, FontSize / 2);
            }
        }


        private void QuestionTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string SelectedText = ((TextBox)sender).SelectedText;
            int SelectedTextNumber = 0;
            if (((TextBox)sender).SelectedText.Length == 0) UnmarkAnswer(this.ContextFlowDocument, this.Context);
            else MarkAnswer(this.ContextFlowDocument, this.Context, SelectedText, ref SelectedTextNumber);
        }

        private void AnswerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string Answer = string.Empty;
            int i = 1;
            for (i = 1; i <= 5; i++)
            {
                if (((TextBox)this.FindName($"Answer{i}TextBox")).IsFocused || ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
					this.HandleAnswerTextBox(i, ((TextBox)sender).Text);
                    break;
                }
            }
        }

        private void HandleAnswerTextBox(int i, string Answer)
        {
            if (Answer.Length == 0)
            {
                UnmarkAnswer(this.ContextFlowDocument, this.Context);
                ((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text = "0";
                ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                ((Button)this.FindName($"Answer{i}NumberButton")).Content = "取\n消";
                ((Button)this.FindName($"Answer{i}NumberButton")).IsEnabled = false;
            }
            else
            {
                int AnswerNumber = 0;
                MarkAnswer(this.ContextFlowDocument, this.Context, Answer, ref AnswerNumber);
                if (AnswerNumber > 1)
                {
                    if (((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text == "0")
                        ((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text = "1";
                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsEnabled = true;
                    ((Button)this.FindName($"Answer{i}NumberButton")).Content = "確\n定";
                    ((Button)this.FindName($"Answer{i}NumberButton")).IsEnabled = true;
                }
                else if (AnswerNumber == 1)
                {
                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text = "1";
                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                    ((Button)this.FindName($"Answer{i}NumberButton")).Content = "取\n消";
                    ((Button)this.FindName($"Answer{i}NumberButton")).IsEnabled = false;
                }
                else
                {
                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text = "0";
                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                    ((Button)this.FindName($"Answer{i}NumberButton")).Content = "取\n消";
                    ((Button)this.FindName($"Answer{i}NumberButton")).IsEnabled = false;
                }
            }
        }

        private void AnswerNumberButton_Click(object sender, RoutedEventArgs e)
            => ((Button)sender).Content = (string)((Button)sender).Content == "取\n消" ? "確\n定" : "取\n消";

        private void AnswerNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            for (int i = 1; i <= 5; i++)
            {
                if (((TextBox)this.FindName($"Answer{i}NumberTextBox"))?.IsFocused == true)
                { ((Button)this.FindName($"Answer{i}NumberButton")).Content = "取\n消"; break; }
            }
        }

        private void AnswerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionLength = 0;
            UnmarkAnswer(this.ContextFlowDocument, this.Context);
        }

        private void AnswerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            string Answer = string.Empty;
            int i = 1;
            for (i = 1; i <= 5; i++)
            {
                if (((TextBox)this.FindName($"Answer{i}TextBox")).IsFocused || ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    Answer = ((TextBox)sender).Text;
                    if (Answer.Length > 0)
                    {
                        int AnswerNumber = 0;
                        MarkAnswer(this.ContextFlowDocument, this.Context, Answer, ref AnswerNumber);
                    }
                    break;
                }
            }
        }

        private void AnswerNumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 5; i++)
                if (((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    int AnswerNumber = 0;
                    MarkAnswer(this.ContextFlowDocument, this.Context, ((TextBox)this.FindName($"Answer{i}TextBox")).Text, ref AnswerNumber);

                    ((TextBox)this.FindName($"Answer{i}NumberTextBox")).SelectAll();
                    break;
                }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string Answers = string.Empty;
            int AnswerCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if ((string)((Button)this.FindName($"Answer{i}NumberButton")).Content == "確\n定")
                { MessageBox.Show($"答案{i}未確定。"); return; }
                int.TryParse(((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text, out int AnswerNumber);
                if (!CheckAnswer(this.Context, ((TextBox)this.FindName($"Question{i}TextBox")).Text,
                    ((TextBox)this.FindName($"Answer{i}TextBox")).Text, AnswerNumber, i, ref Answers, ref AnswerCount)) return;
            }
            if (AnswerCount < 3) { MessageBox.Show("答案數量不足。"); return; }
            Answers = Answers.Replace("甚麼", "什麼");
            Answers = Answers.Replace("時後", "時候");

            string DirectoryName = $"{DateTime.UtcNow.ToString("MM-dd")} (0)";
            if (Directory.GetDirectories(".\\", $"{DateTime.UtcNow.ToString("MM-dd")} (*)").Length == 0)
            { Directory.CreateDirectory(DirectoryName); }
            string[] DirectoryList = Directory.GetDirectories(".\\", $"{DateTime.UtcNow.ToString("MM-dd")} (*)");
            if (DirectoryList.Length != 1) { MessageBox.Show("資料夾重複。"); return; }
            DirectoryName = Regex.Replace(DirectoryList.First(), @"\((\d+)\)",
                $"({(Directory.GetFiles(DirectoryList.First(), "*_*_QA.txt", SearchOption.AllDirectories).Length + 1).ToString()})");
            while (true)
            {
                try
                {
                    if (DirectoryList.First() != DirectoryName) Directory.Move(DirectoryList.First(), DirectoryName);
                    break;
                }
                catch
                { if (MessageBox.Show("按確定重試", "資料夾更名失敗", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel) return; }
            }

            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, $"{(string)this.CurrentComboBox.SelectedItem}.txt"), false, new UTF8Encoding(false)))
            { StreamWriter.Write(this.Context.TrimEnd(Environment.NewLine.ToCharArray())); }
            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, $"{(string)this.CurrentComboBox.SelectedItem}_QA.txt"), false, new UTF8Encoding(false)))
            { StreamWriter.Write(Answers.TrimEnd(Environment.NewLine.ToCharArray())); }
            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, "資料問題.txt"), true, new UTF8Encoding(false)))
            {
                if (this.ProblemListBox.SelectedIndex >= 0)
                {
                    string Probrem = string.Empty;
                    Probrem = $"{(string)this.CurrentComboBox.SelectedItem} 有下列問題：\n";
					this.ProblemListBox.Items.Cast<ListBoxItem>().ToList().ForEach(Item =>
                    {
                        if (!Item.IsSelected && Item.Content?.ToString().Length > 0)
                        { if (MessageBox.Show("按確定選擇", "問題有填但未選擇", MessageBoxButton.OKCancel) == MessageBoxResult.OK) Item.IsSelected = true; }
                        if (Item.IsSelected)
                        {
                            Probrem += $"\t{Item.Tag}";
                            if (((string)Item.Content).Length > 0) Probrem += $"｛{Item.Content}｝";
                            Probrem += "\n";
                        }
                    });
                    Probrem += "\n";
                    StreamWriter.Write(Probrem);
                }
            }
			this.CurrentComboBox.SelectedIndex = this.CurrentComboBox.SelectedIndex < this.CurrentComboBox.Items.Count - 1 ? this.CurrentComboBox.SelectedIndex + 1 : -1;
        }

        void MoveToNextUIElement(KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is UIElement KeyboardFocusedElement &&
                KeyboardFocusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)))
            { e.Handled = true; }
        }

        private void QuestionAndAnswerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (sender as TextBox).AcceptsReturn == false) this.MoveToNextUIElement(e);
            if (e.Key == Key.Up) (sender as TextBox).CaretIndex = 0;
            if (e.Key == Key.Down) (sender as TextBox).CaretIndex = (sender as TextBox).Text.Length;
        }

        private void AnswerNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, "[0-9]+");

        private void ContextRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.ContextRichTextBox.IsFocused)
            {
                int WordTotal = 0;
				this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                    Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
                );
				this.WordTotalRun.Text = $"{WordTotal}";
				this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                {
                    Inlines.ToList().ForEach(Inline =>
                    {
                        (Inline as Run).FontSize = this.ContextFlowDocument.FontSize;
                        (Inline as Run).Background = Brushes.Transparent;
                        (Inline as Run).Foreground = TaggedForegroundBrush;
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

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ReplaceFromTextBox.Text != string.Empty && this.ReplaceToTextBox.Text != string.Empty)
            {
				this.Context = this.Context.Replace(this.ReplaceFromTextBox.Text, this.ReplaceToTextBox.Text);
				this.ContextFlowDocument.Blocks.Clear();
				this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
                int WordTotal = 0;
				this.ContextFlowDocument?.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                    Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length));
				this.WordTotalRun.Text = $"{WordTotal}";
            }
        }
    }
}