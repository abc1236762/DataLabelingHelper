using System.Net.Http;
using System.Windows;

namespace Wikipedia_Question_Helper
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Main;
        public static MakeQAWindow MakeQA;
        public static EditQAWindow EditQA;
        public static ReAnswerWindow ReAnswer;

        public MainWindow()
        {
            InitializeComponent();
            WindowBlur.WindowBlur.Enable(this);
            Main = this;
        }
        
        private void MakeQAButton_Click(object sender, RoutedEventArgs e)
        {
            if (MakeQA == null)
            {
                MakeQA = new MakeQAWindow();
                MakeQA.Closing += OnWindowClosing;
                WindowBlur.WindowBlur.Enable(MakeQA);
            }
            MakeQA.Show();
            Hide();
        }

        private void EditQAButton_Click(object sender, RoutedEventArgs e)
        {
            if (EditQA == null)
            {
                EditQA = new EditQAWindow();
                EditQA.Closing += OnWindowClosing;
                WindowBlur.WindowBlur.Enable(EditQA);
            }
            EditQA.Show();
            Hide();
        }

        private void ReAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            if (ReAnswer == null)
            {
                ReAnswer = new ReAnswerWindow();
                ReAnswer.Closing += OnWindowClosing;
                WindowBlur.WindowBlur.Enable(ReAnswer);
            }
            ReAnswer.Show();
            Hide();
        }

        private void Window_Closing(object sender, System.EventArgs e)
        {
            if (MakeQA?.IsInitialized == true)
            {
                MakeQA.Closing -= OnWindowClosing;
                MakeQA.Close();
            }
            if (EditQA?.IsInitialized == true)
            {
                EditQA.Closing -= OnWindowClosing;
                EditQA.Close();
            }
            if (ReAnswer?.IsInitialized == true)
            {
                ReAnswer.Closing -= OnWindowClosing;
                ReAnswer.Close();
            }
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            ((Window)sender).Hide();
            Main.Show();
        }
    }
}
