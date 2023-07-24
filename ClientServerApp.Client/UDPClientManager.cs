using ClientServerApp.Common;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerApp.Client
{
	public class UDPClientManager
	{
		// Put to the bottom of the project
		private static string _ip;
        private static int _port = 8082;
        private static string _serverIp;
        private readonly int _clientId;
        private readonly StringBuilder _data;
		/// <summary>
		/// Property that response for displaying info
		/// </summary>
		private static Action<string> _activitiesInfo;

        private readonly IPEndPoint _udpEndPoint;
        private readonly IPEndPoint _serverEndPoint;
        private readonly IPEndPoint _senderEndPoint;// Check if need it here
        private readonly Socket _udpSocket;
		// Give to the constructor ref and property that response for displaying data in the label 
		// It will change in the class and also will change in ViewModel
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="activitiesInfo">It`s a property that will display activities info on UI</param>
        public UDPClientManager(Action<string> activitiesInfo)
        {
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file
			_activitiesInfo = activitiesInfo;
            _data = new StringBuilder();

            _udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);
			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), 8081);
			
			//Можливо краще було б винести це в окремий метод але на рахунок SendToAsync треба глянути чи коректно воно прийме 
			// відповідь 
            var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "" }.ToJson();
			_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectingMessage), _serverEndPoint);

			StartListening();
		}

		private async Task StartListening()
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
						var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						senderEndPoint = result.RemoteEndPoint;

						answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
						request = JsonConvert.DeserializeObject<RequestData>(answer);
						var methods = new Dictionary<string, Action>
						{
							{RequestActions.IsAlive, async() => await SendAliveStatus()},
							{RequestActions.WpfConnectionStatus, () => DisplayConnectionStatus(bool.Parse(request.Message))},
							{RequestActions.XamarinConnectionStatus, () => XamarinConnectionStatus(bool.Parse(request.Message))},
							{RequestActions.Greeting, () => GetGreeting(request.Message)}
						};
						methods[request.ActionName]();
					}
					while (_udpSocket.Available > 0);
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
			var connectingMessage = new RequestData { Id = _clientId, ActionName=RequestActions.Alive, Message="I`m alive" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(connectingMessage), _serverEndPoint);
		}
		private void AppendData(string line)
		{
			_data.AppendLine(line);
			_activitiesInfo(_data.ToString());
		}
		private static void GetIPFromFile(string path)
		{
			string[] lines = File.ReadAllLines(path);
			_ip = lines[0];
			_serverIp = lines[1];
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
		public async Task SendGreeting()
		{
			var greetingMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.Greeting, Message = "" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMessage), _serverEndPoint);
		}
	}
}
