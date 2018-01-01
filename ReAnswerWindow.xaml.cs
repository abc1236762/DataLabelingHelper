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
using static Wikipedia_Question_Helper.Mark;

namespace Wikipedia_Question_Helper
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
            InitializeComponent();
            for (int i = 1; i <= 5; i++) InputMethod.SetIsInputMethodEnabled((TextBox)FindName($"Answer{i}NumberTextBox"), false);

            if (Directory.Exists("data\\reanswer\\"))
            {
                InputComboBox.Items.Clear();
                Directory.GetDirectories("data\\reanswer\\").ToList().ForEach(DirectoryPath =>
                {
                    string PathName = Path.GetFileName(DirectoryPath);
                    InputList.Add(PathName, new List<string>());
                    Directory.GetFiles(Path.Combine(DirectoryPath, "paragraph\\")).ToList().ForEach(FilePath =>
                        InputList[PathName].Add(Path.GetFileNameWithoutExtension(FilePath)));
                    InputComboBox.Items.Add(PathName);
                });

                try
                {
                    BeginFolder = ConfigurationManager.AppSettings["ReAnswer.BeginFolder"] ?? BeginFolder;
                    InputComboBox.SelectedIndex = InputComboBox.Items.IndexOf(
                        InputComboBox.Items.Cast<string>().Where(Item => Item == BeginFolder).First());
                }
                catch { MessageBox.Show("取得進度錯誤。"); }

            }
        }

        private void InputComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContextFlowDocument.Blocks.Clear();
            CurrentComboBox.Items.Clear();
            Context = string.Empty;
            InputList[(string)InputComboBox.SelectedItem].ForEach(Item => CurrentComboBox.Items.Add(Item));
            CurrentComboBox.IsEnabled = true;
            if (CurrentComboBox.Items.Count > 0) NextButton.IsEnabled = true;
            try
            {
                BeginPage = ConfigurationManager.AppSettings["ReAnswer.BeginPage"] ?? BeginPage;
                if (BeginPage.Length == 0) CurrentComboBox.SelectedIndex = -1;
                else
                {
                    CurrentComboBox.SelectedIndex = CurrentComboBox.Items.IndexOf(
                       CurrentComboBox.Items.Cast<string>().Where(Item => Item == BeginPage).First());
                }
            }
            catch { }
        }

        private void CurrentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Context = string.Empty;
            ContextFlowDocument.Blocks.Clear();
            ProblemListBox.SelectedItems.Clear();
            for (int i = 1; i <= 5; i++)
            {
                (FindName($"Question{i}TextBox") as TextBox).Text = string.Empty;
                (FindName($"Answer{i}TextBox") as TextBox).Text = string.Empty;
                (FindName($"Answer{i}NumberTextBox") as TextBox).Text = "0";
                (FindName($"Answer{i}NumberTextBox") as TextBox).IsEnabled = false;
                (FindName($"Answer{i}NumberButton") as Button).Content = "取\n消";
                (FindName($"Answer{i}NumberButton") as Button).IsEnabled = false;
            }
            ProblemListBox.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.Content = string.Empty);
            WordTotalRun.Text = "0";
            SaveButton.IsEnabled = CurrentComboBox.SelectedIndex != -1;
            if (CurrentComboBox.SelectedIndex <= 0)
            {
                PreviousButton.IsEnabled = false;
                NextButton.IsEnabled = true;
            }
            else if (CurrentComboBox.SelectedIndex > 0 && CurrentComboBox.SelectedIndex < CurrentComboBox.Items.Count - 1)
            {
                PreviousButton.IsEnabled = true;
                NextButton.IsEnabled = true;
            }
            else
            {
                PreviousButton.IsEnabled = true;
                NextButton.IsEnabled = false;
            }
            if (CurrentComboBox.SelectedIndex >= 0)
            {
                Context = File.ReadAllText("data\\reanswer\\" + (string)InputComboBox.SelectedItem +
                    "\\paragraph\\" + (string)CurrentComboBox.SelectedItem + ".txt");
                try
                {

                    ContextFlowDocument.Blocks.Add(new Paragraph(new Run(Context)));
                    try
                    { ContextFlowDocument.FontSize = double.Parse(FontSizeTextBox.Text); }
                    catch { }
                    if (Context.IndexOf(' ') >= 0)
                    {
                        ProblemListBox.SelectedItems.Add(ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有空格").First());
                        ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有空格").First().Content
                            = $"第{string.Join(",", Enumerable.Range(0, Context.Length).Where(i => Context[i] == ' '))}個字元";
                    }
                    if (Context.IndexOfAny(new char[] { '(', ')', '[', ']', '（', '）', '［', '］' }) >= 0)
                    {
                        ProblemListBox.SelectedItems.Add(ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有小／中括號").First());
                        ProblemListBox.Items.Cast<ListBoxItem>().Where(Item => (string)Item.Tag == "文中有小／中括號").First().Content
                            = $"第{string.Join(",", Enumerable.Range(0, Context.Length).Where(i => "()[]（）［］".IndexOf(Context[i]) >= 0))}個字元";
                    }

                    WordTotalRun.Text = $"{Context.Length}";
                    string[] Question = File.ReadAllText("data\\reanswer\\" + (string)InputComboBox.SelectedItem +
                        "\\question\\" + (string)CurrentComboBox.SelectedItem + "_QA.txt").Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < Question.Length; i++) ((TextBox)FindName($"Question{i + 1}TextBox")).Text = Question[i];

                }
                catch (Exception Exception) { MessageBox.Show(Exception.Message); }
            }

            try
            {
                var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var Settings = ConfigFile.AppSettings.Settings;
                if (Settings["ReAnswer.BeginFolder"] == null) { Settings.Add("ReAnswer.BeginFolder", (string)InputComboBox.SelectedItem); }
                else { Settings["ReAnswer.BeginFolder"].Value = (string)InputComboBox.SelectedItem; }
                BeginPage = CurrentComboBox.SelectedItem == null ? "" : (string)CurrentComboBox.SelectedItem;
                if (Settings["ReAnswer.BeginPage"] == null) { Settings.Add("ReAnswer.BeginPage", BeginPage); }
                else { Settings["ReAnswer.BeginPage"].Value = BeginPage; }
                ConfigFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(ConfigFile.AppSettings.SectionInformation.Name);
            }
            catch { MessageBox.Show("更新進度錯誤。"); }

        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => CurrentComboBox.SelectedIndex -= 1;

        private void NextButton_Click(object sender, RoutedEventArgs e) => CurrentComboBox.SelectedIndex += 1;


        private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(FontSizeTextBox.Text, out double FontSize))
            {
                ProblemListBox?.Items.Cast<ListBoxItem>().ToList().ForEach(Item => Item.FontSize = FontSize * 3 / 4);
                for (int i = 1; i <= 5; i++) ((Button)FindName($"Answer{i}NumberButton"))?.SetValue(FontSizeProperty, FontSize / 2);
            }
        }


        private void QuestionTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            string SelectedText = ((TextBox)sender).SelectedText;
            int SelectedTextNumber = 0;
            if (((TextBox)sender).SelectedText.Length == 0) UnmarkAnswer(ContextFlowDocument, Context);
            else MarkAnswer(ContextFlowDocument, Context, SelectedText, ref SelectedTextNumber);
        }

        private void AnswerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string Answer = string.Empty;
            int i = 1;
            for (i = 1; i <= 5; i++)
            {
                if (((TextBox)FindName($"Answer{i}TextBox")).IsFocused || ((TextBox)FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    Answer = ((TextBox)sender).Text;
                    if (Answer.Length == 0)
                    {
                        UnmarkAnswer(ContextFlowDocument, Context);
                        ((TextBox)FindName($"Answer{i}NumberTextBox")).Text = "0";
                        ((TextBox)FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                        ((Button)FindName($"Answer{i}NumberButton")).Content = "取\n消";
                        ((Button)FindName($"Answer{i}NumberButton")).IsEnabled = false;
                    }
                    else
                    {
                        int AnswerNumber = 0;
                        MarkAnswer(ContextFlowDocument, Context, Answer, ref AnswerNumber);
                        if (AnswerNumber > 1)
                        {
                            if (((TextBox)FindName($"Answer{i}NumberTextBox")).Text == "0")
                                ((TextBox)FindName($"Answer{i}NumberTextBox")).Text = "1";
                            ((TextBox)FindName($"Answer{i}NumberTextBox")).IsEnabled = true;
                            ((Button)FindName($"Answer{i}NumberButton")).Content = "確\n定";
                            ((Button)FindName($"Answer{i}NumberButton")).IsEnabled = true;
                        }
                        else if (AnswerNumber == 1)
                        {
                            ((TextBox)FindName($"Answer{i}NumberTextBox")).Text = "1";
                            ((TextBox)FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                            ((Button)FindName($"Answer{i}NumberButton")).Content = "取\n消";
                            ((Button)FindName($"Answer{i}NumberButton")).IsEnabled = false;
                        }
                        else
                        {
                            ((TextBox)FindName($"Answer{i}NumberTextBox")).Text = "0";
                            ((TextBox)FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                            ((Button)FindName($"Answer{i}NumberButton")).Content = "取\n消";
                            ((Button)FindName($"Answer{i}NumberButton")).IsEnabled = false;
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
                if (((TextBox)FindName($"Answer{i}NumberTextBox"))?.IsFocused == true)
                { ((Button)FindName($"Answer{i}NumberButton")).Content = "取\n消"; break; }
            }
        }

        private void AnswerTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionLength = 0;
            UnmarkAnswer(ContextFlowDocument, Context);
        }

        private void AnswerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            string Answer = string.Empty;
            int i = 1;
            for (i = 1; i <= 5; i++)
            {
                if (((TextBox)FindName($"Answer{i}TextBox")).IsFocused || ((TextBox)FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    Answer = ((TextBox)sender).Text;
                    if (Answer.Length > 0)
                    {
                        int AnswerNumber = 0;
                        MarkAnswer(ContextFlowDocument, Context, Answer, ref AnswerNumber);
                    }
                    break;
                }
            }
        }

        private void AnswerNumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 5; i++)
                if (((TextBox)FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    int AnswerNumber = 0;
                    MarkAnswer(ContextFlowDocument, Context, ((TextBox)FindName($"Answer{i}TextBox")).Text, ref AnswerNumber);

                    ((TextBox)FindName($"Answer{i}NumberTextBox")).SelectAll();
                    break;
                }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string Answers = string.Empty;
            int AnswerCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if ((string)((Button)FindName($"Answer{i}NumberButton")).Content == "確\n定")
                { MessageBox.Show($"答案{i}未確定。"); return; }
                int.TryParse(((TextBox)FindName($"Answer{i}NumberTextBox")).Text, out int AnswerNumber);
                if (!CheckAnswer(Context, ((TextBox)FindName($"Question{i}TextBox")).Text,
                    ((TextBox)FindName($"Answer{i}TextBox")).Text, AnswerNumber, i, ref Answers, ref AnswerCount)) return;
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

            File.Copy("data\\reanswer\\" + (string)InputComboBox.SelectedItem +
                "\\paragraph\\" + (string)CurrentComboBox.SelectedItem + ".txt",
                Path.Combine(DirectoryName, $"paragraph\\{(string)CurrentComboBox.SelectedItem}.txt"), true);
            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, $"question\\{(string)CurrentComboBox.SelectedItem}_QA.txt"), false, new UTF8Encoding(false)))
            { StreamWriter.Write(Answers.TrimEnd(Environment.NewLine.ToCharArray())); }
            using (var StreamWriter = new StreamWriter(Path.Combine(DirectoryName, "資料問題.txt"), true, new UTF8Encoding(false)))
            {
                if (ProblemListBox.SelectedIndex >= 0)
                {
                    string Probrem = string.Empty;
                    Probrem = $"{(string)CurrentComboBox.SelectedItem} 有下列問題：\n";
                    ProblemListBox.Items.Cast<ListBoxItem>().ToList().ForEach(Item =>
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
            CurrentComboBox.SelectedIndex = CurrentComboBox.SelectedIndex < CurrentComboBox.Items.Count - 1 ? CurrentComboBox.SelectedIndex + 1 : -1;
        }

        void MoveToNextUIElement(KeyEventArgs e)
        {
            if (Keyboard.FocusedElement is UIElement KeyboardFocusedElement &&
                KeyboardFocusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)))
            { e.Handled = true; }
        }

        private void QuestionAndAnswerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (sender as TextBox).AcceptsReturn == false) MoveToNextUIElement(e);
            if (e.Key == Key.Up) (sender as TextBox).CaretIndex = 0;
            if (e.Key == Key.Down) (sender as TextBox).CaretIndex = (sender as TextBox).Text.Length;
        }

        private void AnswerNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, "[0-9]+");

    }
}