using ClientServerApp.Mobile.Helpers;
using System.Collections.Generic;
using System.Net;

namespace ClientServerApp.Mobile.Models
{
	internal class ClientData
	{
		public ClientData()
		{
			RequestsHistory = new List<RequestData>();
		}
		public int Id { get; set; }
		/// <summary>
		/// Mobile client endpoint
		/// </summary>
		public IPEndPoint MobileEndPoint { get; set; }
		/// <summary>
		/// Wpf client endpoint
		/// </summary>
		public IPEndPoint DekstopEndPoint { get; set; }
		/// <summary>
		/// Displays whether the mobile client is online
		/// </summary>
		public bool IsMobileClientConnected { get; set; }
		/// <summary>
		/// Displays whether the dekstop client is online
		/// </summary>
		public bool IsDekstopClientConnected { get; set; }
		/// <summary>
		/// Request History
		/// </summary>
		public List<RequestData> RequestsHistory { get; set; }
	}
}
