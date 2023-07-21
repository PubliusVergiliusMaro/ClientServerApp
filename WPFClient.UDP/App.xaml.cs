using System.Windows;
using WPFClient.UDP.NavigationServices;
using WPFClient.UDP.ViewModels;
using WPFClient.UDP.Views;

namespace WPFClient.UDP
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private readonly NavigationStore _navigationStore;

		public App()
		{
			_navigationStore = new NavigationStore();
		}
		protected override void OnStartup(StartupEventArgs e)
		{
			_navigationStore.CurrentViewModel = new MainViewModel();

			MainWindow = new MainWindow()
			{
				DataContext = new MainWindowViewModel(_navigationStore)
			};
			MainWindow.Show();
			base.OnStartup(e);
		}
	}
}
