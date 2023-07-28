namespace ClientServerApp.Common.Constants
{
	public class RequestActions
	{
		/// <summary>
		/// Displays for client that server asks if client is online
		/// </summary>
		public const string IsAlive = "IsAlive";
		/// <summary>
		/// Xamarin client connection status with server
		/// </summary>
		public const string XamarinConnectionStatus = "XamarinConnectionStatus";
		/// <summary>
		/// Wpf client connection status with server
		/// </summary>
		public const string WpfConnectionStatus = "WpfConnectionStatus";
		/// <summary>
		/// Action for greeting client from another client
		/// </summary>
		public const string Greeting = "Greeting";
		/// <summary>
		/// Displays for server that client is online
		/// </summary>
		public const string Alive = "Alive";
		/// <summary>
		/// Action for sending image to another client
		/// </summary>
		public const string Image = "Image";
		/// <summary>
		/// Action for sending video to another client
		/// </summary>
		public const string Video = "Video";
		public const string SendMeImage = "SendMeImage";
	}
	public class RequestMessages
	{
		/// <summary>
		/// Message that server sends with IsAlive Request Action
		/// </summary>
		public const string IsAlive = "Server is checking if you are alive";
		/// <summary>
		/// Displays true status
		/// </summary>
		public const string True = "true";
		/// <summary>
		/// Displays false status
		/// </summary>
		public const string False = "false";
	}
}
