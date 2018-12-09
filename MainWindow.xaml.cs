using System.Windows;

namespace DataLabelingHelper
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
		public static TagPAWindow TagPA;

		public MainWindow() {
			this.InitializeComponent();
			WindowBlur.WindowBlur.Enable(this);
			Main = this;
		}

		private void MakeQAButton_Click(object sender, RoutedEventArgs e) {
			if (MakeQA is null) {
				MakeQA = new MakeQAWindow();
				MakeQA.Closing += this.OnWindowClosing;
				WindowBlur.WindowBlur.Enable(MakeQA);
			}
			MakeQA.Show();
			this.Hide();
		}

		private void EditQAButton_Click(object sender, RoutedEventArgs e) {
			if (EditQA is null) {
				EditQA = new EditQAWindow();
				EditQA.Closing += this.OnWindowClosing;
				WindowBlur.WindowBlur.Enable(EditQA);
			}
			EditQA.Show();
			this.Hide();
		}

		private void ReAnswerButton_Click(object sender, RoutedEventArgs e) {
			if (ReAnswer is null) {
				ReAnswer = new ReAnswerWindow();
				ReAnswer.Closing += this.OnWindowClosing;
				WindowBlur.WindowBlur.Enable(ReAnswer);
			}
			ReAnswer.Show();
			this.Hide();
		}

		private void TagPAButton_Click(object sender, RoutedEventArgs e) {
			if (TagPA is null) {
				TagPA = new TagPAWindow();
				TagPA.Closing += this.OnWindowClosing;
				WindowBlur.WindowBlur.Enable(TagPA);
			}
			TagPA.Show();
			this.Hide();
		}

		private void Window_Closing(object sender, System.EventArgs e) {
			if (MakeQA?.IsInitialized == true) {
				MakeQA.Closing -= this.OnWindowClosing;
				MakeQA.Close();
			}
			if (EditQA?.IsInitialized == true) {
				EditQA.Closing -= this.OnWindowClosing;
				EditQA.Close();
			}
			if (ReAnswer?.IsInitialized == true) {
				ReAnswer.Closing -= this.OnWindowClosing;
				ReAnswer.Close();
			}
			if (TagPA?.IsInitialized == true) {
				TagPA.Closing -= this.OnWindowClosing;
				TagPA.Close();
			}
			Application.Current.Shutdown();
		}

		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			e.Cancel = true;
			((Window)sender).Hide();
			Main.Show();
		}
	}
}
