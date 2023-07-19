using ClientServerApp.Common;
using ClientServerApp.Database.Models;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.UDP
{
	public class Program
	{
		private static List<ClientData> connectedClients;
		private static string ip;
		private const int port = 8081;

		private static IPEndPoint udpEndPoint;
		private static Socket udpSocket;
		private static EndPoint senderEndPoint;
		private static IPEndPoint lastSended;

		static void Main(string[] args)
		{
			connectedClients = new List<ClientData>();
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file

			udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSocket.Bind(udpEndPoint);
			StartListening();

			Console.ReadKey();
		}
		static async void StartListening()
		{
			await Console.Out.WriteLineAsync("<-=-=-=-=-= Server started =-=-=-=-=->");
			CheckStatus();
			while (true)
			{
				try
				{
					var buffer = new byte[256];
					var size = 0;
					var data = new StringBuilder();
					var request = new RequestData();

					senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					do
					{
						size = udpSocket.ReceiveFrom(buffer, ref senderEndPoint);
						IPEndPoint tmpSender = (IPEndPoint)senderEndPoint;
						if (tmpSender.Address == IPAddress.None)
						{
							MakeNotAlive(lastSended);
						}
						request = JsonConvert.DeserializeObject<RequestData>(Encoding.UTF8.GetString(buffer));
						var methods = new Dictionary<string, Action>
						{
							{ "Greeting", Greeting },
							{ "Connecting", Connecting },
							{ "Alive", Alive }
						};

						methods[request.ActionName]();
					}
					while (udpSocket.Available > 0);
				}
				catch (SocketException ex)
				{
					MakeNotAlive(lastSended);
				}
			}
		}
		private static void Alive()
		{
			Console.WriteLine($"{senderEndPoint} is alive");
		}

		static void Connecting()
		{
			string connectionMessage = "";
			foreach (var client in connectedClients)
			{
				if (client.MobileEndPoint.Equals(senderEndPoint))
				{
					Console.WriteLine("Xamarin client connected");
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.True }.ToString();//.ToJson();
					udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsMobileClientConnected = true;
					if (client.IsDekstopClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//"Xamarin client connected"
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//"Wpf client connected" 
					}
					else
					{
						connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);
					}
				}
				else if (client.DekstopEndPoint.Equals(senderEndPoint))
				{
					Console.WriteLine("Wpf client connected");
					connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.WpfConnectionStatus, Message=RequestMessages.True }.ToJson();
					udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);//Succesfully connected"), client.MobileEndPoint);
					client.IsDekstopClientConnected = true;
					if (client.IsMobileClientConnected)
					{
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.WpfConnectionStatus, Message = RequestMessages.True }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.MobileEndPoint);//"Wpf client connected"
						connectionMessage = new RequestData { Id=0, ActionName=RequestActions.XamarinConnectionStatus, Message = RequestMessages.True }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);// "Xamarin client connected"
					}
					else
					{
						connectionMessage = new RequestData { Id = 0, ActionName=RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(connectionMessage), client.DekstopEndPoint);
					}
				}
				else
				{
					Console.WriteLine("Unknown client connected");
				}
			}
		}
		static void Greeting()
		{
			string greetingMsg;
			foreach (var client in connectedClients)
			{
				if (client.MobileEndPoint.Equals(senderEndPoint))
				{
					if (client.IsDekstopClientConnected)
					{
						greetingMsg = new RequestData() { Id=0, ActionName=RequestActions.Greeting, Message="Xamarin client is Greeting you" }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(greetingMsg), client.DekstopEndPoint);//"Xamarin client is Greeting you"
					}
					else
					{
						greetingMsg = new RequestData { Id=0, ActionName= RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(greetingMsg), client.MobileEndPoint);//"Wpf client is not connected"
					}
				}
				else if (client.DekstopEndPoint.Equals(senderEndPoint))
				{
					if (client.IsMobileClientConnected)
					{
						greetingMsg = new RequestData() { Id=0, ActionName = RequestActions.Greeting, Message="Wpf client is Greeting you" }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(greetingMsg), client.MobileEndPoint);//"Wpf client is Greeting you"
					}
					else
					{
						greetingMsg = new RequestData { Id=0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(greetingMsg), client.DekstopEndPoint);//"Xamarin client is not connected"
					}
				}
				else
				{
					throw new Exception("Client is not recognized");
				}
			}
		}
		static async Task CheckStatus()
		{
			while (true)
			{
				foreach (var client in connectedClients.Where(c => c.IsMobileClientConnected || c.IsDekstopClientConnected))
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

		static async Task SendChecker(ClientData client, IPEndPoint clientEndPoint, bool isForXamarin)
		{
			string isAliveMsg = "";
			lastSended = clientEndPoint;
			if (isForXamarin)
			{
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToString();//.ToJson()
			}
			else
			{
				isAliveMsg = new RequestData { Id = 0, ActionName = RequestActions.IsAlive, Message = RequestMessages.IsAlive }.ToJson();
			}
			udpSocket.SendTo(Encoding.UTF8.GetBytes(isAliveMsg), clientEndPoint);
			await Console.Out.WriteLineAsync("Send");
		}
		static void MakeNotAlive(IPEndPoint clientEndPoint)
		{
			string disconectedMsg;
			foreach (var client in connectedClients)
			{
				if (client.MobileEndPoint.Equals(clientEndPoint))
				{
					client.IsMobileClientConnected = false;
					Console.WriteLine("Xamarin client disconected");
					if (client.IsDekstopClientConnected)
					{
						disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.XamarinConnectionStatus, Message=RequestMessages.False }.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(disconectedMsg), client.DekstopEndPoint);//"Xamarin client disconected"
					}
				}
				else if (client.DekstopEndPoint.Equals(clientEndPoint))
				{
					client.IsDekstopClientConnected = false;
					Console.WriteLine("Wpf client disconected");
					if (client.IsDekstopClientConnected)
					{
						disconectedMsg = new RequestData { Id = 0, ActionName = RequestActions.WpfConnectionStatus, Message=RequestMessages.False }.ToString();//.ToJson();
						udpSocket.SendTo(Encoding.UTF8.GetBytes(disconectedMsg), client.MobileEndPoint);//"Wpf client disconected"
					}
				}
				else
				{
					Console.WriteLine("Unknown client");
				}
			}
		}
		private static void GetIPFromFile(string path)
		{
			string[] lines = File.ReadAllLines(path);
			ip = lines[0];
			connectedClients.Add(new ClientData
			{
				Id = 1,
				DekstopEndPoint = new IPEndPoint(IPAddress.Parse(lines[1]), 8082),
				MobileEndPoint = new IPEndPoint(IPAddress.Parse(lines[2]), 8083),
				IsMobileClientConnected = false,
				IsDekstopClientConnected = false
			});
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
	}
}