using Newtonsoft.Json;

namespace ClientServerApp.Services.Helpers
{
	public class RequestData
	{
		public int Id { get; set; }
		public string ActionName { get; set; }
		public string? Message { get; set; }
		public byte[]? Image { get; set; }
		public int? TotalChunks { get; set; }
		public int? ChunkNumber { get; set; }
		public string ToJson() => JsonConvert.SerializeObject(this);
		
		/// <summary>
		/// Using for sending requests on Xamarin
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			if(Message == null)
				Message = string.Empty;
			if(Image == null)
				Image = new byte[0];
			if(TotalChunks == null)
				TotalChunks = -1;
			if(ChunkNumber == null) 
				ChunkNumber = -1;
			return $"{Id}:{ActionName}:{Message}:{Image}:{TotalChunks}:{ChunkNumber}";
		}
	}
}
