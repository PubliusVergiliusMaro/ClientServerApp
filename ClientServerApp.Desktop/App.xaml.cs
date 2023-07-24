using ClientServerApp.Desktop.NavigationServices;
using ClientServerApp.Desktop.ViewModels;
using ClientServerApp.Desktop.Views;
using System.Windows;

namespace ClientServerApp.Desktop
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
