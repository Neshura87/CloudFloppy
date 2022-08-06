using Gtk;
using CloudFloppy;
using System.Diagnostics;

namespace CloudFloppy.Gtk;

public class Program
{
	static MainWindow window;
	public static int Main(string[] args)
	{
		var app = new Application("link.ryhn.cloudfloppy", GLib.ApplicationFlags.None);

		app.Activated += (sender, e) =>
		{
			if (window != null)
			{
				window.Present();
				return;
			}
			
			Config.LoadConfig();

			window = new MainWindow(app);
			window.SetPosition(WindowPosition.Center);
			window.ShowAll();
		};

		return app.Run(app.ApplicationId, args);
	}
}