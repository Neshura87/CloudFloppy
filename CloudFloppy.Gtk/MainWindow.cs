using Gtk;
using CloudFloppy;
using UI = Gtk.Builder.ObjectAttribute;
using System.Diagnostics;

namespace CloudFloppy.Gtk;

public class MainWindow : ApplicationWindow
{
	[UI] Button AddGameButton;
	[UI] Button RefreshButton;
	[UI] Button AddGamePlaceholderButton;
	[UI] ListBox GameList;

	public MainWindow(Application app) : this(app, new Builder("main.glade")) { }

	public MainWindow(Application app, Builder b) : base(b.GetRawOwnedObject("win"))
	{
		b.Autoconnect(this);

		Application = app;

		AddGameButton.Clicked += ShowAddGameMenu;
		AddGamePlaceholderButton.Clicked += ShowAddGameMenu;

		List<Game> games = new(Config.Instance.Games);

		GameList.RowSelected += (sender, e) =>
		{
			var game = games[GameList.SelectedRow.Index];
		};

		GameList.ListRowActivated += async (sender, e) =>
		{
			var game = games[GameList.SelectedRow.Index];
			Console.WriteLine("Launch " + game.Name);

			GameList.SelectedRow.Sensitive = false;

			var psi = new ProcessStartInfo("sh");
			psi.ArgumentList.Add("-c");
			psi.WorkingDirectory = game.GameDirectory;
			psi.ArgumentList.Add(game.ShellCommand);
			var proc = new Process();
			proc.StartInfo = psi;
			proc.Start();
			await proc.WaitForExitAsync();

			GameList.SelectedRow.Sensitive = true;
		};

		foreach (var g in Config.Instance.Games)
		{
			var r = new ListBoxRow();
			r.Add(new Label(g.Name));

			GameList.Add(r);
		}
	}

	void ShowAddGameMenu(object sender, object e)
	{
		new AddGameMenu(this).Show();
	}
}