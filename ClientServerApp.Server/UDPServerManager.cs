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
		// Put to the bottom of the project
		private static List<ClientData> _connectedClients;
		private Dictionary<EndPoint, System.Timers.Timer> _clientTimers;
		private static Stopwatch timer;

		private static string _ip;// Change on your server IP
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
			_clientTimers = new Dictionary<EndPoint, System.Timers.Timer>();
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
					var data = new StringBuilder();
					var request = new RequestData();
					var result = new SocketReceiveFromResult();
					_senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					do
					{
						var buffer = new byte[4096];
						result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						_senderEndPoint = result.RemoteEndPoint;
						request = JsonConvert.DeserializeObject<RequestData>(Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes));

						var methods = new Dictionary<string, Func<Task>> 
						{
							{ RequestActions.Greeting, Greeting },
							{ "Connecting", Connecting },
							{ RequestActions.Alive, Alive },
							{ RequestActions.SendMeImage, AskForImage},
							{ RequestActions.PreparedImage, () => PreparedImage(request.TotalChunks) },
							{ RequestActions.GetChunk, async() => await GetChunk(request.ChunkNumber) },
							{ RequestActions.SendChunk, async() => await SendChunk(request.Image,request.ChunkNumber,request.TotalChunks) }
						};
						await methods[request.ActionName]();
					}
					while (_udpSocket.Available > 0);
				}
				catch (SocketException ex)
				{
					//await MakeNotAlive(_lastEndPoint);
				}
			}
		}

		private async Task SendChunk(byte[]? image, int? chunkNumber, int? totalChunks)
		{
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsDekstopClientConnected)
					{
						var message = new RequestData() { Id= 1, ActionName=RequestActions.SendChunk, Message = "", Image = image, ChunkNumber = chunkNumber, TotalChunks = totalChunks }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(message), client.DekstopEndPoint);
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		private async Task GetChunk(int? chunkNumber)
		{
			var message = new RequestData { Id = 0, ActionName=RequestActions.GetChunk, ChunkNumber=chunkNumber }.ToJson();
			foreach (var client in _connectedClients)
			{
				if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsMobileClientConnected)
					{
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(message), client.MobileEndPoint);//"Wpf client is Greeting you"
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		public async Task PreparedImage(int? totalChunks)
		{
			string resultMessage;
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsDekstopClientConnected)
					{
						resultMessage = new RequestData() { Id = 0, ActionName = RequestActions.PreparedImage, TotalChunks = totalChunks }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(resultMessage), client.DekstopEndPoint);
						await Console.Out.WriteLineAsync($"Succesfully Prepered {totalChunks} chunks.");
					}
					else
					{
						resultMessage = new RequestData { Id=0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(resultMessage), client.MobileEndPoint);//"WPF client is not connected"
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		public async Task AskForImage()
		{
			string askMessage;
			foreach (var client in _connectedClients)
			{
				if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsMobileClientConnected)
					{
						askMessage = new RequestData() { Id = 0, ActionName = RequestActions.SendMeImage, Message = "" }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(askMessage), client.MobileEndPoint);//"Wpf client is Greeting you"
						await Console.Out.WriteLineAsync("Send Request On Image");
					}
					else
					{
						askMessage = new RequestData { Id=0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(askMessage), client.DekstopEndPoint);//"Xamarin client is not connected"
						await Console.Out.WriteLineAsync("Denied Request");
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		private async Task Alive()
		{
			if (_clientTimers.ContainsKey(_senderEndPoint))
			{
				// Reset the timer for this client
				_clientTimers[_senderEndPoint].Stop();
				_clientTimers[_senderEndPoint].Start();
				
			}
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

					var timer = new System.Timers.Timer();
					timer.Interval = 5_000;
					timer.Elapsed += (sender, e) => MakeNotAlive(client.MobileEndPoint);
					timer.Start();

					_clientTimers[_senderEndPoint] = timer;

					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.True }.ToJson();//.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsMobileClientConnected = true;
					if (client.IsDekstopClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//"Xamarin client connected"
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToJson();//.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//"Wpf client connected" 
					}
					else
					{
						connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();//.ToJson();
						_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);
					}
				}
				else if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					await Console.Out.WriteLineAsync("Wpf client connected");
					
					var timer = new System.Timers.Timer();
					timer.Interval = 5_000;
					timer.Elapsed += (sender, e) => MakeNotAlive(client.DekstopEndPoint);
					timer.Start();
					_clientTimers[_senderEndPoint] = timer;

					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.True }.ToJson();
					_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsDekstopClientConnected = true;
					if (client.IsMobileClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToJson();//.ToJson();
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
						greetingMsg = new RequestData { Id=0, ActionName= RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();//.ToJson();
						await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMsg), client.MobileEndPoint);//"Wpf client is not connected"
					}
				}
				else if (client.DekstopEndPoint.Equals(_senderEndPoint))
				{
					if (client.IsMobileClientConnected)
					{
						greetingMsg = new RequestData() { Id=0, ActionName = RequestActions.Greeting, Message="Wpf client is Greeting you" }.ToJson();//.ToJson();
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
				await Task.Delay(2_000);// 10 seconds
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
		private async Task MakeNotAlive(IPEndPoint clientEndPoint)
		{
			string disconectedMsg;
			foreach (var client in _connectedClients)
			{
				if (client.MobileEndPoint.Equals(clientEndPoint))
				{
					client.IsMobileClientConnected = false;
					await Console.Out.WriteLineAsync("Xamarin client disconected");
					_clientTimers[client.MobileEndPoint].Stop();
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
					_clientTimers[client.DekstopEndPoint].Stop();
					if (client.IsDekstopClientConnected)
					{
						disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToJson();
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
	}
}