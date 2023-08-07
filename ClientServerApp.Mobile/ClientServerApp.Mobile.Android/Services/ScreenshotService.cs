using Android.App;
using Android.Graphics;
using ClientServerApp.Mobile.Droid;
using ClientServerApp.Mobile.Services.ScreenshotServices;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(ScreenshotServiceAndroid))]
namespace ClientServerApp.Mobile.Droid
{
	public class ScreenshotServiceAndroid : IScreenshotService
	{
		/// <summary>
		/// Make screenshot and get it in byte array
		/// </summary>
		/// <returns></returns>
		public byte[] CaptureScreen()
		{
			var activity = Forms.Context as Activity;
			if (activity == null)
			{
				return null;
			}

			var rootView = activity.Window.DecorView;
			rootView.DrawingCacheEnabled = true;
			var screenshot = Bitmap.CreateBitmap(rootView.DrawingCache);
			rootView.DrawingCacheEnabled = false;

			using (var stream = new MemoryStream())
			{
				screenshot.Compress(Bitmap.CompressFormat.Png, 1, stream);
				return stream.ToArray();
			}
		}
	}
}