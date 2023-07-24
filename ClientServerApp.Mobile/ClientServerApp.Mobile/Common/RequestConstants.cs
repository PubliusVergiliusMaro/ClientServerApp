using System;
using System.Collections.Generic;
using System.Text;

namespace ClientServerApp.Mobile.Common
{
	internal class RequestActions
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
	}
	internal class RequestMessages
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
