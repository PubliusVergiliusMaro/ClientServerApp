using ClientServerApp.Client;
using ClientServerApp.Desktop.Commands;
using System.Windows.Input;

namespace ClientServerApp.Desktop.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private readonly UDPClientManager _clientManager;
		public MainViewModel()
		{
			_clientManager = new UDPClientManager(ainfo => ActivitiesInfo = ainfo);
			SendGreetingCommand = new DelegateCommand(SendGreeting);
		}
		private async void SendGreeting()
		{
			await _clientManager.SendGreeting();
		}

		private string _activitiesInfo;
		public string ActivitiesInfo
		{
			get => _activitiesInfo;
			set
			{
				_activitiesInfo = value;
				OnPropertyChanged(nameof(ActivitiesInfo));
			}
		}

		public ICommand SendGreetingCommand { get; }

	}
}
