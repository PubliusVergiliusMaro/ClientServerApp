using ClientServerApp.Mobile.Common;
using ClientServerApp.Mobile.Helpers;
using ClientServerApp.Mobile.Services.ScreenshotServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ClientServerApp.Mobile.Client
{
	internal class UDPClientManager
	{
		public UDPClientManager(Action<string> activitiesInfo)
		{
			_preparedImage = new Dictionary<int, byte[]>();
			_activitiesInfo=activitiesInfo;
			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);

			_dekstopEndPoint = new IPEndPoint(IPAddress.Parse(_dekstopIp), _dekstopPort);
			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);
			var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "message" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
			StartListening();
		}
		/// <summary>
		/// IP of a current client
		/// </summary>
		private static string _ip = "192.168.1.103";// Change On Yours Mobile IP
		/// <summary>
		/// IP of a server
		/// </summary>
		private static string _serverIp = "192.168.1.104"; // Change On Server IP
		/// <summary>
		/// IP of a dekstop client
		/// </summary>
		private static string _dekstopIp = "192.168.1.104"; // Change On Your Dekstop IP
		/// <summary>
		/// Port of a current client 
		/// </summary>
		private static int _port = 8083;
		/// <summary>
		/// Server`s port
		/// </summary>
		private static int _serverPort = 8081;
		/// <summary>
		/// Dekstop client`s port
		/// </summary>
		private static int _dekstopPort = 8082;
		/// <summary>
		/// Id of current client
		/// </summary>
		private static int _clientId;
		/// <summary>
		/// Id of the last screenshot taken 
		/// </summary>
		private static int _currentImageId;
		/// <summary>
		/// Field that in UI displays data 
		/// </summary>
		private readonly Action<string> _activitiesInfo;
		/// <summary>
		/// Dictionary with an screenshot that will be sent
		/// </summary>
		private static Dictionary<int, byte[]> _preparedImage;
		/// <summary>
		/// EndPoint of current client
		/// </summary>
		private readonly IPEndPoint _udpEndPoint;
		/// <summary>
		/// Server`s endpoint
		/// </summary>
		private readonly IPEndPoint _serverEndPoint;
		/// <summary>
		/// Dekstop client`s endpoint
		/// </summary>
		private readonly IPEndPoint _dekstopEndPoint;
		/// <summary>
		/// Socket of current client
		/// </summary>
		private readonly Socket _udpSocket;

		private async Task StartListening()
		{
			while (true)
			{
				var buffer = new byte[NetworkConfig.BUFFER_SIZE];
				var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
				var answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
				var request = JsonConvert.DeserializeObject<RequestData>(answer);

				Task.Run(() => HandleRequest(request));
			}
		}
		private void HandleRequest(RequestData request)
		{
			var methods = new Dictionary<string, Action>
			{
				{ RequestActions.IsAlive, async() => SendAliveStatus() },
				{ RequestActions.XamarinConnectionStatus, async() => DisplayConnectionStatus(request.Message) },
				{ RequestActions.WpfConnectionStatus, async() => WpfConnectionStatus(request.Message) },
				{ RequestActions.Greeting,async () => GetGreeting(request.Message) },
				{ RequestActions.SendMeImage, async() => PrepareImage() },
				{ RequestActions.GetChunk, async() => SendChunk(request.ChunkNumbers, request.ImageId) }
			};

			if (methods.TryGetValue(request.ActionName, out var action))
			{
				action();
			}
			else
			{
				_activitiesInfo($"Unknown action: {request.ActionName}");
			}
		}
		private async void SendChunk(int[] chunkNumbers, int imageId)
		{
			if (_currentImageId != imageId)
				return;
			
			foreach (var number in chunkNumbers)
			{
				byte[] neededChunk = _preparedImage[(int)number];
				var message = new RequestData() { Id= 1, ActionName=RequestActions.SendChunk, Message = "", Image = neededChunk, ChunkNumber = number, TotalChunks = _preparedImage.Count, ImageId = _currentImageId }.ToJson();
				SendData(message, _dekstopEndPoint);
			};

		}
		public async void PrepareImage()
		{
			byte[] screenshotBytes = DependencyService.Get<IScreenshotService>().CaptureScreen();

			if (screenshotBytes.Length > 10)
			{
				int totalChunks = (int)Math.Ceiling((double)screenshotBytes.Length/ChunkConfig.IMAGE_CHUNK_MAX_SIZE);

				for (int chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
				{
					int startIndex = chunkNumber * ChunkConfig.IMAGE_CHUNK_MAX_SIZE;
					int remainingBytes = screenshotBytes.Length - startIndex;
					int chunkSize = Math.Min(ChunkConfig.IMAGE_CHUNK_MAX_SIZE, remainingBytes);
					byte[] chunkData = new byte[chunkSize];
					Array.Copy(screenshotBytes, startIndex, chunkData, 0, chunkSize);

					_preparedImage[(int)chunkNumber] = chunkData;
					if (_preparedImage.Count == totalChunks)
					{
						var tmpImageId = _currentImageId;
						_currentImageId = new Random().Next(100000, 999999);
						if (tmpImageId == _currentImageId)
							_currentImageId = new Random().Next(100000, 999999);

						var succesMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.PreparedImage, TotalChunks = totalChunks, ImageId = _currentImageId }.ToJson();
						SendData(succesMessage, _dekstopEndPoint);
					}
				}
			}
			else
			{
				_activitiesInfo("Screenshot is empty");
			}
		}

		private async void GetGreeting(string message)
		{
			_activitiesInfo(message);
		}

		private async void WpfConnectionStatus(string successfulStatus)
		{
			var message = successfulStatus == RequestMessages.True ? "Wpf client is connected" :
						  successfulStatus == RequestMessages.False ? "Wpf client is not connected" :
						  throw new Exception("Status is not equal true/false format.");

			_activitiesInfo(message);
		}

		private async void DisplayConnectionStatus(string successfulStatus)
		{
			var message = successfulStatus == RequestMessages.True ? "Successfully connected" :
						  successfulStatus == RequestMessages.False ? "Connection failed" :
						  throw new Exception("Status is not equal true/false format.");

			_activitiesInfo(message);
		}

		private async void SendAliveStatus()
		{
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Alive", Message="alive" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
		}
		/// <summary>
		/// Get IP of this client
		/// </summary>
		/// <returns></returns>
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
		/// <summary>
		/// Send "Greeting message" to the WPF client
		/// </summary>
		/// <returns></returns>
		public async Task SendGreeting()
		{
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Greeting", Message="Greeting" }.ToJson();
			SendData(connectingMessage, _dekstopEndPoint);
		}
		/// <summary>
		/// Method that responds for Async sending data to some endpoint
		/// </summary>
		/// <param name="data">Data that we send</param>
		/// <param name="endPoint">EndPoint of Receiver</param>
		private async void SendData(string data, IPEndPoint endPoint)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(data);
			Task.Run(() =>
			{
				var sendEvent = new SocketAsyncEventArgs()
				{
					RemoteEndPoint = endPoint,
				};
				sendEvent.SetBuffer(buffer, 0, buffer.Length);
				_udpSocket.SendToAsync(sendEvent);
			});
		}
	}
}
