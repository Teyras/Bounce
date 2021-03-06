using System;
using Gtk;
using Mono;
using Bounce;

public partial class MainWindow: Gtk.Window
{
	Board board;
	Game game;
	int level;

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();
	}

	protected Board createBoard (int width, int height)
	{
		int fieldSize = 20;
		BoardRenderer renderer = new BoardRenderer (this.canvas, width, height, fieldSize);
		board = new Board (width, height, fieldSize, renderer);
		return board;
	}

	public void StartGame (Config config)
	{
		game = new Game (this.createBoard (config.Width, config.Height));
		game.GameWon += delegate(object sender, EventArgs e) {
			MessageDialog dialog = new MessageDialog (
				this,
			 DialogFlags.Modal,
			 MessageType.Info,
			 ButtonsType.None,
			 "Úroveň dokončena! Abyste zvládli víc nepřátel, dostanete další život."
			);
			dialog.AddButton ("Další kolo", ResponseType.Accept);
			dialog.AddButton ("Konec hry", ResponseType.Cancel);
			dialog.Response += delegate(object o, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					NextLevel (config);
				} else {
					Application.Quit ();
				}
			};
			dialog.Run ();
			dialog.Destroy ();
		};
		game.GameLost += delegate(object sender, EventArgs e) {
			MessageDialog dialog = new MessageDialog (
				this,
			 DialogFlags.Modal,
			 MessageType.Info,
			 ButtonsType.None,
			 "Konec hry"
			);
			dialog.AddButton ("Nová hra", ResponseType.Accept);
			dialog.AddButton ("Konec", ResponseType.Close);
			dialog.Response += delegate(object o, ResponseArgs args) {
				if (args.ResponseId == ResponseType.Accept) {
					MainClass.ShowLauncher ();
					this.Destroy ();
				} else {
					Application.Quit ();
				}
			};
			dialog.Run ();
			dialog.Destroy ();
		};
		game.FilledAreaChanged += delegate(object sender, int value) {
			fillCounter.Text = String.Format ("Zaplněno: {0}%", value);
		};
		game.LivesChanged += delegate(object sender, int value) {
			lifeCounter.Text = String.Format ("Životy: {0}", value);
		};
		game.RemainingTimeChanged += delegate(object sender, int value) {
			remainingTimeCounter.Text = string.Format ("Zbývající čas: {0} sekund", value);
		};
		game.Start (config);
		level = 1;
		updateLevelCounter ();
	}

	protected void PauseGame ()
	{
		game.Pause ();
		board.OverlayText = "Hra je pozastavena. Stiskněte libovolnou klávesu.";
	}

	protected void NextLevel (Config config)
	{
		level += 1;
		updateLevelCounter ();
		config.BallCount += 1;
		if (level % 2 == 0) {
			config.MonsterCount += 1;
		}
		config.Lives = game.Lives + 1;
		board.Clear ();
		game.Start (config);
	}

	protected void updateLevelCounter ()
	{
		levelCounter.Text = String.Format ("Úroveň: {0}", level);
	}

	protected void OnFocusOutEvent (object sender, FocusOutEventArgs args)
	{
		if (game.Running) {
			PauseGame ();
		}
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnCanvasExposeEvent (object o, ExposeEventArgs args)
	{
		if (game.Running) {
			board.Render ();
		}
	}

	[GLib.ConnectBefore]
	protected void OnKeyPressEvent (object o, KeyPressEventArgs args)
	{
		if (!game.Running) {
			game.Resume ();
		} else {
			board.SteerPlayer (keyToDirection (args.Event.Key));
		}
	}

	protected void OnKeyReleaseEvent (object o, KeyReleaseEventArgs args)
	{
		board.UnsteerPlayer (keyToDirection (args.Event.Key));
	}

	protected Direction keyToDirection (Gdk.Key key)
	{
		switch (key) {
		case Gdk.Key.w:
		case Gdk.Key.k:
		case Gdk.Key.Up:
			return Bounce.Direction.Up;
		case Gdk.Key.s:
		case Gdk.Key.j:
		case Gdk.Key.Down:
			return Bounce.Direction.Down;
		case Gdk.Key.a:
		case Gdk.Key.h:
		case Gdk.Key.Left:
			return Bounce.Direction.Left;
		case Gdk.Key.d:
		case Gdk.Key.l:
		case Gdk.Key.Right:
			return Bounce.Direction.Right;
		default:
			return Bounce.Direction.None;
		}
	}
}
