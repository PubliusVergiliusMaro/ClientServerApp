namespace XamarinClient.UDP.Helpers
{
	internal class RequestData
	{
		public int Id { get; set; }
		public string ActionName { get; set; }
		public string Message { get; set; }
		public string ToJson()
		{
			return $"{{\"Id\":{Id},\"ActionName\":\"{ActionName}\",\"Message\":\"{Message}\"}}";
		}
		public override string ToString()
		{
			return $"{Id}:{ActionName}:{Message}";
		}
	}
}
