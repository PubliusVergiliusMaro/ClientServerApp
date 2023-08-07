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
		public UDPClientManager(Action<string> activitiesInfo, Action<byte[]> imageData)
		{
			_receivedChunks = new Dictionary<int, byte[]>();
			_imageData = imageData;
			GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file
			_activitiesInfo = activitiesInfo;
			_data = new StringBuilder();
			_stopwatch = new Stopwatch();

			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);
			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), 8081);
			_mobileEndPoint = new IPEndPoint(IPAddress.Parse(_mobileIp), 8083);
			var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "" }.ToJson();
			_udpSocket.SendTo(Encoding.UTF8.GetBytes(connectingMessage), _serverEndPoint);

			StartListening();
		}
		/// <summary>
		/// IP of this client
		/// </summary>
		private static string _ip;
		/// <summary>
		/// Port of this client
		/// </summary>
		private static int _port = 8082;
		/// <summary>
		/// Server`s IP
		/// </summary>
		private static string _serverIp;
		/// <summary>
		/// Mobile client`s IP
		/// </summary>
		private static string _mobileIp;
		/// <summary>
		/// Id of this client
		/// </summary>
		private readonly int _clientId;
		/// <summary>
		/// StringBuilder of saving activities info
		/// </summary>
		private readonly StringBuilder _data;
		/// <summary>
		/// Dictionary for screenshot in bytes
		/// </summary>
		private static Dictionary<int, byte[]> _receivedChunks;
		/// <summary>
		/// Responsible for displaying info in UI
		/// </summary>
		private static Action<string> _activitiesInfo;
		/// <summary>
		/// Responsible for displaying image in UI
		/// </summary>
		private static Action<byte[]> _imageData;
		/// <summary>
		/// Stopwatch for calculation time of getting image
		/// </summary>
		private readonly Stopwatch _stopwatch;
		/// <summary>
		/// Id of current screenshot
		/// </summary>
		private static int _currentImageId;
		/// <summary>
		/// EndPoint of current client
		/// </summary>
		private readonly IPEndPoint _udpEndPoint;
		/// <summary>
		/// Server`s endpoint
		/// </summary>
		private readonly IPEndPoint _serverEndPoint;
		/// <summary>
		/// Sender`s endpoint
		/// </summary>
		private readonly IPEndPoint _senderEndPoint;
		/// <summary>
		/// Mobile client`s endpoint
		/// </summary>
		private readonly IPEndPoint _mobileEndPoint;
		/// <summary>
		/// Socket of current client
		/// </summary>
		private readonly Socket _udpSocket;

		private async Task StartListening()
		{
			while (true)
			{
				do
				{
					EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
					var buffer = new byte[NetworkConfig.BUFFER_SIZE];

					var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
					senderEndPoint = result.RemoteEndPoint;
					var answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
					var request = JsonConvert.DeserializeObject<RequestData>(answer);

					await ProccessRequest(request);
				}
				while (_udpSocket.Available > 0);
			}
		}
		private async Task ProccessRequest(RequestData request)
		{
			var methods = new Dictionary<string, Func<Task>>
			{
				{RequestActions.IsAlive, async() =>  SendAliveStatus()},
				{RequestActions.WpfConnectionStatus, async() => DisplayConnectionStatus(bool.Parse(request.Message))},
				{RequestActions.XamarinConnectionStatus, async() => XamarinConnectionStatus(bool.Parse(request.Message))},
				{RequestActions.Greeting, async() => GetGreeting(request.Message)},
				{RequestActions.PreparedImage, async() => ReadyForGetting(request.TotalChunks,request.ImageId) },
				{RequestActions.SendChunk, async() => GetImageChunk(request.Image,request.ChunkNumber,request.TotalChunks,request.ImageId) },
			};

			if (methods.TryGetValue(request.ActionName, out var method))
			{
				await method.Invoke();
			}
			else
			{
				AppendData($"Unknown action: {request.ActionName}, from {_senderEndPoint.AddressFamily}");
			}

		}
		private void GetGreeting(string message) => AppendData(message);
		private void XamarinConnectionStatus(bool successfulStatus)
		{
			AppendData(successfulStatus ? "Xamarin client is connected" : "Xamarin client is not connected");
		}

		private void DisplayConnectionStatus(bool successfulStatus)
		{
			AppendData(successfulStatus ? "Successfully connected" : "Connection failed");
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
			_mobileIp = lines[2];
		}
		/// <summary>
		/// Starts sending requests on image chunks
		/// </summary>
		/// <param name="totalChunks"></param>
		/// <param name="imageId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private async Task ReadyForGetting(int? totalChunks, int? imageId)
		{
			AppendData($"Image prepared. Starting getting image. {totalChunks} chunks");
			if (imageId <= 100_000 && imageId >= 999_999)
				throw new ArgumentException("Error with imageId. It comes not in correct format");

			_currentImageId = (int)imageId;
			for (int i = 0; i < totalChunks; i++)
				_receivedChunks[i] = new byte[0];
			AskChunks();
		}

		public async Task AskForImage()
		{
			_stopwatch.Start();
			_receivedChunks = new Dictionary<int, byte[]>();
			var askMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.SendMeImage, Message = "" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(askMessage), _mobileEndPoint);
			AppendData("Send ask");
		}
		/// <summary>
		/// Get chunk of image
		/// </summary>
		/// <param name="chunk">Array with a chunk</param>
		/// <param name="chunkNumber">Number of this chunk in a Dicitionary</param>
		/// <param name="totalChunks">Total number of chunks</param>
		/// <param name="imageId">Id of Image for what is this chunk</param>
		/// <returns></returns>
		private async Task GetImageChunk(byte[] chunk, int? chunkNumber, int? totalChunks, int? imageId)
		{
			if (_receivedChunks != null)
			{
				if (imageId != _currentImageId || _receivedChunks[(int)chunkNumber].Length > 0)
				{
					return;
				}
				_receivedChunks[(int)chunkNumber] = chunk;

				if (_receivedChunks.Where(chunk => chunk.Value.Length > 0).Count() == totalChunks)
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
						_imageData(assembledImageData);
					}
					AppendData($"Succesfully Get Image Id:{_currentImageId}");
					_stopwatch.Stop();
					AppendData($"Time consumed: {_stopwatch.Elapsed.TotalMilliseconds} ms");
					_receivedChunks = null;
				}
				else
					AskChunks();
			}
		}
		/// <summary>
		/// Sends request on empty chunks
		/// </summary>
		/// <returns></returns>
		private async Task AskChunks()
		{
			List<int> requaredChunks = new List<int>();
			foreach (var chunk in _receivedChunks.Where(val => val.Value.Length == 0))
			{
				requaredChunks.Add(chunk.Key);
			}
			var message = new RequestData { Id = _clientId, ActionName = RequestActions.GetChunk, ChunkNumbers =  requaredChunks.ToArray(), ImageId = _currentImageId }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(message), _mobileEndPoint);
		}
		public async Task SendGreeting()
		{
			var greetingMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.Greeting, Message = "" }.ToJson();
			await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(greetingMessage), _mobileEndPoint);
		}
	}
}
