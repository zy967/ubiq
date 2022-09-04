using System.Collections.Generic;

namespace SpatialModel_dev.VR2022.Scripts
{
	public interface ICell
	{
		List<string> NeighborNames { get; }
		Dictionary<string, ICell> Neighbors { get; }
		ICellCoordinates Coordinates { get; set; }
		string Name { get; }
		string CellUuid { get; }
		IGrid Grid { get; set; }
	}
}