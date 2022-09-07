using System;
using UnityEngine;

namespace Ubiq.Grid
{
	[Serializable]
	public struct CellCoordinates : ICellCoordinates
	{
		[SerializeField] private int x, y, z;

		public CellCoordinates(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public CellCoordinates(Vector3 coord)
		{
			x = (int) coord.x;
			y = (int) coord.y;
			z = (int) coord.z;
		}

		public int X => x;
		public int Y => y;
		public int Z => z;

		public override string ToString()
		{
			return $"({X.ToString()}, {Y.ToString()}, {Z.ToString()})";
		}

		public string ToStringOnSeparateLines()
		{
			return $"{X.ToString()}\n{Y.ToString()}\n{Z.ToString()}";
		}

		public Vector3 AsVector3()
		{
			return new Vector3(X, Y, Z);
		}
	}
}