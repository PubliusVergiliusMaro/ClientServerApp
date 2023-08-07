using ClientServerApp.Mobile.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace ClientServerApp.Mobile.ViewModels
{
	internal class MainViewModel : BaseViewModel
	{
		public MainViewModel()
		{
			_activitiesInfo = new StringBuilder();
			_clientManager = new UDPClientManager(cinfo => ActivitiesInfo = cinfo);
			SendGreetingCommand = new Command(SendGreeting);
			UpdateTime();
		}
		
		private readonly UDPClientManager _clientManager;
		private StringBuilder _activitiesInfo;
		public string ActivitiesInfo
		{
			get => _activitiesInfo.ToString();
			set
			{
				_activitiesInfo.AppendLine(value);
				OnPropertyChanged(nameof(ActivitiesInfo));
			}
		}
		private DateTime _timeNow;
		public string TimeNow
		{
			get => _timeNow.ToString("HH:mm:ss:fff");
			set
			{
				_timeNow = DateTime.Now;
				OnPropertyChanged(nameof(TimeNow));
			}
		}
		public ICommand SendGreetingCommand { get; }
		/// <summary>
		/// Update current time
		/// </summary>
		private async Task UpdateTime()
		{
			while (true) await Task.Run(() => TimeNow = DateTime.Now.ToString("HH:mm:ss:fff"));
		}
		private async void SendGreeting() => await _clientManager.SendGreeting();
	}
}
