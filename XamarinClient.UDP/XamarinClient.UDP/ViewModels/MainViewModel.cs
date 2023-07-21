using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Xamarin.Forms;
using System.Collections.Generic;
using XamarinClient.UDP.Common;
using System.Threading.Tasks;
using System.IO;
using XamarinClient.UDP.Helpers;
using System.Threading;

namespace XamarinClient.UDP.ViewModels
{
	internal class MainViewModel : BaseViewModel
	{
		public MainViewModel() 
		{
			try
			{
				SendGreetingCommand = new Command(SendGreeting);
				udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
				udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				udpSocket.Bind(udpEndPoint);
				data = new StringBuilder();

				serverEndPoint = new IPEndPoint(IPAddress.Parse("111.222.3.444"), 8081);// Change On Yours Server IP
				var connectingMessage = new RequestData() { Id = currentID, ActionName = "Connecting", Message = "message" }.ToJson();
				Send(connectingMessage, serverEndPoint);

				StartListening();
			}
			catch (SocketException e)
			{
				throw new Exception(e.Message);
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
		private async void StartListening()
		{
			try
			{
				while (true)
				{
					var buffer = new byte[256];
					string answer = "";
					EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					int id = 0;
					string action = "";
					string message = "";
					do
					{
						var result = await udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						senderEndPoint = result.RemoteEndPoint;
						//answer format -> ID:ActionName:Message
						answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
						string[] parts = answer.Split(':');

						if (int.TryParse(parts[0], out int intValue))
						{
							id = intValue;
							action = parts[1].Replace(":", "");
							message = parts[2].Replace(":", "");
						}
						//request = JsonConvert.DeserializeObject<RequestManager>(answer);

						var methods = new Dictionary<string, Action>
						{
							{ RequestActions.IsAlive, SendAliveStatus },
							{ RequestActions.XamarinConnectionStatus,() => DisplayConnectionStatus(message) },
							{ RequestActions.WpfConnectionStatus, () => WpfConnectionStatus(message) },
							{ RequestActions.Greeting, () => GetGreeting(message) }
						};
						methods[action]();
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

		private void WpfConnectionStatus(string successfulStatus)
		{
			if (successfulStatus == "true")
			{
				AppendData("Wpf client is connected");
			}
			else if (successfulStatus == "false")
			{
				AppendData("Wpf client in not connected");
			}
			else
				throw new Exception("Status is not equal true/false format.");
		}

		private async void DisplayConnectionStatus(string successfulStatus)
		{
			if (successfulStatus == "true")
			{
				 AppendData("Succesfully connected");
			}
			else if (successfulStatus == "false")
			{
				 AppendData("Connection failed");
			}
			else
				throw new Exception("Status is not equal true/false format.");
		}

		private void SendAliveStatus()
		{
			var connectingMessage = new RequestData { Id = currentID, ActionName="Alive", Message="alive" }.ToJson();
			Send(connectingMessage, serverEndPoint);
		}
		private void AppendData(string line)
		{
			data.AppendLine(line);
			Device.BeginInvokeOnMainThread(() => ActivitiesInfo = data.ToString());
		}

		private static string GetIPFromFile(string path)
		{
			string[] lines = File.ReadAllLines(path);
			string ipAdress = lines[0];
			return ipAdress;
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
			var connectingMessage = new RequestData { Id = currentID, ActionName="Greeting", Message="alive" }.ToJson();
			Send(connectingMessage, serverEndPoint);
		}
		private void Send(string message,IPEndPoint endPoint)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);

			var waitHandle = new ManualResetEvent(false);
			var sendEvent = new SocketAsyncEventArgs();

			sendEvent.RemoteEndPoint = endPoint;
			sendEvent.SetBuffer(buffer, 0, buffer.Length);
			sendEvent.Completed += (s, e) => waitHandle.Set();

			udpSocket.SendToAsync(sendEvent);
			if (sendEvent.SocketError != SocketError.Success)
			{
				throw new SocketException((int)sendEvent.SocketError);
			}
		}
		private static string ip = "111.222.3.444";// Change On Yours Mobile IP
		private const int port = 8083;
		private const int currentID = 1;
		private readonly StringBuilder data;

		private readonly IPEndPoint udpEndPoint;
		private readonly IPEndPoint serverEndPoint;
		private readonly IPEndPoint senderEndPoint;
		private readonly Socket udpSocket;

		private string _activitiesInfo;
		public string ActivitiesInfo
		{
			get => _activitiesInfo;
			set => SetProperty(ref _activitiesInfo, value);
		}
		public Command SendGreetingCommand { get; }
	}
}
