using ClientServerApp.Common;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WPFClient.UDP.Commands;

namespace WPFClient.UDP.ViewModels
{
	public class MainViewModel : ViewModelBase
	{

		public MainViewModel()
		{
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file
			udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSocket.Bind(udpEndPoint);

			serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), 8081);
			var connectingMessage = new RequestData() { Id = currentId, ActionName = "Connecting", Message = "" }.ToJson();
			udpSocket.SendTo(Encoding.UTF8.GetBytes(connectingMessage), serverEndPoint);
			data = new StringBuilder();
			SendGreetingCommand = new DelegateCommand(SendGreeting);

			StartListening();
		}
		private async void StartListening()
		{
			try
			{
				var data = new StringBuilder();
				while (true)
				{
					var buffer = new byte[256];
					var request = new RequestData();
					string answer = "";
					EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					do
					{
						var result = await udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						senderEndPoint = result.RemoteEndPoint;

						answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
						request = JsonConvert.DeserializeObject<RequestData>(answer);
						var methods = new Dictionary<string, Action>
						{
							{RequestActions.IsAlive, async() => await SendAliveStatus() },
							{RequestActions.WpfConnectionStatus, () => DisplayConnectionStatus(bool.Parse(request.Message)) },
							{RequestActions.XamarinConnectionStatus, () => XamarinConnectionStatus(bool.Parse(request.Message)) },
							{RequestActions.Greeting, () => GetGreeting(request.Message) }
						};
						methods[request.ActionName]();
					}
					while (udpSocket.Available > 0);
				}
			}
			catch (Exception ex)
			{
				AppendData(ex.Message);
			}
		}
		private void GetGreeting(string message)
		{
			 AppendData(message);
		}
		private void XamarinConnectionStatus(bool succesfulStatus)
		{
			if (succesfulStatus)
			{
				AppendData("Xamarin client is connected");
			}
			else
			{
				AppendData("Xamarin client is not connected");
			}
		}

		private void DisplayConnectionStatus(bool succesfulStatus)
		{
			if (succesfulStatus)
			{
				AppendData("Succesfuly connected");
			}
			else
			{
				AppendData("Conection failed");
			}
		}

		private async Task SendAliveStatus()
		{
			var connectingMessage = new RequestData { Id = currentId, ActionName=RequestActions.Alive, Message="I`m alive" }.ToJson();
			await udpSocket.SendToAsync(Encoding.UTF8.GetBytes(connectingMessage), serverEndPoint);
		}
		private void AppendData(string line)
		{
			data.AppendLine(line);
			ActivitiesInfo = data.ToString();
		}
		private static void GetIPFromFile(string path)
		{
			string[] lines = File.ReadAllLines(path);
			ip = lines[0];
			serverIp = lines[1];
		}
		private static string GetLocalIP()
		{
			string localIP;
			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
			{
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				localIP = endPoint.Address.ToString();
			}
			return localIP;
		}
		private async void SendGreeting()
		{
			var greetingMessage = new RequestData() { Id = currentId, ActionName = RequestActions.Greeting, Message = "" }.ToJson();
			await udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMessage), serverEndPoint);
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
		private static string ip;
		private static string serverIp;
		private const int port = 8082;
		private const int currentId = 1;

		private readonly IPEndPoint udpEndPoint;
		private readonly IPEndPoint serverEndPoint;
		private readonly IPEndPoint senderEndPoint;
		private readonly Socket udpSocket;
		private readonly StringBuilder data;
		public ICommand SendGreetingCommand { get; }

	}
}
