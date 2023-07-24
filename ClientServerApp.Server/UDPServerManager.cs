using System.Net.Sockets;
using System.Net;
using System.Text;
using ClientServerApp.Services.Helpers;
using ClientServerApp.Common;
using ClientServerApp.Database.Models;
using Newtonsoft.Json;

namespace ClientServerApp.Server
{
	public class UDPServerManager
	{
		// Put to the bottom of the project
		private static List<ClientData> _connectedClients;
		private static string _ip;// Change on your server IP  <-- Add to the github with updated desctiprion
		private const int _port = 8081;
		private readonly Action<string> _activitiesInfo;

		private readonly IPEndPoint _udpEndPoint;
		private readonly Socket _udpSocket;
		private static EndPoint _senderEndPoint;
		private static IPEndPoint _lastEndPoint;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="activitiesInfo">It`s a property that will display activities info on UI</param>
		public UDPServerManager(Action<string> activitiesInfo)
		{
			_activitiesInfo = activitiesInfo;
			_connectedClients = new List<ClientData>();
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file

			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);

			StartListening();
		}
		private async void StartListening()
		{
			await Console.Out.WriteLineAsync("<-=-=-=-=-= Server started =-=-=-=-=->");
			CheckStatus();
			while (true)
			{
				try
				{
					var buffer = new byte[256];
					var data = new StringBuilder();
					var request = new RequestData();

					_senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					do
					{
						var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						_senderEndPoint = result.RemoteEndPoint;

						request = JsonConvert.DeserializeObject<RequestData>(Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes));
						var methods = new Dictionary<string, Func<Task>>
						{
							{ "Greeting", Greeting },
							{ "Connecting", Connecting },
							{ "Alive", Alive },
						};

						await methods[request.ActionName]();
					}
					while (_udpSocket.Available > 0);
				}
				catch (SocketException ex)
				{
					await MakeNotAlive(_lastEndPoint);
				}
			}
		}
		private async Task Alive()
		{
			await Console.Out.WriteLineAsync($"{_senderEndPoint} is alive");
		}

		private async Task Connecting()
		{
			string connectionMessage = "";
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(_senderEndPoint))
				{
					await Console.Out.WriteLineAsync("Xamarin client connected");
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.True }.ToString();//.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsMobileClientConnected = true;
					if (client.IsDekstopClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//"Xamarin client connected"
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToString();//.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//"Wpf client connected" 
					}
					else
					{
						connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);
					}
				}
				else if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					await Console.Out.WriteLineAsync("Wpf client connected");
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsDekstopClientConnected = true;
					if (client.IsMobileClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToString();//.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//"Wpf client connected"
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);// "Xamarin client connected"
					}
					else
					{
						connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);
					}
				}
				else
				{
					await Console.Out.WriteLineAsync("Unknown client connected");
				}
			}
		}
		private async Task Greeting() // Make method for searching (replcace foreach and if dublication in two methods)
		{
			string greetingMsg;
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsDekstopClientConnected)
					{
						greetingMsg = new RequestData() { Id=0, ActionName=RequestActions.Greeting, Message="Xamarin client is Greeting you" }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), client.DekstopEndPoint);//"Xamarin client is Greeting you"
					}
					else
					{
						greetingMsg = new RequestData { Id=0, ActionName= RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), client.MobileEndPoint);//"Wpf client is not connected"
					}
				}
				else if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsMobileClientConnected)
					{
						greetingMsg = new RequestData() { Id=0, ActionName = RequestActions.Greeting, Message="Wpf client is Greeting you" }.ToString();//.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), client.MobileEndPoint);//"Wpf client is Greeting you"
					}
					else
					{
						greetingMsg = new RequestData { Id=0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), client.DekstopEndPoint);//"Xamarin client is not connected"
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		private async Task CheckStatus()
		{
			while (true)
			{
				foreach (var client in _connectedClients.Where(c => c.IsMobileClientConnected || c.IsDekstopClientConnected))
				{
					if (client.IsMobileClientConnected && client.IsDekstopClientConnected)
					{
						await SendChecker(client, client.MobileEndPoint, true);
						await SendChecker(client, client.DekstopEndPoint, false);
					}
					else if (client.IsMobileClientConnected)
					{
						await SendChecker(client, client.MobileEndPoint, true);
					}
					else
					{
						await SendChecker(client, client.DekstopEndPoint, false);
					}
				}
				await Task.Delay(10_000);// 10 seconds
			}
		}

		private async Task SendChecker(ClientData client, IPEndPoint clientEndPoint, bool isForXamarin)
		{
			string isAliveMsg = "";
			_lastEndPoint = clientEndPoint;
			if (isForXamarin)
			{
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToString();//.ToJson()
			}
			else
			{
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToJson();
			}
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(isAliveMsg), clientEndPoint);
			await Console.Out.WriteLineAsync("Send");
		}
		private async Task MakeNotAlive(IPEndPoint clientEndPoint)
		{
			string disconectedMsg;
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(clientEndPoint))
				{
					client.IsMobileClientConnected = false;
					await Console.Out.WriteLineAsync("Xamarin client disconected");
					if (client.IsDekstopClientConnected)
					{
						disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(disconectedMsg), client.DekstopEndPoint);//"Xamarin client disconected"
					}
				}
				else if (client.DekstopEndPoint.Equals(clientEndPoint))
				{
					client.IsDekstopClientConnected = false;
					await Console.Out.WriteLineAsync("Wpf client disconected");
					if (client.IsDekstopClientConnected)
					{
						disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(disconectedMsg), client.MobileEndPoint);//"Wpf client disconected"
					}
				}
				else
				{
					await Console.Out.WriteLineAsync("Unknown client");
				}
			}
		}
		private static void GetIPFromFile(string path)// Make this method async
		{
			string[] lines = File.ReadAllLines(path);
			_ip = lines[0];
			_connectedClients.Add(new ClientData
			{
				Id = 1,
				DekstopEndPoint = new IPEndPoint(IPAddress.Parse(lines[1]), 8082),
				MobileEndPoint = new IPEndPoint(IPAddress.Parse(lines[2]), 8083),
				IsMobileClientConnected = false,
				IsDekstopClientConnected = false
			});
		}
		private static string GetLocalIP()// try to make this method async
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
	}
}