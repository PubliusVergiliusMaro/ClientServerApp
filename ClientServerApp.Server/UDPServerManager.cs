using ClientServerApp.Common;
using ClientServerApp.Common.Constants;
using ClientServerApp.Database.Models;
using ClientServerApp.Services.Helpers;
using Newtonsoft.Json;
using SkiaSharp;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace ClientServerApp.Server
{
	public class UDPServerManager
	{
		// Put to the bottom of the project
		private static List<ClientData> _connectedClients;
		private static Dictionary<int, byte[]> _receivedChunks;
		private static Stopwatch timer;

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
			
			_receivedChunks = new Dictionary<int, byte[]>();
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
							{ RequestActions.Image, async() => await GetImage(request.Image,request.ChunkNumber,request.TotalChunks) },
							{ RequestActions.Video, async() => await GetVideo(request.Video,request.ChunkNumber,request.TotalChunks) },
							{ RequestActions.SendMeImage, SendImage},
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

		private async Task GetVideo(byte[] chunk, int? chunkNumber, int? totalChunks)
		{
			
			if (chunkNumber == 1)
			{
				timer = new Stopwatch();
				timer.Start();
			}
			//int sequenceNumber = BitConverter.ToInt32(chunk, 0);
			int startIndex = (int)chunkNumber * ChunkData.VIDEO_CHUNK_MAX_SIZE;

			byte[] chunkData = new byte[chunk.Length];
			Array.Copy(chunk, 0, chunkData, 0, chunk.Length);

			_receivedChunks[(int)chunkNumber] = chunkData;
			await Console.Out.WriteLineAsync($"Video loads on . {(double)_receivedChunks.Count/totalChunks * 100.0}");

			if (_receivedChunks.Count == totalChunks)
			{
				byte[] assembledVideoData;
				using (MemoryStream imgStream = new MemoryStream())
				{
					for (int i = 1; i <= totalChunks; i++)
					{
						byte[] tmpdata = _receivedChunks[i];
						imgStream.Write(tmpdata, 0, tmpdata.Length);
					}
					assembledVideoData = imgStream.ToArray();

					await File.WriteAllBytesAsync($@"C:\Users\user\Desktop\TEST\Video{new Random().Next(0, 100000)}.mp4", assembledVideoData);
					await Console.Out.WriteLineAsync("Succesfully Saved");
					timer.Stop();
					await Console.Out.WriteLineAsync($"VIDEO -> Required time: {timer.ElapsedMilliseconds}");
					_receivedChunks = new Dictionary<int, byte[]>();
				}
			}
		}
		public async Task SendImage()
		{
			//C:\Users\user\Desktop\Im.jpg
			string imagePath = @"C:\Users\user\Desktop\Im.jpg";
			byte[] buffer;
			using (FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
			{
				buffer = new byte[stream.Length];
				await stream.ReadAsync(buffer, 0, buffer.Length);
			}
			int totalChunks = (int)Math.Ceiling((double)buffer.Length/ChunkData.IMAGE_CHUNK_MAX_SIZE);
			int bytesCount = 0;
			for (int chunkNumber = 0; chunkNumber < totalChunks; chunkNumber++)
			{
				int startIndex = chunkNumber * ChunkData.IMAGE_CHUNK_MAX_SIZE;
				int remainingBytes = buffer.Length - startIndex;
				int chunkSize = Math.Min(ChunkData.IMAGE_CHUNK_MAX_SIZE, remainingBytes);

				byte[] chunkData = new byte[chunkSize];
				Array.Copy(buffer, startIndex, chunkData, 0, chunkSize);

				bytesCount++;

				// Add client ID
				var message = new RequestData() { Id= 1, ActionName=RequestActions.Image, Message = "", Image = chunkData, TotalChunks = totalChunks, ChunkNumber =  chunkNumber+1 }.ToJson();
				bytesCount +=  chunkData.Length;
				await _udpSocket.SendToAsync(Encoding.UTF8.GetBytes(message), _senderEndPoint);
					await Task.Delay(100);
				//if (chunkNumber % 10 == 0)
			}
		}
		private async Task GetImage(byte[] chunk, int? chunkNumber, int? totalChunks)
		{
			//int sequenceNumber = BitConverter.ToInt32(chunk, 0);
			if (chunkNumber == 1)
			{
				timer = new Stopwatch();
				timer.Start();
			}
			int startIndex = (int)chunkNumber * ChunkData.IMAGE_CHUNK_MAX_SIZE;

			byte[] chunkData = new byte[chunk.Length];
			Array.Copy(chunk, 0, chunkData, 0, chunk.Length);

			_receivedChunks[(int)chunkNumber] = chunkData;
			await Console.Out.WriteLineAsync($"Image packets loading. {_receivedChunks.Count}");

			if (_receivedChunks.Count == totalChunks)
			{
				byte[] assembledImageData;
				using (MemoryStream imgStream = new MemoryStream())
				{
					for (int i = 1; i <= totalChunks; i++)
					{
						byte[] tmpdata = _receivedChunks[i];
						imgStream.Write(tmpdata, 0, tmpdata.Length);
					}
					assembledImageData = imgStream.ToArray();

					SaveImageFromBytes(assembledImageData, @"C:\Users\user\Desktop\TEST", $"FirstImage{new Random().Next(0,1000000)}.png");
					await Console.Out.WriteLineAsync("Succesfully Saved");
					timer.Stop();
                    await Console.Out.WriteLineAsync($"IMAGE -> Required time: {timer.ElapsedMilliseconds}");
                }
				//using(MemoryStream stream = new MemoryStream(assembledImageData))
				//	{
				//	using (Image image = Image.FromStream(stream))
				//	{

				//		string folderPath = @"C:\Users\user\Desktop\TEST";
				//		string imageName = "FirstImage.png";

				//		string imagePath = Path.Combine(folderPath, imageName);
				//		image.Save(imagePath, ImageFormat.Png);
				//		await Console.Out.WriteLineAsync("Succesfully Saved");
				//	}
				//	}
			}
		}

		public static void SaveImageFromBytes(byte[] imageData, string folderPath, string imageName)
		{
			try
			{
				using (var stream = new SKMemoryStream(imageData))
				{
					using (var codec = SKCodec.Create(stream))
					{
						if (codec == null)
						{
							throw new Exception("Failed to create SKCodec for the image data.");
						}

						var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
						using (var bitmap = new SKBitmap(info))
						{
							var result = codec.GetPixels(info, bitmap.GetPixels());
							if (result == SKCodecResult.Success)
							{
								// Save the image as PNG format
								string imagePath = Path.Combine(folderPath, imageName);

								using (var image = SKImage.FromBitmap(bitmap))
								using (var imageStream = File.OpenWrite(imagePath))
								{
									image.Encode(SKEncodedImageFormat.Png, 100).SaveTo(imageStream);
								}
							}
							else if (result == SKCodecResult.IncompleteInput)
							{
								throw new Exception("Incomplete input: The image data provided is insufficient.");
							}
							else if (result == SKCodecResult.InvalidInput)
							{
								throw new Exception("Invalid input: The image data is in an unsupported or corrupted format.");
							}
							else
							{
								throw new Exception($"Failed to decode the image data: {result}");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error saving the image: {ex.Message}");
			}
		}
		//private async Task GetImage(byte[] byteImage)
		//{
		//	if (byteImage.Length > 0)
		//	{
		//		using(MemoryStream memstr = new MemoryStream(byteImage))
		//		{
		//			Image img = Image.FromStream(memstr);

		//			string folderPath = @"‪C:\Users\user\Desktop\TEST";
		//			string imageName = "my_Image.jpg";

		//			string imagePath = Path.Combine(folderPath, imageName);
		//			img.Save(imagePath, ImageFormat.Jpeg);
		//		}
		//	}
		//	else
		//		throw new Exception("Array with image in bytes is empty.");
		//}

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