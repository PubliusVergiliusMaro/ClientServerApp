using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace ClientServerApp.Services.Helpers
{
	public class RequestData
	{
		/// <summary>
		/// Id of sender
		/// </summary>
		public int Id { get; set; } = -1;
		/// <summary>
		/// Name of action
		/// </summary>
		public string ActionName { get; set; } = " ";
		public string? Message { get; set; } = " ";
		/// <summary>
		/// Array for one chunk
		/// </summary>
		public byte[]? Image { get; set; } = new byte[0];
		/// <summary>
		/// Id of screenshot
		/// </summary>
		public int? ImageId { get; set; } = -1;
		/// <summary>
		/// Total number of chunks
		/// </summary>
		public int? TotalChunks { get; set; } = -1;
		/// <summary>
		/// Number of some chunk
		/// </summary>
		public int? ChunkNumber { get; set; } = -1;
		/// <summary>
		/// Numbers of chunks
		/// </summary>
		public int[]? ChunkNumbers { get; set; } = new int[0];
		public string ToJson() => JsonConvert.SerializeObject(this);
	}
}
