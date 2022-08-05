using Gtk;
using CloudFloppy;
using System.Diagnostics;

namespace CloudFloppy.Gtk;

public class Program
{
	static ApplicationWindow window;
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

			window = new ApplicationWindow(app);
			window.SetPosition(WindowPosition.Center);

			VBox hb = new VBox();
			window.Add(hb);
			hb.Add(new Label("Available games:"));

			Config.LoadConfig();
			foreach (var g in Config.Instance.Games)
			{
				var btn = new Button(g.Name);
				btn.AlwaysShowImage = true;

				btn.Clicked += async (sender, e) =>
				{
					btn.Sensitive = false;

					// Sync
					{
						btn.Image = new Image("gtk-refresh", IconSize.Button);
						await Task.Delay(1000);
					}

					// Launch game
					{
						btn.Image = new Image("gtk-media-play", IconSize.Button);

						var psi = new ProcessStartInfo("sh");
						psi.ArgumentList.Add("-c");
						psi.WorkingDirectory = g.GameDirectory;
						psi.ArgumentList.Add(g.ShellCommand);
						var proc = new Process();
						proc.StartInfo = psi;
						proc.Start();
						await proc.WaitForExitAsync();
					}

					// Sync
					{
						btn.Image = new Image("gtk-refresh", IconSize.Button);
						await Task.Delay(1000);
					}

					btn.Image = null;
					btn.Sensitive = true;
				};

				hb.Add(btn);
			}

			window.ShowAll();
		};

		return app.Run(app.ApplicationId, args);
	}
}