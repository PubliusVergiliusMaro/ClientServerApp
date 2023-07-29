using ClientServerApp.Mobile.Client;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ClientServerApp.Mobile.ViewModels
{
	internal class MainViewModel : BaseViewModel
	{
		private readonly UDPClientManager _clientManager;
		public MainViewModel()
		{
			_activitiesInfo = new StringBuilder();
			_clientManager = new UDPClientManager(cinfo => ActivitiesInfo = cinfo);
			SendGreetingCommand = new Command(SendGreeting);
			PrepareImageCommand = new Command(PrepareImage);
		}
		private async Task MakeScreenshot()
		{
			var screenshot = await Screenshot.CaptureAsync();
			var stream = await screenshot.OpenReadAsync();
			Screen = ImageSource.FromStream(() => stream);
		}

		private async void PrepareImage(object obj)
		{
			await MakeScreenshot();

			Stream imgStream = await ((StreamImageSource)Screen).Stream(CancellationToken.None);
			imgStream.Seek(0, SeekOrigin.Begin);

			byte[] bytesAvailable = new byte[imgStream.Length];
			await imgStream.ReadAsync(bytesAvailable, 0, bytesAvailable.Length);
			_clientManager.ImageBytes = bytesAvailable;

			await MakeScreenshot();
		}
		private async void SendGreeting()
		{
			await _clientManager.SendGreeting();
		}

		private ImageSource _screen;
		public ImageSource Screen
		{
			get => _screen;
			set
			{ 
				SetProperty(ref _screen, value);
			}
		}
		private StringBuilder _activitiesInfo;
		public string ActivitiesInfo
		{
			get => _activitiesInfo.ToString();
			set
			{
				_activitiesInfo.AppendLine(value);
				OnPropertyChanged(nameof(ActivitiesInfo));
				//SetProperty(ref _data, _data);
			}
		}
		public ICommand SendGreetingCommand { get; }
		public ICommand PrepareImageCommand { get; }
	}
}
