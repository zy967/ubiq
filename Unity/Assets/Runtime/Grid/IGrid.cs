using System.Collections.Generic;

namespace Ubiq.Grid
{
	public interface IGrid
	{
		ICell PlayerCell { get; set; }
		Dictionary<string, ICell> Cells { get; }
	}
}