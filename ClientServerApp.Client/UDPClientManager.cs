using ClientServerApp.Common.Constants;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClientServerApp.Client
{
	public class UDPClientManager
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="activitiesInfo">It`s a property that will display activities info on UI</param>
		public UDPClientManager(Action<string> activitiesInfo, Action<byte[]> imageData)
		{
			_receivedChunks = new Dictionary<int, byte[]>();
			_imageData = imageData;
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file
			_activitiesInfo = activitiesInfo;
			_data = new StringBuilder();

			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);
			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), 8081);

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
					var buffer = new byte[4096];
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
							{RequestActions.Greeting, () => GetGreeting(request.Message)},
							{RequestActions.PreparedImage, async() => await ReadyForGetting(request.TotalChunks) },
							{RequestActions.SendChunk, async() => await GetImageChunk(request.Image,request.ChunkNumber,request.TotalChunks) },
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
		private void GetGreeting(string message) => AppendData(message);
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

		/// <summary>
		/// TODO: Use here stringbuilder
		/// </summary>
		/// <param name="line"></param>
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

		private async Task ReadyForGetting(int? totalChunks)
		{
			AppendData($"Image prepared. Starting getting image. {totalChunks} chunks");

			for (int i = 0; i < totalChunks; i++)
			{
				_receivedChunks[i] = new byte[0];
			}
			await GetImage(totalChunks);
		}

		private async Task GetImage(int? totalChunks)
		{
			int count = 0;
			while (true)
			{
				foreach (var chunk in _receivedChunks.Where(val => val.Value.Length == 0))
				{
					count++;
					var message = new RequestData { Id = _clientId, ActionName=RequestActions.GetChunk, ChunkNumber=chunk.Key }.ToJson();
					await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(message), _serverEndPoint);
				
					if (count == _receivedChunks.Where(val => val.Value.Length == 0).Count())
					{
						await Task.Delay(1);
						count = 0;
					}
				}
				if(_receivedChunks.Where(val => val.Value.Length == 0).Count() == 0)
				{
					break;
				}
			}
		}
		public async Task AskForImage()
		{
			var askMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.SendMeImage, Message = "" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(askMessage), _serverEndPoint);
			AppendData("Send ask");
		}
		private async Task GetImageChunk(byte[] chunk, int? chunkNumber, int? totalChunks)
		{
			int startIndex = (int)chunkNumber * ChunkData.IMAGE_CHUNK_MAX_SIZE;

			byte[] chunkData = new byte[chunk.Length];
			Array.Copy(chunk, 0, chunkData, 0, chunk.Length);

			_receivedChunks[(int)chunkNumber] = chunkData;

			//AppendData($"Loading image packets: {_receivedChunks.Where(c=>c.Value.Length >0).Count()}");

			if (_receivedChunks.Where(chunk=>chunk.Value.Length > 0).Count() == totalChunks)
			{
				byte[] assembledImageData;
				using (MemoryStream imgStream = new MemoryStream())
				{
					for (int i = 0; i < totalChunks; i++)
					{
						byte[] tmpdata = _receivedChunks[i];
						await imgStream.WriteAsync(tmpdata, 0, tmpdata.Length);
					}
					assembledImageData = imgStream.ToArray();

					//AppendData("Succesfully Get Image");
					_imageData(assembledImageData);
					_receivedChunks = new Dictionary<int, byte[]>();
				}
			}
		}
		public async Task SendGreeting()
		{
			var greetingMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.Greeting, Message = "" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMessage), _serverEndPoint);
		}

		private static string _ip;
		private static int _port = 8082;
		private static string _serverIp;
		private readonly int _clientId;
		private readonly StringBuilder _data;
		private static Dictionary<int, byte[]> _receivedChunks;
		private static Stopwatch timer;
		/// <summary>
		/// Property that response for displaying info
		/// </summary>
		private static Action<string> _activitiesInfo;
		private static Action<byte[]> _imageData;

		private readonly IPEndPoint _udpEndPoint;
		private readonly IPEndPoint _serverEndPoint;
		private readonly IPEndPoint _senderEndPoint;// Check if need it here
		private readonly Socket _udpSocket;


	}
}
