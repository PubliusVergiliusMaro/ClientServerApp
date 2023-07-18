using ClientServerApp.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientServerApp.Models.Models
{
	public class ClientData
	{
		public ClientData()
		{
			RequestsHistory = new List<RequestData>();
		}
		public int Id { get; set; }
		public IPEndPoint MobileEndPoint { get; set; }
		public IPEndPoint DekstopEndPoint { get; set; }
		public bool IsMobileClientConnected { get; set; }
		public bool IsDekstopClientConnected { get; set; }
		public List<RequestData> RequestsHistory { get; set; }
	}
}
