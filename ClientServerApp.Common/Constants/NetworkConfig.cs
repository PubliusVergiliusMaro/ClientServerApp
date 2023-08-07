namespace ClientServerApp.Common.Constants
{
	/*
	 * * Buffer Sizes:
	 *
	 * * Small Buffer Sizes:
	 * - 18 bytes
	 * - 32 bytes
	 * - 64 bytes
	 * - 128 bytes
	 * - 256 bytes
	 * - 512 bytes
	 *	
	 * * Moderate Buffer Sizes (1 KB - 4 KB):
	 * - 1024 bytes (1 KB)
	 *	
	 * Network MTU: 
	 * The MTU is the largest size of data that can be transmitted in a single network packet. 
	 * It's typically around 1500 bytes for Ethernet networks
	 *	
	 * - 1500 bytes 
	 * - 2048 bytes (2 KB)
	 *	
	 * * Larger Buffer Sizes (4 KB - 16 KB):
	 * - 4096 bytes (4 KB)
	 */
	
	public class ChunkConfig
	{
		/// <summary>
		/// Maximum size for one chunk of image
		/// </summary>
		public const int IMAGE_CHUNK_MAX_SIZE = 1024;// bytes
	}
	public class NetworkConfig
	{
		/// <summary>
		/// Buffer size for response
		/// </summary>
		public const int BUFFER_SIZE = 1500;// bytes
	}
}
