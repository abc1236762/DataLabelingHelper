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
using static DataLabelingHelper.Mark;

namespace DataLabelingHelper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    public partial class ReAnswerWindow : Window
    {
        private Dictionary<string, List<string>> InputList = new Dictionary<string, List<string>>();
        private string Context = string.Empty;

        public string BeginFolder = string.Empty;
        public string BeginPage = string.Empty;

        public ReAnswerWindow()
        {
			this.InitializeComponent();
            for (int i = 1; i <= 5; i++) InputMethod.SetIsInputMethodEnabled((TextBox)this.FindName($"Answer{i}NumberTextBox"), false);

            if (Directory.Exists("data\\reanswer\\"))
            {
				this.InputComboBox.Items.Clear();
                Directory.GetDirectories("data\\reanswer\\").ToList().ForEach(DirectoryPath =>
                {
                    string PathName = Path.GetFileName(DirectoryPath);
					this.InputList.Add(PathName, new List<string>());
                    Directory.GetFiles(Path.Combine(DirectoryPath, "paragraph\\")).ToList().ForEach(FilePath =>
						this.InputList[PathName].Add(Path.GetFileNameWithoutExtension(FilePath)));
					this.InputComboBox.Items.Add(PathName);
                });

                try
                {
					this.BeginFolder = ConfigurationManager.AppSettings["ReAnswer.BeginFolder"] ?? this.BeginFolder;
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
            try
            {
				this.BeginPage = ConfigurationManager.AppSettings["ReAnswer.BeginPage"] ?? this.BeginPage;
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
			this.ProblemListBox.SelectedItems.Clear();
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
				this.Context = File.ReadAllText("data\\reanswer\\" + (string)this.InputComboBox.SelectedItem +
                    "\\paragraph\\" + (string)this.CurrentComboBox.SelectedItem + ".txt");
                try
                {

					this.ContextFlowDocument.Blocks.Add(new Paragraph(new Run(this.Context)));
                    try
                    { this.ContextFlowDocument.FontSize = double.Parse(this.FontSizeTextBox.Text); }
                    catch { }
                    if (this.Context.IndexOf(' ') >= 0)
                    {
						this.ProblemListBox.SelectedItems.Add(this.ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有空格").First());
						this.ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有空格").First().Content
                            = $"第{string.Join(",", Enumerable.Range(0, this.Context.Length).Where(i => this.Context[i] == ' '))}個字元";
                    }
                    if (this.Context.IndexOfAny(new char[] { '(', ')', '[', ']', '（', '）', '［', '］' }) >= 0)
                    {
						this.ProblemListBox.SelectedItems.Add(this.ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有小／中括號").First());
						this.ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有小／中括號").First().Content
                            = $"第{string.Join(",", Enumerable.Range(0, this.Context.Length).Where(i => "()[]（）［］".IndexOf(this.Context[i]) >= 0))}個字元";
                    }

					this.WordTotalRun.Text = $"{this.Context.Length}";
                    string[] Question = File.ReadAllText("data\\reanswer\\" + (string)this.InputComboBox.SelectedItem +
                        "\\question\\" + (string)this.CurrentComboBox.SelectedItem + "_QA.txt").Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < Question.Length; i++) ((TextBox)this.FindName($"Question{i + 1}TextBox")).Text = Question[i];

                }
                catch (Exception Exception) { MessageBox.Show(Exception.Message); }
            }

            try
            {
                var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var Settings = ConfigFile.AppSettings.Settings;
                if (Settings["ReAnswer.BeginFolder"] == null) { Settings.Add("ReAnswer.BeginFolder", (string)this.InputComboBox.SelectedItem); }
                else { Settings["ReAnswer.BeginFolder"].Value = (string)this.InputComboBox.SelectedItem; }
				this.BeginPage = this.CurrentComboBox.SelectedItem == null ? "" : (string)this.CurrentComboBox.SelectedItem;
                if (Settings["ReAnswer.BeginPage"] == null) { Settings.Add("ReAnswer.BeginPage", this.BeginPage); }
                else { Settings["ReAnswer.BeginPage"].Value = this.BeginPage; }
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
				this.ProblemListBox?.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.FontSize = FontSize * 3 / 4);
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
                    Answer = ((TextBox)sender).Text;
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
                    break;
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
            {
                Directory.CreateDirectory(DirectoryName);
                Directory.CreateDirectory(Path.Combine(DirectoryName, "paragraph\\"));
                Directory.CreateDirectory(Path.Combine(DirectoryName, "question\\"));
            }
            string[] DirectoryList = Directory.GetDirectories(".\\", $"{DateTime.UtcNow.ToString("MM-dd")} (*)");
            if (DirectoryList.Length != 1) { MessageBox.Show("資料夾重複。"); return; }
            DirectoryName = Regex.Replace(DirectoryList.First(), @"\((\d+)\)",
                $"({(Directory.GetFiles(Path.Combine(DirectoryList.First(), "question\\"), "*_*_QA.txt", SearchOption.AllDirectories).Length + 1).ToString()})");
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

            File.Copy("data\\reanswer\\" + (string)this.InputComboBox.SelectedItem +
                "\\paragraph\\" + (string)this.CurrentComboBox.SelectedItem + ".txt",
                Path.Combine(DirectoryName, $"paragraph\\{(string)this.CurrentComboBox.SelectedItem}.txt"), true);
            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, $"question\\{(string)this.CurrentComboBox.SelectedItem}_QA.txt"), false, new UTF8Encoding(false)))
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

    }
}