using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Configuration;
using static DataLabelingHelper.Mark;

namespace DataLabelingHelper
{

    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    /// 
    public partial class MakeQAWindow : Window
    {

        public HttpClient HttpClient = new HttpClient();
        private Dictionary<string, string> InputList = new Dictionary<string, string>();

        public string BeginPage = "0";
        public string BeginParagraph = "0";

        public MakeQAWindow()
        {
			this.InitializeComponent();
            for (int i = 1; i <= 5; i++) InputMethod.SetIsInputMethodEnabled((TextBox)this.FindName($"Answer{i}NumberTextBox"), false);

            if (File.Exists("data\\makeqa\\list.csv"))
            {
                using (var StreamReader = new StreamReader(new FileStream("data\\makeqa\\list.csv", FileMode.Open), new UTF8Encoding(false)))
                { this.GetInputList(StreamReader.ReadToEnd()); }


                if (!File.Exists("data\\makeqa\\data.xml"))
                {
                    if (MessageBox.Show("是否預先下載文章？", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        Dictionary<string, List<string>> DuplicatePages = new Dictionary<string, List<string>>();
                        List<string> NotExistPages = new List<string>();
                        using (var StreamWriter = new StreamWriter("data\\makeqa\\data.xml", false, new UTF8Encoding(false)))
                        {
                            XElement Wikipedia = new XElement("Wikipedia");
                            foreach (var Pair in this.InputList)
                            {
                                if (this.TryDownloadWikipediaPage(Pair.Value, out string PageTitle, out string PageString))
                                {
                                    var Dictionary = Wikipedia.Elements("Page").ToDictionary(Page => Page.Attribute("ID").Value, Page => Page.Attribute("Title").Value);
                                    if (Dictionary.Where(Data => Data.Value == PageTitle).Count() == 0)
                                    {
                                        Wikipedia.Add(
                                            new XElement("Page", PageString,
                                                new XAttribute("ID", Pair.Key),
                                                new XAttribute("Name", Pair.Value),
                                                new XAttribute("Title", PageTitle)
                                            )
                                        );
                                        Debug.WriteLine("{0} - {1} downloaded", Pair.Key, Pair.Value);
                                    }
                                    else
                                    {
                                        var DictionaryPair = Dictionary.Where(Data => Data.Value == PageTitle).ToArray()[0];
                                        if (!DuplicatePages.ContainsKey(DictionaryPair.Value))
                                        {
                                            DuplicatePages.Add(DictionaryPair.Value, new List<string>());
                                            DuplicatePages[DictionaryPair.Value].Add(DictionaryPair.Key);
                                        }
                                        DuplicatePages[DictionaryPair.Value].Add(Pair.Key);
                                    }
                                }
                                else { NotExistPages.Add(Pair.Key); }
                            };
                            StreamWriter.Write(Wikipedia.ToString());
                        }
                        using (var StreamWriter = new StreamWriter("error.txt", false, new UTF8Encoding(false)))
                        {
                            string ErrorMessage = string.Empty;
                            ErrorMessage += $"# 頁面為相同的編號（\"頁面的原始標題\" => 會導向這標題的所有編號）{Environment.NewLine}";
                            DuplicatePages.ToList().ForEach(Pair =>
                            {
                                ErrorMessage += $"\"{Pair.Key}\" => ";
                                Pair.Value.ForEach(Key => ErrorMessage += $"{Key}, ");
                                ErrorMessage = ErrorMessage.Remove(ErrorMessage.Length - 2);
                                ErrorMessage += Environment.NewLine;
                            });
                            ErrorMessage += $"# 頁面為不存在的編號{Environment.NewLine}";
                            ErrorMessage += string.Join(", ", NotExistPages.ToArray());
                            StreamWriter.Write(ErrorMessage);
                        }
                    }
                }

                try
                {
					this.BeginPage = ConfigurationManager.AppSettings["MakeQA.BeginPage"] ?? this.BeginPage;
					this.BeginParagraph = ConfigurationManager.AppSettings["MakeQA.BeginParagraph"] ?? this.BeginParagraph;
					this.CurrentComboBox.SelectedIndex = this.CurrentComboBox.Items.IndexOf(
						this.CurrentComboBox.Items.Cast<ComboBoxItem>().Where(Item => (string)(Item.Tag) == this.BeginPage).First());
					this.ContextWrapPanel.Children.RemoveRange(0, int.Parse(this.BeginParagraph));
                }
                catch { MessageBox.Show("取得進度錯誤。"); }

            }

        }

        private void GetInputList(string OriginalText)
        {
            string[] OriginalList = OriginalText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			this.InputList.Clear();
            List<KeyValuePair<string, int>> SortableList = new List<KeyValuePair<string, int>>();
            var PageElements = XDocument.Load("data\\makeqa\\data.xml").Element("Wikipedia").Elements("Page");
            foreach (string Item in OriginalList)
            {
                var SplitedItem = PageElements.Where(Page => Page.Attribute("ID").Value == Item.Split(new char[] { ',', '\t' },
                    StringSplitOptions.RemoveEmptyEntries)[0]).ToArray();
                if (SplitedItem.Length > 0) SortableList.Add(new KeyValuePair<string, int>(Item, SplitedItem[0].Value.Length));
                else SortableList.Add(new KeyValuePair<string, int>(Item, -1));
            }
            SortableList.Sort((Key, Value) => Key.Value.CompareTo(Value.Value));
            foreach (KeyValuePair<string, int> KeyValuePair in SortableList)
            {
                string[] SplitedItem = KeyValuePair.Key.Split(new char[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				this.InputList.Add(SplitedItem[0], SplitedItem[1]);
                ComboBoxItem ComboBoxItem = new ComboBoxItem()
                {
                    Content = KeyValuePair.Key,
                    Tag = SplitedItem[0]
                };
				this.CurrentComboBox.Items.Add(ComboBoxItem);
            }
			this.CurrentComboBox.IsEnabled = true;
			this.NextButton.IsEnabled = true;
        }

        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            string OriginalText = Clipboard.GetText();
			this.GetInputList(OriginalText);
        }

        private void CurrentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            for (int i = 1; i <= 5; i++)
            {
                (this.FindName($"Question{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).Text = "0";
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).IsEnabled = false;
                (this.FindName($"Answer{i}NumberButton") as Button).Content = "取\n消";
                (this.FindName($"Answer{i}NumberButton") as Button).IsEnabled = false;
            }
			this.TaggedTotalRun.Text = "0";
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
            if (this.CurrentComboBox.SelectedIndex < 0) { return; }
			this.ContextWrapPanel.Children.Clear();
            try
            {
                var PageString = string.Empty;
                if (File.Exists("data\\makeqa\\data.xml"))
                {
                    var WikipediaPage = XDocument.Load("data\\makeqa\\data.xml").Element("Wikipedia").Elements("Page")
                        .Where(Page => Page.Attribute("ID").Value == (string)((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag).ToArray();
                    if (WikipediaPage.Length != 0)
                    { PageString = WikipediaPage[0].Value; }
                }
                if (PageString == string.Empty)
                {
                    if (!this.TryDownloadWikipediaPage(this.InputList[((string)((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag)], out string PageTitle, out PageString))
                    { MessageBox.Show("無此頁面。"); return; }
                }
                PageString.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(Text =>
					this.ContextWrapPanel.Children.Add(new ParagraphItem() { Line = this.ContextWrapPanel.Children.Count + 1, Context = Text }));

                try
                {
                    var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var Settings = ConfigFile.AppSettings.Settings;
                    if (Settings["MakeQA.BeginPage"] == null) { Settings.Add("MakeQA.BeginPage", (string)(((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag)); }
                    else { Settings["MakeQA.BeginPage"].Value = (string)(((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag); }
                    if (this.BeginPage != (string)(((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag))
                    {
                        if (Settings["MakeQA.BeginParagraph"] == null) { Settings.Add("MakeQA.BeginParagraph", "0"); }
                        else { Settings["MakeQA.BeginParagraph"].Value = "0"; }
						this.BeginPage = (string)(((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag);
                    }
                    ConfigFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(ConfigFile.AppSettings.SectionInformation.Name);
                }
                catch { MessageBox.Show("更新進度錯誤。"); }
            }
            catch (Exception Exception) { MessageBox.Show(Exception.Message); }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e) => this.CurrentComboBox.SelectedIndex -= 1;

        private void NextButton_Click(object sender, RoutedEventArgs e) => this.CurrentComboBox.SelectedIndex += 1;

        private bool TryDownloadWikipediaPage(string Title, out string PageTitle, out string PageData)
        {
            XmlDocument XmlDocument = new XmlDocument();
            XmlDocument.LoadXml(this.HttpClient.GetAsync(
                "https://zh.wikipedia.org/w/api.php?titles=" + HttpUtility.UrlEncode(Title)
                + "&format=xml&action=query&uselang=zh-tw&prop=extracts&explaintext&exlimit=1&converttitles=&redirects="
            ).Result.Content.ReadAsStringAsync().Result);
			this.ContextWrapPanel.Children.Clear();
            PageTitle = PageData = string.Empty;
            try
            {
                PageTitle = (XmlDocument.GetElementsByTagName("page")[0] as XmlElement).GetAttribute("title");
                PageData = Regex.Replace(XmlDocument.GetElementsByTagName("extract")[0].InnerText,
                    @"(^\s*$[\r\n]*)|(\(.*?\))|(（.*?）)|(\[.*?\])|(［.*?］)| ", "", RegexOptions.Multiline);
            }
            catch { return false; }
            return true;
        }

        public void CountParagraph()
        {
            int TaggedTotal = 0, WordTotal = 0;
			this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                .Where(Item => Item.IsTagged).ToList().ForEach(Item =>
                {
                    TaggedTotal += 1;

                    Item.ContextFlowDocument.Blocks.Select(Block => (Block as Paragraph).Inlines).ToList().ForEach(Inlines =>
                        Inlines.ToList().ForEach(Inline => WordTotal += ((Run)Inline).Text.Length)
                    );
                }
            );
			this.TaggedTotalRun.Text = TaggedTotal.ToString();
			this.WordTotalRun.Text = WordTotal.ToString();
			this.WordTotalRun.Foreground = (WordTotal > 250 && WordTotal <= 1500) ? new SolidColorBrush(Color.FromArgb(0xFF, 0xDC, 0xDC, 0xDC)) : Brushes.Red;

        }

        private void FontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(this.FontSizeTextBox.Text, out double FontSize))
            {
				this.ContextWrapPanel?.Children.OfType<ParagraphItem>().ToList().ForEach(Item => Item.ContextFlowDocument.FontSize = FontSize);
                for (int i = 1; i <= 5; i++) ((Button)this.FindName($"Answer{i}NumberButton"))?.SetValue(FontSizeProperty, FontSize / 2);
            }
        }

        private void AnswerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string Answer = string.Empty;
            int i = 0;
            for (i = 1; i <= 5; i++)
            {
                if (((TextBox)this.FindName($"Answer{i}TextBox")).IsFocused || ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsFocused)
                {
                    Answer = ((TextBox)sender).Text;
                    if (Answer.Length == 0)
                    {
						this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                            .Where(Item => Item.IsTagged).ToList().ForEach(Item => UnmarkAnswer(Item.ContextFlowDocument, Item.Context));
                        ((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text = "0";
                        ((TextBox)this.FindName($"Answer{i}NumberTextBox")).IsEnabled = false;
                        ((Button)this.FindName($"Answer{i}NumberButton")).Content = "取\n消";
                        ((Button)this.FindName($"Answer{i}NumberButton")).IsEnabled = false;
                    }
                    else
                    {
                        int AnswerNumber = 0;
						this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                            .Where(Item => Item.IsTagged).ToList().ForEach(Item => MarkAnswer(Item.ContextFlowDocument, Item.Context, Answer, ref AnswerNumber));
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
			this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                .Where(Item => Item.IsTagged).ToList().ForEach(Item => UnmarkAnswer(Item.ContextFlowDocument, Item.Context));
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
						this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                            .Where(Item => Item.IsTagged).ToList().ForEach(Item => MarkAnswer(Item.ContextFlowDocument, Item.Context, Answer, ref AnswerNumber));
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
					this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                        .Where(Item => Item.IsTagged).ToList().ForEach(Item =>
                        MarkAnswer(Item.ContextFlowDocument, Item.Context,
                        ((TextBox)this.FindName($"Answer{i}TextBox")).Text, ref AnswerNumber));
                    ((TextBox)sender).SelectAll();
                    break;
                }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string TaggedParagraph = string.Empty, Answers = string.Empty;
			this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                .Where(Item => Item.IsTagged).ToList().ForEach(Item => TaggedParagraph += Item.Context + Environment.NewLine);
            if (TaggedParagraph.Length <= 250 || TaggedParagraph.Length > 1500) { MessageBox.Show("段落字數錯誤。"); return; }

            int AnswerCount = 0;
            for (int i = 1; i <= 5; i++)
            {
                if ((string)((Button)this.FindName($"Answer{i}NumberButton")).Content == "確\n定")
                { MessageBox.Show($"答案{i}未確定。"); return; }
                int.TryParse(((TextBox)this.FindName($"Answer{i}NumberTextBox")).Text, out int AnswerNumber);
                if (!CheckAnswer(TaggedParagraph, ((TextBox)this.FindName($"Question{i}TextBox")).Text,
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

            string[] FileList = Directory.GetFiles(".\\", "*_*.txt", SearchOption.AllDirectories).Select(File => Path.GetFileName(File)).ToArray();

            int FileNumber = 1;
            while (Array.IndexOf(FileList, $"{((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag}_{FileNumber}.txt") >= 0)
            { FileNumber += 1; }
            using (var StreamWriter = new StreamWriter($"{DirectoryName}\\{((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag}_{FileNumber}.txt", false, new UTF8Encoding(false)))
            { StreamWriter.Write(TaggedParagraph.TrimEnd(Environment.NewLine.ToCharArray())); }
            using (var StreamWriter = new StreamWriter($"{DirectoryName}\\{((ComboBoxItem)this.CurrentComboBox.SelectedItem).Tag}_{FileNumber}_QA.txt", false, new UTF8Encoding(false)))
            { StreamWriter.Write(Answers.TrimEnd(Environment.NewLine.ToCharArray())); }

			this.BeginParagraph = this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList().Where(Item => Item.IsTagged).ToList().Last().Line.ToString();
			this.ContextWrapPanel.Children.RemoveRange(0, this.ContextWrapPanel.Children.IndexOf(
				this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList().Where(Item => Item.IsTagged).ToList().Last()) + 1);

            try
            {
                var ConfigFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var Settings = ConfigFile.AppSettings.Settings;
                if (Settings["MakeQA.BeginParagraph"] == null) { Settings.Add("MakeQA.BeginParagraph", this.BeginParagraph); }
                else { Settings["MakeQA.BeginParagraph"].Value = this.BeginParagraph; }
                ConfigFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(ConfigFile.AppSettings.SectionInformation.Name);
            }
            catch { MessageBox.Show("更新進度錯誤。"); }

            for (int i = 1; i <= 5; i++)
            {
                (this.FindName($"Question{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}TextBox") as TextBox).Text = string.Empty;
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).Text = "0";
                (this.FindName($"Answer{i}NumberTextBox") as TextBox).IsEnabled = false;
                (this.FindName($"Answer{i}NumberButton") as Button).Content = "取\n消";
                (this.FindName($"Answer{i}NumberButton") as Button).IsEnabled = false;
            }
			this.TaggedTotalRun.Text = "0";
			this.WordTotalRun.Text = "0";
			this.ContextScrollViewer.ScrollToTop();
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

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ReplaceFromTextBox.Text != string.Empty && this.ReplaceToTextBox.Text != string.Empty)
				this.ContextWrapPanel.Children.OfType<ParagraphItem>().ToList()
                    .ForEach(Item => Item.ReplaceParagraph(this.ReplaceFromTextBox.Text, this.ReplaceToTextBox.Text));
        }

        private void AnswerNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, "[0-9]+");

    }
}