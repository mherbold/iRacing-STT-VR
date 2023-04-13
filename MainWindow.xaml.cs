
using System.Windows;

namespace iRacingSTTVR
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool _isClosed { get; set; } = false;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Closed( object sender, System.EventArgs e )
		{
			_isClosed = true;
		}
	}
}
