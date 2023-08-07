using ClientServerApp.Common.Constants;
using ClientServerApp.Database.Models;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerApp.Server
{
	public class UDPServerManager
	{
		public UDPServerManager(Action<string> activitiesInfo)
		{
			_connectedClients = new List<ClientData>();
			_clientTimers = new Dictionary<EndPoint, System.Timers.Timer>();
			_activitiesInfo = activitiesInfo;
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");

			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);

			StartListening();
			CheckClientStatus();
		}
		/// <summary>
		/// List with all registered Clients
		/// </summary>
		private static List<ClientData> _connectedClients;
		/// <summary>
		/// IP of current client
		/// </summary>
		private static string _ip;
		/// <summary>
		/// Port of current client
		/// </summary>
		private const int _port = 8081;
		/// <summary>
		/// Responsible for displaying info in UI
		/// </summary>
		private readonly Action<string> _activitiesInfo;
		/// <summary>
		/// Responsible for monitoring the online status of clients
		/// </summary>
		private Dictionary<EndPoint, System.Timers.Timer> _clientTimers;
		/// <summary>
		/// EndPoint of current client
		/// </summary>
		private readonly IPEndPoint _udpEndPoint;
		/// <summary>
		/// Sender`s endpoint
		/// </summary>
		private static EndPoint _senderEndPoint;
		/// <summary>
		/// Client's endpoint for which the last time a request was sent to see if it is online
		/// </summary>
		private static IPEndPoint _lastEndPoint;
		/// <summary>
		/// Socket of current client
		/// </summary>
		private readonly Socket _udpSocket;
		/// <summary>
		/// Start listening for incoming requests.
		/// </summary>
		/// <returns></returns>
		private async Task StartListening()
		{
			try
			{
				await Console.Out.WriteLineAsync("<-=-=-=-=-= Server started =-=-=-=-=->");
				while (true)
				{
					var buffer = new byte[NetworkConfig.BUFFER_SIZE];
					var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
					_senderEndPoint = result.RemoteEndPoint;
					var request = JsonConvert.DeserializeObject<RequestData>(Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes));

					await ProcessRequest(request);
				}
			}
			catch (SocketException ex)
			{
				await MakeClientNotAlive(_lastEndPoint);
			}
		}
		/// <summary>
		/// Process the incoming request based on its action.
		/// </summary>
		/// <param name="request">Request that comes from clients</param>
		/// <returns></returns>
		private async Task ProcessRequest(RequestData request)
		{
			var methods = new Dictionary<string, Func<Task>>
			{
				{ RequestActions.Greeting, async() =>  Greeting() },
				{ RequestActions.Connecting, async() =>  Connecting(_senderEndPoint) },
				{ RequestActions.Alive, async () =>  Alive(_senderEndPoint) },
			};

			if (methods.TryGetValue(request.ActionName, out var method))
			{
				await method.Invoke();
			}
			else
			{
				await Console.Out.WriteLineAsync($"Unknown action: {request.ActionName}, from {_senderEndPoint.AddressFamily}");
			}
		}
		
		private async Task Alive(EndPoint senderEndPoint)
		{
			if(_clientTimers.ContainsKey(senderEndPoint))
			{
				_clientTimers[senderEndPoint].Stop();
				_clientTimers[senderEndPoint].Start();
			}
			await Console.Out.WriteLineAsync($"{_senderEndPoint} is alive");
		}
		private ClientData GetClientDataByEndPoint(EndPoint endPoint) =>
			_connectedClients.Find(client => client.MobileEndPoint.Equals(endPoint) || client.DekstopEndPoint.Equals(endPoint));
		
		private async Task Connecting(EndPoint senderEndPoint)
		{
			string connectionMessage;
			var clientData = GetClientDataByEndPoint(senderEndPoint);

			if (clientData.MobileEndPoint.Equals(senderEndPoint))
			{
				await Console.Out.WriteLineAsync("Xamarin client connected");

				var timer = new System.Timers.Timer();
				timer.Interval = 15_000;// miliseconds
				timer.Elapsed += (sender, e) => MakeClientNotAlive(clientData.MobileEndPoint);
				timer.Start();
				_clientTimers[senderEndPoint] = timer;

				connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.True }.ToJson();
				_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.MobileEndPoint);
				clientData.IsMobileClientConnected = true;
				if (clientData.IsDekstopClientConnected)
				{
					connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.DekstopEndPoint);
					connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.MobileEndPoint);
				}
				else
				{
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.MobileEndPoint);
				}
			}
			else if (clientData.DekstopEndPoint.Equals(senderEndPoint))
			{
				await Console.Out.WriteLineAsync("Wpf client connected");

				var timer = new System.Timers.Timer();
				timer.Interval = 15_000;// miliseconds
				timer.Elapsed += (sender, e) => MakeClientNotAlive(clientData.DekstopEndPoint);
				timer.Start();
				_clientTimers[senderEndPoint] = timer;

				connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.True }.ToJson();
				_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.DekstopEndPoint);
				clientData.IsDekstopClientConnected = true;
				if (clientData.IsMobileClientConnected)
				{
					connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.MobileEndPoint);
					connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.DekstopEndPoint);
				}
				else
				{
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), clientData.DekstopEndPoint);
				}
			}
			else
			{
				await Console.Out.WriteLineAsync("Unknown client connected");
			}

		}

		private async Task Greeting()
		{
			string greetingMsg;
			var clientData = GetClientDataByEndPoint(_senderEndPoint);

			if (clientData.MobileEndPoint.Equals(_senderEndPoint))
			{
				if (clientData.IsDekstopClientConnected)
				{
					greetingMsg = new RequestData() { Id=0, ActionName=RequestActions.Greeting, Message="Xamarin client is Greeting you" }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), clientData.DekstopEndPoint);
				}
				else
				{
					greetingMsg = new RequestData { Id=0, ActionName= RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), clientData.MobileEndPoint);
				}
			}
			else if (clientData.DekstopEndPoint.Equals(_senderEndPoint))
			{
				if (clientData.IsMobileClientConnected)
				{
					greetingMsg = new RequestData() { Id=0, ActionName = RequestActions.Greeting, Message="Wpf client is Greeting you" }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), clientData.MobileEndPoint);
				}
				else
				{
					greetingMsg = new RequestData { Id=0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), clientData.DekstopEndPoint);
				}
			}
			else
			{
				throw new Exception("Client is not recognized");
			}

		}
		private async Task CheckClientStatus()
		{
			while (true)
			{
				foreach (var client in _connectedClients)
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
					else if (client.IsDekstopClientConnected)
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
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToJson();
			}
			else
			{
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToJson();
			}
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(isAliveMsg), clientEndPoint);
			await Console.Out.WriteLineAsync("Send");
		}

		private async Task MakeClientNotAlive(IPEndPoint clientEndPoint)
		{
			var clientData = GetClientDataByEndPoint(clientEndPoint);
			string disconectedMsg;

			if (clientData.MobileEndPoint.Equals(clientEndPoint))
			{
				clientData.IsMobileClientConnected = false;
				await Console.Out.WriteLineAsync("Xamarin client disconected");
				_clientTimers[clientData.MobileEndPoint].Stop();

				if (clientData.IsDekstopClientConnected)
				{
					disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(disconectedMsg), clientData.DekstopEndPoint);
				}
			}
			else if (clientData.DekstopEndPoint.Equals(clientEndPoint))
			{
				clientData.IsDekstopClientConnected = false;
				await Console.Out.WriteLineAsync("Wpf client disconected");
				_clientTimers[clientData.DekstopEndPoint].Stop();

				if (clientData.IsDekstopClientConnected)
				{
					disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(disconectedMsg), clientData.MobileEndPoint);
				}
			}
			else
			{
				await Console.Out.WriteLineAsync("Unknown client");
			}

		}
		private void GetIPFromFile(string path)
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
	}
}