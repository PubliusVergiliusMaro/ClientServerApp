using ClientServerApp.Client;
using ClientServerApp.Desktop.Commands;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClientServerApp.Desktop.ViewModels
{
	public class MainViewModel : ViewModelBase
	{
		private readonly UDPClientManager _clientManager;
		public MainViewModel()
		{
			_clientManager = new UDPClientManager(ainfo => ActivitiesInfo = ainfo,img=>BinaryImage = img);
			SendGreetingCommand = new DelegateCommand(SendGreeting);
			SendImageCommand = new DelegateCommand(SendImage);
			//SendVideoCommand = new DelegateCommand(SendVideo);
		}

		//private async void SendVideo()
		//{
		//	await _clientManager.SendVideo();
		//	MessageBox.Show("Succesfully sended");
		//}

		private async void SendGreeting()
		{
			await _clientManager.SendGreeting();
		}
		private async void SendImage()
		{
			await _clientManager.AskForImage();
			//await _clientManager.SendImage();
			//MessageBox.Show("Succesfully sended");
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
		private byte[] _binaryImage;
		public byte[] BinaryImage
		{
			get => _binaryImage;
			set
			{
				_binaryImage = value;
				OnPropertyChanged(nameof(ImageSource));
				using (var stream = new MemoryStream(value))
				{
					var bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.StreamSource = stream;
					bitmapImage.EndInit();
					Image = bitmapImage;
				}
			}
		}
		private ImageSource _image;
		public ImageSource Image
		{
			get => _image;
			set
			{
				_image = value;
				OnPropertyChanged(nameof(Image));
			}
		}
		//public ICommand SendVideoCommand { get; }
		public ICommand SendGreetingCommand { get; }
		public ICommand SendImageCommand { get; }
	}
}
