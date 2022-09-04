using System.Collections.Generic;

namespace SpatialModel_dev.Spatial.Scripts.Grid
{
	public interface IGrid
	{
		ICell PlayerCell { get; set; }
		Dictionary<string, ICell> Cells { get; }
	}
}