using System;
using System.Collections.Generic;

namespace Bounce
{
	public class Player : Sprite
	{
		public LinkedList<Direction> SteeringStack = new LinkedList<Direction> ();
		public List<Field> Trail = new List<Field> ();
		public Field BaseField;
		public DateTime HitTime = DateTime.Now;

		public Player (int x, int y): base (x, y)
		{
		}
	}
}

