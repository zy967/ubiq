using System.Collections.Generic;

namespace SpatialModel_dev.VR2022.Scripts
{
	public interface IGrid
	{
		ICell PlayerCell { get; set; }
		Dictionary<string, ICell> Cells { get; }
	}
}