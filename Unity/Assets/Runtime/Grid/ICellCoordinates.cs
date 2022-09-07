using UnityEngine;

namespace Ubiq.Grid
{
	public interface ICellCoordinates
	{
		int X { get; }
		int Y { get; }
		int Z { get; }

		public Vector3 AsVector3();
	}
}