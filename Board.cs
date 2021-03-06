using System;
using System.Collections.Generic;
using Gtk;
using Mono;

namespace Bounce
{
	public class AreaFilledEventArgs : EventArgs
	{
		public int FilledArea;

		public AreaFilledEventArgs (int filled)
		{
			FilledArea = filled;
		}
	}
	public delegate void AreaFilledHandler (object sender, AreaFilledEventArgs e);
	public delegate void PlayerCollisionHandler (object sender, EventArgs e);
	public class Board
	{
		protected List<Ball> balls = new List<Ball> ();
		protected List<Monster> monsters = new List<Monster> ();
		private Field[,] fields;

		public int FieldSize { get; protected set; }

		const int hitLimit = 1000;
		BoardRenderer renderer;

		public string OverlayText {
			set {
				renderer.RenderOverlay (value);
			}
		}

		public Player Player {
			get;
			protected set;
		}

		public int Width { get; protected set; }

		public int Height { get; protected set; }

		public event AreaFilledHandler AreaFilled;
		public event PlayerCollisionHandler PlayerCollision;

		public Board (int width, int height, int fieldSize, BoardRenderer renderer)
		{
			this.renderer = renderer;
			this.Width = width;
			this.Height = height;
			this.Clear ();
			this.FieldSize = fieldSize;
			renderer.RefreshBackground (fields);
		}

		public void Clear ()
		{
			balls = new List<Ball> ();
			monsters = new List<Monster> ();
			fields = new Field[Width, Height];
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					fields [i, j] = new Field ();
					if (i == 0 || j == 0 || i + 1 == Width || j + 1 == Height) {
						fields [i, j].Full = true;
					} else {
						fields [i, j].Full = false;
					}
					fields [i, j].X = i;
					fields [i, j].Y = j;
				}
			}
			Player = new Player (0, 0);
			Player.BaseField = fields [0, 0];
			renderer.RefreshBackground (fields);
		}

		public void Render ()
		{
			renderer.Render (Player, balls, monsters);
		}

		public void AddBall (int x, int y, int dX, int dY)
		{
			balls.Add (new Ball (x * FieldSize, y * FieldSize, dX, dY));
		}

		public void AddMonster (string type)
		{
			double[,] record = new double [Width, Height];
			Field playerField = crossedField (Player.X, Player.Y);
			record [playerField.X, playerField.Y] = -1;
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					if (fields [i, j].Full && record [i, j] != -1) {
						record [i, j] = (monsters.Count + 1) * Math.Sqrt (Math.Pow (i - playerField.X, 2) + Math.Pow (j - playerField.Y, 2));
						foreach (Monster monster in monsters) {
							Field monsterField = crossedField (monster.X, monster.Y);
							record [monsterField.X, monsterField.Y] = -1;
							record [i, j] += Math.Sqrt (Math.Pow (i - monsterField.X, 2) + Math.Pow (j - monsterField.Y, 2));
						}
					}
				}
			}
			Field field = null;
			double maxRank = -1;
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					if (record [i, j] > maxRank) {
						field = fields [i, j];
						maxRank = record [i, j];
					}
				}
			}
			MonsterStrategy strategy = (MonsterStrategy)Activator.CreateInstance (Type.GetType ("Bounce." + type));
			monsters.Add (new Monster (strategy, type, field.X * FieldSize, field.Y * FieldSize));
		}

		protected void closeTrail ()
		{
			int filled = 0;
			List<Field> ballMap = new List<Field> ();
			foreach (Ball ball in balls) {
				ballMap.Add (crossedField (ball.X, ball.Y));
			}
			foreach (Field field in Player.Trail) {
				field.Full = true;
				filled += 1;
			}
			Player.Trail = new List<Field> ();
			bool[,] visited = new bool[Width, Height]; 

			List<Field> reserve = new List<Field> ();
			
			Func<bool> visitedAll = (delegate () {
				for (int i = 0; i < visited.GetLength(0); i++) {
					for (int j = 0; j < visited.GetLength(1); j++) {
						if (!visited [i, j] && !fields [i, j].Full) {
							return false;
						}
					}
				}
				return true;
			});
			
			do {
				reserve = calculateReserve (ballMap, visited);
				foreach (Field field in reserve) {
					field.Full = true;
					filled += 1;
				}
			} while (!visitedAll());
			
			renderer.RefreshBackground (fields);
			if (AreaFilled != null) {
				AreaFilled (this, new AreaFilledEventArgs (filled));
			}
		}

		protected List<Field> calculateReserve (List<Field> ballMap, bool[,] visited)
		{
			Field start = null;
			bool containsBall = false;
			List<Field> result = new List<Field> ();
			for (int i = 0; i < Width; i++) {
				for (int j = 0; j < Height; j++) {
					if (!fields [i, j].Full && !visited [i, j]) {
						start = fields [i, j];
						visited [i, j] = true;
						result.Add (start);
						break;
					}
				}
				if (start != null) {
					break;
				}
			}
			if (start != null) {
				Queue<Field> queue = new Queue<Field> ();
				queue.Enqueue (start);
				while (queue.Count > 0) {
					Field field = queue.Dequeue ();
					if (ballMap.Contains (field)) {
						containsBall = true;
					}
					foreach (Field neighbour in GetNeighbours(field)) {
						if (!neighbour.Full && !visited [neighbour.X, neighbour.Y]) {
							result.Add (neighbour);
							queue.Enqueue (neighbour);
						}
						visited [neighbour.X, neighbour.Y] = true;
					}
				}
			}
			if (containsBall) {
				result.Clear ();
			}
			return result;
		}

		public IEnumerable<Field> GetNeighbours (Field field)
		{
			if (field.X > 0) {
				yield return fields [field.X - 1, field.Y];
			}
			if (field.X + 1 < Width) {
				yield return fields [field.X + 1, field.Y];
			}
			if (field.Y > 0) {
				yield return fields [field.X, field.Y - 1];
			}
			if (field.Y + 1 < Height) {
				yield return fields [field.X, field.Y + 1];
			}
		}

		public void MoveBalls ()
		{
			foreach (Ball ball in balls) {
				int posX = (ball.X + FieldSize / 2) / FieldSize;
				int posY = (ball.Y + ball.dY) / FieldSize;
				if (fields [posX, posY + 1].Full && ball.dY > 0) {
					ball.BounceY ();
				}
				if (fields [posX, posY].Full && ball.dY < 0) {
					ball.BounceY ();
				}
				posX = (ball.X + ball.dX) / FieldSize;
				posY = (ball.Y + FieldSize / 2) / FieldSize;
				if (fields [posX + 1, posY].Full && ball.dX > 0) {
					ball.BounceX ();
				}
				if (fields [posX, posY].Full && ball.dX < 0) {
					ball.BounceX ();
				}
				ball.X += ball.dX;
				ball.Y += ball.dY;
			}
			checkBallCollisions ();
		}

		protected void checkBallCollisions ()
		{
			foreach (Ball ball in balls) {
				if (Player.Trail.Contains (crossedField (ball.X, ball.Y))) {
					Player.Trail.Clear ();
					Player.Place (Player.BaseField.X * FieldSize, Player.BaseField.Y * FieldSize);
					Player.HitTime = DateTime.Now;
					if (PlayerCollision != null) {
						PlayerCollision (this, EventArgs.Empty);
					}
				}
			}
		}

		public void MoveMonsters ()
		{
			foreach (Monster monster in monsters) {
				Field field = crossedField (monster.X, monster.Y);
				if (monster.Remaining == 0) {
					monster.StartMove (new NeighbourMap (field, GetNeighbours (field)), this);
					monster.Move (1);
					monster.Stop (checkedSpriteDistance (monster, calculateResidualSteps (monster)));
				} else {
					monster.Move (1);
				}
			}
			checkMonsterCollisions ();
		}

		protected void checkMonsterCollisions ()
		{
			foreach (Monster monster in monsters) {
				if (crossedField (monster.X, monster.Y) == crossedField (Player.X, Player.Y) && (DateTime.Now - Player.HitTime).TotalMilliseconds > hitLimit) {
					Player.HitTime = DateTime.Now;
					if (PlayerCollision != null) {
						PlayerCollision (this, EventArgs.Empty);
					}
				}
			}
		}

		public void MovePlayer ()
		{
			if (Player.Moving) {
				if (checkedSpriteDistance (Player, 1) == 0) {
					Player.Stop (0);
				} else {
					Player.Move (1);
				}
			} else if (Player.Remaining > 0) {
				Player.Move (1);
				if (Player.Remaining == 0 && Player.SteeringStack.Count > 0) {
					Player.StartMove (Player.SteeringStack.Last.Value);
				}
			}
			Field playerField = crossedField (Player.X, Player.Y);
			if (!playerField.Full && !Player.Trail.Contains (playerField)) {
				Player.Trail.Add (playerField);
			} else if (playerField.Full) {
				if (Player.Trail.Count > 0) {
					closeTrail ();
				}
				Player.BaseField = playerField;
			}
			checkBallCollisions ();
			checkMonsterCollisions ();
		}

		protected Field crossedField (int x, int y)
		{
			return fields [x / FieldSize, y / FieldSize];
		}

		public int checkedSpriteDistance (Sprite sprite, int steps)
		{
			int max = 0;
			switch (sprite.Direction) {
			case Direction.Down:
				max = Height * FieldSize - (sprite.Y + FieldSize);
				break;
			case Direction.Up:
				max = sprite.Y;
				break;
			case Direction.Right:
				max = Width * FieldSize - (sprite.X + FieldSize);
				break;
			case Direction.Left:
				max = sprite.X;
				break;
			}
			return Math.Min (steps, max);
		}

		public int calculateResidualSteps (Sprite sprite)
		{
			switch (sprite.Direction) {
			case Direction.Up:
				return sprite.Y % FieldSize;
			case Direction.Down:
				return FieldSize - sprite.Y % FieldSize;
			case Direction.Right:
				return FieldSize - sprite.X % FieldSize;
			case Direction.Left:
				return sprite.X % FieldSize;
			default:
				return FieldSize;
			}
		}

		public void SteerPlayer (Direction direction)
		{
			if (!Player.SteeringStack.Contains (direction)) {
				Player.SteeringStack.AddLast (direction);
				if (!Player.Moving && Player.Remaining == 0) {
					Player.StartMove (direction);
				} else {
					if (Player.Moving) {
						Player.Stop (checkedSpriteDistance (Player, calculateResidualSteps (Player)));
					}
				}
			}
		}

		public void UnsteerPlayer (Direction direction)
		{
			if (Player.SteeringStack.Contains (direction)) {
				Player.SteeringStack.Remove (direction);
				if (Player.SteeringStack.Count > 0) {
					if (Player.Direction == direction) {
						Player.Stop (checkedSpriteDistance (Player, calculateResidualSteps (Player)));
					}
					if (Player.Remaining == 0) {
						Player.StartMove (Player.SteeringStack.Last.Value);
					}
				} else {
					Player.Stop (checkedSpriteDistance (Player, calculateResidualSteps (Player)));
				}
			}
		}
	}

	public class NeighbourMap
	{
		public Field Current {
			get;
			protected set;
		}

		public Field Left {
			get;
			protected set;
		}

		public Field Up {
			get;
			protected set;
		}

		public Field Right {
			get;
			protected set;
		}

		public Field Down {
			get;
			protected set;
		}

		public NeighbourMap (Field current, IEnumerable<Field> neighbours)
		{
			this.Current = current;
			foreach (Field neighbour in neighbours) {
				if (neighbour.X == current.X - 1) {
					Left = neighbour;
				}
				if (neighbour.X == current.X + 1) {
					Right = neighbour;
				}
				if (neighbour.Y == current.Y - 1) {
					Up = neighbour;
				}
				if (neighbour.Y == current.Y + 1) {
					Down = neighbour;
				}
			}
		}
	}
}

