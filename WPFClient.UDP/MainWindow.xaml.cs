﻿using System.Net.Sockets;
using System.Net;
using System;
using System.Windows;
using System.Text;
using Models.Helpers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows.Media.Converters;
using System.Threading.Tasks;
using ClientServerApp.Common;
using System.IO;

namespace WPFClient.UDP
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static string ip = GetIPFromFile("C:\\Heap\\Programming\\StudyProjects\\ClientServerApp\\IPs.txt");// Change on your Path to file
		private const int port = 8082;
		private const int currentId = 1;

		private readonly IPEndPoint udpEndPoint;
		private readonly IPEndPoint serverEndPoint;
		private readonly IPEndPoint senderEndPoint;
		private readonly Socket udpSocket;
		private readonly StringBuilder data;//
		public MainWindow()
		{
			udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			udpSocket.Bind(udpEndPoint);

			serverEndPoint = new IPEndPoint(IPAddress.Parse("111.222.3.444"), 8081);// Change On Your Server IP
			var connectingMessage = new RequestData() { Id = currentId, ActionName = "Connecting", Message = "" }.ToJson();
			udpSocket.SendTo(Encoding.UTF8.GetBytes(connectingMessage), serverEndPoint);
			data = new StringBuilder();//
			InitializeComponent();
			StartListening();
		}
		private async void StartListening()
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
						var size = await udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer), SocketFlags.None, senderEndPoint);
						answer = Encoding.UTF8.GetString(buffer,0,size.ReceivedBytes);
						request = JsonConvert.DeserializeObject<RequestData>(answer);
						var methods = new Dictionary<string, Action>//
						{
							{RequestActions.IsAlive, SendAliveStatus },
							{RequestActions.WpfConnectionStatus, () => DisplayConnectionStatus(bool.Parse(request.Message)) },//
							{RequestActions.XamarinConnectionStatus, () => XamarinConnectionStatus(bool.Parse(request.Message)) },
							{RequestActions.Greeting, () => GetGreeting(request.Message) }
						};
						methods[request.ActionName]();
					}
					while (udpSocket.Available > 0);
				}
			}
			catch (Exception ex)
			{
				await AppendData(ex.Message);
			}
		}
		private async void GetGreeting(string message)
		{
			await AppendData(message);
		}
		private async void XamarinConnectionStatus(bool succesfulStatus)
		{
			if(succesfulStatus)
			{
				await AppendData("Xamarin client is connected");
			}
			else
			{
				await AppendData("Xamarin client is not connected");
			}
		}

		private async void DisplayConnectionStatus(bool succesfulStatus)//
		{
			if (succesfulStatus)
			{
				await AppendData("Succesfuly connected");//
            }
			else
			{
				await AppendData("Conection failed");//
			}
		}

		private void SendAliveStatus()
		{
			var connectingMessage = new RequestData { Id = currentId, ActionName=RequestActions.Alive, Message="I`m alive"}.ToJson();//$"greeting:Xamarin client has been connected.";
			udpSocket.SendTo(Encoding.UTF8.GetBytes(connectingMessage), serverEndPoint);
		}

		private async void SendGreetingButton_Click(object sender, RoutedEventArgs e)
		{
			var greetingMessage = new RequestData() { Id = currentId, ActionName = RequestActions.Greeting, Message = "" }.ToJson();
			udpSocket.SendTo(Encoding.UTF8.GetBytes(greetingMessage), serverEndPoint);
			await AppendData("Succesfully sent greeting to server for redirecting message");
		}
		private async Task AppendData(string line)//
		{
			data.AppendLine(line);
			await Dispatcher.InvokeAsync(() => Message.Text = data.ToString());
		}
			private static string GetIPFromFile(string path)
		{
			string[]lines = File.ReadAllLines(path);
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
	}
}
