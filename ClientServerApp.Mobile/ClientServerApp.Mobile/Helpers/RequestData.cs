using System;
using Newtonsoft.Json;
namespace ClientServerApp.Mobile.Helpers
{
	internal class RequestData
	{
		public RequestData() 
		{
			Id = 0;
			ActionName = " ";
			Message = " ";
			Image = new byte[0];
			TotalChunks = 0;
			ChunkNumber = 0;
		}
		public int Id { get; set; }
		public string ActionName { get; set; }
		public string Message { get; set; }
		public byte[] Image { get; set; }
		public int TotalChunks { get; set; }
		public int ChunkNumber { get; set; }
		public string ToJson()
		{
			return JsonConvert.SerializeObject(this);
			//string imageJson = $",\"Image\":\"{Image}\"";
			//string totalChunksJson = $",\"TotalChunks\":{TotalChunks}";
			//string chunkNumberJson = $",\"ChunkNumber\":{ChunkNumber}";

			//return $"{{\"Id\":{Id},\"ActionName\":\"{ActionName}\",\"Message\":\"{Message}\"{imageJson}{totalChunksJson}{chunkNumberJson}}}";
		}
		public override string ToString() => $"{Id}:{ActionName}:{Message}:{Image}:{TotalChunks}:{ChunkNumber}";
	}
}
