using ClientServerApp.Mobile.Client;
using Xamarin.Forms;

namespace ClientServerApp.Mobile.ViewModels
{
	internal class MainViewModel : BaseViewModel
	{
		private readonly UDPClientManager _clientManager;
		public MainViewModel()
		{
			_clientManager = new UDPClientManager(cinfo => ActivitiesInfo = cinfo);
			SendGreetingCommand = new Command(SendGreeting);
		}
		private async void SendGreeting()
		{
			await _clientManager.SendGreeting();
		}

		private string _activitiesInfo;
		public string ActivitiesInfo
		{
			get => _activitiesInfo;
			set => SetProperty(ref _activitiesInfo, value);
		}
		public Command SendGreetingCommand { get; }
	}
}
