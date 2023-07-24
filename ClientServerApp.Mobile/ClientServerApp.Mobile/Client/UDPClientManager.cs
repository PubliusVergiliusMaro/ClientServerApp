﻿using System;
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

namespace ClientServerApp.Mobile.Client
{	
	internal class UDPClientManager
	{
		// Put to the bottom of the project
		private static string _ip = "192.168.1.109";// Change On Yours Mobile IP
		private static string _serverIp = "192.168.1.104"; // Change On Server IP
		private static int _port = 8083;
		private static int _serverPort = 8081;
		private static int _clientId;
		private readonly Action<string> _activitiesInfo;

		private readonly StringBuilder _data;
		private readonly IPEndPoint _udpEndPoint;
		private readonly IPEndPoint _serverEndPoint;
		private readonly IPEndPoint _senderEndPoint;// Check if need it here
		private readonly Socket _udpSocket;

		public UDPClientManager(Action<string> activitiesInfo)
		{
			_activitiesInfo=activitiesInfo;
			_udpEndPoint = new IPEndPoint(IPAddress.Parse(_ip), _port);
			_udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			_udpSocket.Bind(_udpEndPoint);
			_data = new StringBuilder();

			_serverEndPoint = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);// Change On Yours Server IP
			var connectingMessage = new RequestData() { Id = _clientId, ActionName = "Connecting", Message = "message" }.ToJson();
			Send(connectingMessage, _serverEndPoint);
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
					do
					{
						var result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));
						senderEndPoint = result.RemoteEndPoint;
						//answer format -> ID:ActionName:Message
						answer = Encoding.UTF8.GetString(buffer, 0, result.ReceivedBytes);
						string[] parts = answer.Split(':');

						if (int.TryParse(parts[0], out int intValue))
						{
							id = intValue;
							action = parts[1].Replace(":", "");
							message = parts[2].Replace(":", "");
						}
						//request = JsonConvert.DeserializeObject<RequestManager>(answer);

						var methods = new Dictionary<string, Action>
						{
							{ RequestActions.IsAlive, SendAliveStatus },
							{ RequestActions.XamarinConnectionStatus,async () => await DisplayConnectionStatus(message) },
							{ RequestActions.WpfConnectionStatus, () => WpfConnectionStatus(message) },
							{ RequestActions.Greeting, () => GetGreeting(message) }
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

		private async Task DisplayConnectionStatus(string successfulStatus)
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
			Send(connectingMessage, _serverEndPoint);
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
			Send(connectingMessage, _serverEndPoint);
		}
		private void Send(string message, IPEndPoint endPoint)
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