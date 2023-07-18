using Models.Helpers;
using System.Net;

namespace Models.DTOs
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
