using ClientServerApp.Server;

public class Program
{
	private static string ActivitiesInfo
	{
		set => Console.WriteLine(value);
	}
	static void Main(string[] args)
	{
		UDPServerManager server = new UDPServerManager(ainfo => ActivitiesInfo = ainfo);
		Console.ReadKey();
	}
}