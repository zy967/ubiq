using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Grid
{
	[Serializable]
	public struct HexCoordinates : ICellCoordinates
	{
		[SerializeField] private int x, z;

		private List<HexCoordinates> neighborCoord;

		public HexCoordinates(int x, int z)
		{
			this.x = x;
			this.z = z;
			neighborCoord = null;
		}

		public HexCoordinates(Vector3 coord)
		{
			x = (int) coord.x;
			z = (int) coord.z;
			neighborCoord = null;
		}

		public int X => x;
		public int Y => 0;
		public int Z => z;

		public static HexCoordinates FromOffsetCoordinates(int x, int z)
		{
			return new HexCoordinates(x - z / 2, z);
		}

		public override string ToString()
		{
			return $"({X.ToString()}, {Z.ToString()})";
		}

		public string ToStringOnSeparateLines()
		{
			return $"{X.ToString()}\n{Z.ToString()}";
		}

		public List<HexCoordinates> GetNeighborCoordinates()
		{
			if (neighborCoord is null)
			{
				neighborCoord = new List<HexCoordinates>();
				neighborCoord.Add(new HexCoordinates(X, Z + 1));
				neighborCoord.Add(new HexCoordinates(X + 1, Z));
				neighborCoord.Add(new HexCoordinates(X + 1, Z - 1));
				neighborCoord.Add(new HexCoordinates(X, Z - 1));
				neighborCoord.Add(new HexCoordinates(X - 1, Z));
				neighborCoord.Add(new HexCoordinates(X - 1, Z + 1));
			}

			return neighborCoord;
		}

		public Vector3 AsVector3()
		{
			return new Vector3(X, Y, Z);
		}
	}
}