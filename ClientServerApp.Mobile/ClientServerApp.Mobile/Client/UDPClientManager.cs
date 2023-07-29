using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xamarin.Forms;
using System.IO;
using ClientServerApp.Mobile.Helpers;
using ClientServerApp.Mobile.Common;
using System.Collections;
using Newtonsoft.Json;

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

			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);// Change On Yours Server IP
			var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "message" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
			StartListening();
		}
		private async Task StartListening()
		{
			while (true)
			{
				var buffer = new byte[256];
				string answer = "";
				EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
				RequestData request = new RequestData();
					
				// Initialize all actions on requests
				var methods = new Dictionary<string, Action>
				{
					{ RequestActions.IsAlive, SendAliveStatus },
					{ RequestActions.XamarinConnectionStatus,() => DisplayConnectionStatus(request.Message) },
					{ RequestActions.WpfConnectionStatus, () => WpfConnectionStatus(request.Message) },
			    	{ RequestActions.Greeting, () => GetGreeting(request.Message) },
					{ RequestActions.SendMeImage,PrepareImage},
					{ RequestActions.GetChunk, () => SendChunk(request.ChunkNumber)}
				};
				do
				{
					var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
					senderEndPoint = result.RemoteEndPoint;
					answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
					request = JsonConvert.DeserializeObject<RequestData>(answer);
						
					methods[request.ActionName]();
				}
				while (_udpSocket.Available > 0);
			}
		}
		private void SendChunk(int chunkNumber)
		{
			_activitiesInfo($"Send chunk {chunkNumber}");
			byte[] neededChunk = _preparedImage[(int)chunkNumber];
			var message = new RequestData() { Id= 1, ActionName=RequestActions.SendChunk, Message = "", Image = neededChunk, ChunkNumber = chunkNumber, TotalChunks = _preparedImage.Count }.ToJson();
			SendData(message, _serverEndPoint);
		}
		public void PrepareImage()
		{
			_activitiesInfo("Someone wants image");
			// Check if array with bytes is more than 10 bytes
			if (ImageBytes.Length > 10)
			{
				int totalChunks = (int)Math.Ceiling((double)ImageBytes.Length/ChunkData.IMAGE_CHUNK_MAX_SIZE);

				for (int chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
				{
					int startIndex = chunkNumber * ChunkData.IMAGE_CHUNK_MAX_SIZE;
					int remainingBytes = ImageBytes.Length - startIndex;
					int chunkSize = Math.Min(ChunkData.IMAGE_CHUNK_MAX_SIZE, remainingBytes);

					byte[] chunkData = new byte[chunkSize];
					Array.Copy(ImageBytes, startIndex, chunkData, 0, chunkSize);

					_preparedImage[(int)chunkNumber] = chunkData;
					if (_preparedImage.Count == totalChunks)
					{
						var succesMessage = new RequestData() { Id = _clientId, ActionName = RequestActions.PrepareImage, TotalChunks = totalChunks }.ToJson();
						_activitiesInfo($"Size of prepered image is {totalChunks} chunks");
						SendData(succesMessage, _serverEndPoint);
					}
				}
			}
			else
			{
				_activitiesInfo("Prepare image for user");
			}
		}

		private void GetGreeting(string message)
		{
			_activitiesInfo(message);
		}

		private void WpfConnectionStatus(string successfulStatus)
		{
			if (successfulStatus == "true")
			{
				_activitiesInfo("Wpf client is connected");
			}
			else if (successfulStatus == "false")
			{
				_activitiesInfo("Wpf client in not connected");
			}
			else
				throw new Exception("Status is not equal true/false format.");
		}

		private void DisplayConnectionStatus(string successfulStatus)
		{
			if (successfulStatus == "true")
			{
				_activitiesInfo("Succesfully connected");
			}
			else if (successfulStatus == "false")
			{
				_activitiesInfo("Connection failed");
			}
			else
				throw new Exception("Status is not equal true/false format.");
		}

		private void SendAliveStatus()
		{
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Alive", Message="alive" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
		}
		/// <summary>
		/// Method for getting IP of this client
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
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Greeting", Message="alive" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
		}
		/// <summary>
		/// Method that responds for Async sending data to some endpoint
		/// </summary>
		/// <param name="data">Data that we send</param>
		/// <param name="endPoint">EndPoint of Receiver</param>
		private void SendData(string data, IPEndPoint endPoint)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(data);

			var waitHandle = new ManualResetEvent(false);
			var sendEvent = new SocketAsyncEventArgs();

			sendEvent.RemoteEndPoint = endPoint;
			sendEvent.SetBuffer(buffer, 0, buffer.Length);
			sendEvent.Completed += (s, e) => waitHandle.Set();

			bool sended = _udpSocket.SendToAsync(sendEvent);
			if (sendEvent.SocketError != SocketError.Success)
			{
				throw new SocketException((int)sendEvent.SocketError);
			}
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
		/// Port of a current client 
		/// </summary>
		private static int _port = 8083;
		/// <summary>
		/// Server`s port
		/// </summary>
		private static int _serverPort = 8081;
		/// <summary>
		/// Id of this client
		/// </summary>
		private static int _clientId;

		/// <summary>
		/// Field that in UI represents data 
		/// </summary>
		private readonly Action<string> _activitiesInfo;
		/// <summary>
		/// Dictionary of a Image that is going to sent
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
		/// Socket of current client
		/// </summary>
		private readonly Socket _udpSocket;
		/// <summary>
		/// Screenshot in byte array
		/// </summary>
		public byte[] ImageBytes { private get; set; }

	}
}
