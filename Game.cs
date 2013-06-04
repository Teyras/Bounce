using System;

namespace Bounce
{
	public delegate void GameWonHandler (object sender, EventArgs e);
	public delegate void GameLostHandler (object sender, EventArgs e);
	public delegate void LivesChangedHandler (object sender, int value);
	public delegate void FilledAreaChangedHandler (object sender, int value);
	public class Game
	{
		Board board;
		Random random = new Random ();

		public int Filled {
			get;
			protected set;
		}

		public int Lives {
			get;
			protected set;
		}

		protected uint timeoutID;
		const int victoryCondition = 70;

		public event GameWonHandler GameWon;
		public event GameLostHandler GameLost;
		public event LivesChangedHandler LivesChanged;
		public event FilledAreaChangedHandler FilledAreaChanged;

		public Game (Board board)
		{
			this.board = board;
			board.AreaFilled += delegate(object sender, AreaFilledEventArgs e) {
				Filled += e.FilledArea;
				if (FilledAreaChanged != null) {
					FilledAreaChanged (this, getFilledPercents ());
				}
				if (getFilledPercents () >= victoryCondition) {
					this.End ();
					if (GameWon != null) {
						GameWon (this, EventArgs.Empty);
					}
				}
			};
			board.PlayerCollision += delegate(object sender, EventArgs e) {
				Lives -= 1;
				if (LivesChanged != null) {
					LivesChanged (this, Lives);
				}
				if (Lives == 0) {
					this.End ();
					if (GameLost != null) {
						GameLost (this, EventArgs.Empty);
					}
				}
			};
		}

		public void Start (Config config)
		{
			Filled = 2 * board.Width + 2 * board.Height - 4;
			if (FilledAreaChanged != null) {
				FilledAreaChanged (this, getFilledPercents ());
			}
			Lives = config.Lives;
			if (LivesChanged != null) {
				LivesChanged (this, Lives);
			}
			for (int i = 0; i < config.BallCount; i++) {
				spawnBall ();
			}
			timeoutID = GLib.Timeout.Add (40, delegate {
				board.MoveBalls ();
				board.Render ();
				return true;
			});
		}

		public void End ()
		{
			GLib.Source.Remove (timeoutID);
			board.MoveBalls ();
			board.Render ();
		}

		protected int getFilledPercents ()
		{
			return (int)Math.Floor ((decimal)((100 * Filled) / (board.Width * board.Height)));
		}

		protected void spawnBall ()
		{
			board.AddBall (
				random.Next (2, board.Width - 1), 
				random.Next (2, board.Height - 1), 
				(int)Math.Pow (-1, random.Next (0, 2)), 
				(int)Math.Pow (-1, random.Next (0, 2))
			);
		}
	}

	public class Config
	{
		public int Width, Height, BallCount, Lives;

		public Config (int Width = 0, int Height = 0, int BallCount = 0, int Lives = 0)
		{
			this.Width = Width;
			this.Height = Height;
			this.BallCount = BallCount;
			this.Lives = Lives;
		}
	}
}
