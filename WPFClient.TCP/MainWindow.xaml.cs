using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace WPFClient.TCP
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const string ip = "192.168.1.102";
		private const int port = 8081;

		private readonly IPEndPoint tcpEndPoint;
		private readonly Socket tcpSocket;

		public MainWindow()
		{
			InitializeComponent();
			
			tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			tcpSocket.Connect(tcpEndPoint);

			var greetingMsg = Encoding.UTF8.GetBytes("Wpf client connected.");
			tcpSocket.Send(greetingMsg);
			StartListening();
		}

		private async void StartListening()
		{
			try
			{
				var data = new StringBuilder();
				var buffer = new byte[256];
				var size = 0;

					do
					{
						size = tcpSocket.Receive(buffer);
						data.Append(Encoding.UTF8.GetString(buffer, 0, size));
					}
					while (tcpSocket.Available > 0);

					await Dispatcher.InvokeAsync(() => Message.Text = data.ToString());
				
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}
	}
}
