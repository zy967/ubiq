using UnityEngine;

namespace SpatialModel_dev.Spatial.Scripts.Grid
{
	public class HexGrid : Grid
	{
		public float outerRadius = 20f;

		public int width = 6;
		public int height = 6;
		public bool showCells;
		public bool expanding;
		public HexCell cellPrefab;
		public float InnerRadius => outerRadius * 0.866025404f;

		public Vector3[] Corners
		{
			get
			{
				Vector3[] arr =
				{
					new Vector3(0f, 0f, outerRadius), new Vector3(InnerRadius, 0f, 0.5f * outerRadius),
					new Vector3(InnerRadius, 0f, -0.5f * outerRadius), new Vector3(0f, 0f, -outerRadius),
					new Vector3(-InnerRadius, 0f, -0.5f * outerRadius),
					new Vector3(-InnerRadius, 0f, 0.5f * outerRadius), new Vector3(0f, 0f, outerRadius)
				};
				return arr;
			}
		}

		// Start is called before the first frame update
		protected override void Start()
		{
			CreateFixedSizeGrid();
		}

		private void CreateFixedSizeGrid()
		{
			for (var z = -height / 2; z <= height / 2; z++)
			for (var x = -width / 2; x <= width / 2; x++)
				CreateCell(x, z);
		}

		private HexCell CreateCell(int x, int z)
		{
			var cell = Instantiate(cellPrefab, transform, false);
			cell.Grid = this;
			cell.transform.localPosition = new Vector3((x + z * 0.5f - (int) (z / 2.0f)) * (InnerRadius * 2f), 0f,
				z * (outerRadius * 1.5f));
			cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
			cell.name = "Hex Cell " + (HexCoordinates) cell.Coordinates;
			CellDictionary[Cell.GetCellUuid(cell)] = cell;

			cell.OnEntered.AddListener(OnCellEntered);
			cell.OnExist.AddListener(OnLeftCell);
			cell.OnCloseToBorder.AddListener(OnBorder);
			cell.OnNotCloseToBorder.AddListener(OnNotBorder);

			cell.SetVisible(showCells);
			return cell;
		}

		protected override void OnCellEntered(CellEventInfo info)
		{
			// Debug.Log("HexGrid: OnCellEntered: " + info.cell.Name + ", " + info.go.name);
			if (info.ObjectType != "Player")
			{
				OnObjectEnteredCell.Invoke(info);
				return;
			}

			var cell = (HexCell) info.Cell;

			if (PlayerCell?.CellUuid == cell.CellUuid) return;

			PlayerCell = info.Cell;

			if (expanding) ExpandGridOnCell(cell);

			OnPlayerCellChanged.Invoke(info);
		}

		private void ExpandGridOnCell(HexCell cell)
		{
			foreach (var item in ((HexCoordinates) cell.Coordinates).GetNeighborCoordinates())
			{
				var key = Cell.GetCellUuid("Hex Cell " + item, item);
				// Debug.Log("Player Cell Neighbor Coordinate" + item.ToString());
				if (!CellDictionary.ContainsKey(key))
				{
					// Debug.Log("Create new Cell for Player Cell Neighbor Coordinate");
					var neighbor = CreateCell(item.X + item.Z / 2, item.Z);
					PlayerCell.Neighbors[key] = neighbor;
					neighbor.Neighbors[PlayerCell.CellUuid] = (HexCell) PlayerCell;
				}
			}
		}
	}
}