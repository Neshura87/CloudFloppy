using Gtk;
using CloudFloppy;
using UI = Gtk.Builder.ObjectAttribute;
using System.Diagnostics;

namespace CloudFloppy.Gtk;

public class AddGameMenu : Dialog
{
	Game Game = new();

	[UI] Button BackButton;
	[UI] Button NextButton;
	[UI] Button AddButton;
	[UI] Stack PageStack;
	Widget GameTypePage;
	Widget DirectoryPage;

	[UI] RadioButton GameTypeLinux;
	[UI] Entry PrefixLocation;
	[UI] Button PrefixLocationFileButton;

	[UI] ComboBox BaseDirectory;
	[UI] Entry Subdirectory;
	[UI] Button SubdirectoryFileButton;
	[UI] Label FullPath;
	[UI] Entry IncludeRegex;
	[UI] Entry ExcludeRegex;
	[UI] TreeView Files;

	public AddGameMenu(Window win) : this(win, new Builder("addgame.glade")) { }

	public AddGameMenu(Window win, Builder b) : base(b.GetRawOwnedObject("dialog"))
	{
		b.Autoconnect(this);

		TransientFor = win;
		Modal = true;

		AddButton.Hide();
		BackButton.Hide();

		GameTypePage = PageStack.Children[0];
		DirectoryPage = PageStack.Children[1];

		PageStack.VisibleChild = GameTypePage;

		BaseDirectory.Changed += (sender, e) =>
		{
			TreeIter it;
			BaseDirectory.Model.IterNthChild(out it, BaseDirectory.Active);

			Game.SaveRoot = (SaveRoot)BaseDirectory.Model.GetValue(it, 1);
			UpdatePath();
		};

		Subdirectory.Changed += (sender, e) =>
		{
			Game.SaveRootSubdirectory = Subdirectory.Text;
			UpdatePath();
		};

		NextButton.Clicked += (sender, e) =>
		{
			if (PageStack.VisibleChild == GameTypePage)
			{
				Game.GameType = GameTypeLinux.Active ? GameType.Native : GameType.Wine;

				PageStack.VisibleChild = DirectoryPage;
				BackButton.Show();
				NextButton.Hide();
				AddButton.Show();

				BaseDirectory.Clear();
				BaseDirectory.Model = new ListStore(typeof(string), typeof(SaveRoot));

				CellRendererText cell = new CellRendererText();
				BaseDirectory.PackStart(cell, true);
				BaseDirectory.SetAttributes (cell, "text", 0);

				(BaseDirectory.Model as ListStore).AppendValues("Custom", SaveRoot.Custom);
				(BaseDirectory.Model as ListStore).AppendValues("Documents", SaveRoot.Documents);

				BaseDirectory.Active = 0;
			}
		};

		BackButton.Clicked += (sender, e) =>
		{
			if (PageStack.VisibleChild == DirectoryPage)
			{
				PageStack.VisibleChild = GameTypePage;
				BackButton.Hide();
				NextButton.Show();
				AddButton.Hide();
			}
		};

		AddButton.Clicked += (sender, e) =>
		{
			Destroy();
		};
	}

	void UpdatePath()
	{
			FullPath.Text = "Full path: " + Game.FullPath;

	}
}