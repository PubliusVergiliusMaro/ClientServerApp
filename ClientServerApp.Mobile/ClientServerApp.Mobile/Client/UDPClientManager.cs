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

namespace ClientServerApp.Mobile.Client
{	
	internal class UDPClientManager
	{
		// Put to the bottom of the project
		private static string _ip = "192.168.1.103";// Change On Yours Mobile IP
		private static string _serverIp = "192.168.1.104"; // Change On Server IP
		private static int _port = 8083;
		private static int _serverPort = 8081;
		private static int _clientId;
		private readonly Action<string> _activitiesInfo;
		private static Dictionary<int, byte[]> _preparedImage;

		private readonly StringBuilder _data;
		private readonly IPEndPoint _udpEndPoint;
		private readonly IPEndPoint _serverEndPoint;
		private readonly IPEndPoint _senderEndPoint;// Check if need it here
		private readonly Socket _udpSocket;
		
		/// <summary>
		/// Screenshot in byte array
		/// </summary>
		public byte[] ImageBytes 
		{
			private get; 
			set; 
		}
		public UDPClientManager(Action<string> activitiesInfo)
		{
			
			_preparedImage = new Dictionary<int, byte[]>();
			_activitiesInfo=activitiesInfo;
			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);
			_data = new StringBuilder();

			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);// Change On Yours Server IP
			var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "message" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
			StartListening();
		}
		private async Task StartListening()
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
					byte[] image = new byte[0];
					byte[] video = new byte[0];
					int totalChunks = 0;
					int chunkNumber = 0;
					do
					{
						var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						senderEndPoint = result.RemoteEndPoint;
						//
						// answer format -> ID:ActionName:Message:Image
						answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
						//

						#region Get parts of Answer TODO: Try to change on json
						string[] parts = answer.Split(':');
						if(parts.Length != 6)
							throw new Exception("Not right format of answer");
						if (int.TryParse(parts[0], out int ID))
						id = ID;
						else
						{
							throw new Exception("Not right format of answer");
						}
						action = parts[1].Replace(":", "");
						message = parts[2].Replace(":", "");
						image = Encoding.UTF8.GetBytes(parts[3].Replace(":", ""));
						
						if (int.TryParse(parts[4].Replace(":", ""), out int total))
							totalChunks = total;
						else
						{
							throw new Exception("Not right format of answer");
						}

						if (int.TryParse(parts[5].Replace(":", ""), out int num))
								chunkNumber = num;
						else
						{
							throw new Exception("Not right format of answer");
						}
						#endregion
						//request = JsonConvert.DeserializeObject<RequestManager>(answer);
						
						var methods = new Dictionary<string, Action>
						{
							{ RequestActions.IsAlive, SendAliveStatus },
							{ RequestActions.XamarinConnectionStatus,() => DisplayConnectionStatus(message) },
							{ RequestActions.WpfConnectionStatus, () => WpfConnectionStatus(message) },
							{ RequestActions.Greeting, () => GetGreeting(message) },
							{ RequestActions.SendMeImage,PrepareImage},
							{ RequestActions.GetChunk, () => SendChunk(chunkNumber)}
						};
						methods[action]();
					}
					while (_udpSocket.Available > 0);
				}
			}
			catch (Exception ex)
			{
				AppendData(ex.Message);
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
			AppendData("Someone wants image");
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
						AppendData($"Size of prepered image is {totalChunks} chunks");
						SendData(succesMessage, _serverEndPoint);
					}
				}
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

		private void DisplayConnectionStatus(string successfulStatus)
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
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Alive", Message="alive" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
		}
		private void AppendData(string line)
		{
			_data.AppendLine(line);
			Device.BeginInvokeOnMainThread(() => _activitiesInfo(_data.ToString()));
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
		public async Task SendGreeting()
		{
			var connectingMessage = new RequestData { Id = _clientId, ActionName="Greeting", Message="alive" }.ToJson();
			SendData(connectingMessage, _serverEndPoint);
		}
		private void SendData(string message, IPEndPoint endPoint)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);

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
	}
}
