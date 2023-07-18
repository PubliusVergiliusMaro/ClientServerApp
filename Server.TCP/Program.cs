using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server.TCP
{
	internal class Program
	{
		static void Main(string[] args)
		{
			const string ip = "192.168.1.102";
			const int port = 8081;
			try
			{
				var tcpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
				var tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				tcpSocket.Bind(tcpEndPoint);
				tcpSocket.Listen(10);

				while (true)
				{
					Console.WriteLine("Waiting for connection threw {0}", tcpEndPoint);

					var listener = tcpSocket.Accept();

					var buffer = new byte[256];
					var size = 0;
					var data = new StringBuilder();

					do
					{
						size = listener.Receive(buffer);
						data.Append(Encoding.UTF8.GetString(buffer, 0, size));
					}
					while (listener.Available > 0);

					Console.WriteLine(data);

					listener.Send(Encoding.UTF8.GetBytes("Message Received"));
					listener.Shutdown(SocketShutdown.Both);
					listener.Close();
				}
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
			finally
			{
				Console.ReadLine();
			}
		}
	}
}