using UnityEngine;

namespace SpatialModel
{
	public class HexGrid : Grid
	{
		public float outerRadius = 20f;
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

		public int width = 6;
		public int height = 6;
		public bool showCells;
		public bool expanding;
		public HexCell cellPrefab;

		// Start is called before the first frame update
		protected override void Start()
		{
			CreateFixedSizeGrid();
		}

		private void CreateFixedSizeGrid()
		{
			for (int z = -height / 2; z <= height / 2; z++)
			{
				for (int x = -width / 2; x <= width / 2; x++)
				{
					CreateCell(x, z);
				}
			}
		}

		HexCell CreateCell(int x, int z)
		{
			Vector3 position = new Vector3((x + z * 0.5f - (int) (z / 2.0f)) * (InnerRadius * 2f), 0f,
				z * (outerRadius * 1.5f));
			HexCell cell = Instantiate(cellPrefab, transform, false);
			cell.Grid = this;
			cell.transform.localPosition = position;
			cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
			cell.name = "Hex Cell " + ((HexCoordinates) cell.Coordinates).ToString();
			var key = Cell.GetCellUuid(cell);
			CellDictionary[key] = cell;
			cell.OnEnteredCell.AddListener(OnCellEntered);
			cell.OnLeftCell.AddListener(OnLeftCell);
			cell.OnCloseToCellBorder.AddListener(OnBorder);
			cell.OnNotCloseToCellBorder.AddListener(OnNotBorder);
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

			HexCell cell = (HexCell) info.Cell;

			if (PlayerCell?.CellUuid == cell.CellUuid)
			{
				return;
			}

			PlayerCell = info.Cell;

			if (expanding)
			{
				ExpandGridOnCell(cell);
			}

			OnPlayerCellChanged.Invoke(info);
		}

		private void ExpandGridOnCell(HexCell cell)
		{
			foreach (var item in ((HexCoordinates) cell.Coordinates).GetNeighborCoordinates())
			{
				var key = Cell.GetCellUuid("Hex Cell " + item.ToString(), item);
				// Debug.Log("Player Cell Neighbor Coordinate" + item.ToString());
				if (!CellDictionary.ContainsKey(key))
				{
					// Debug.Log("Create new Cell for Player Cell Neighbor Coordinate");
					HexCell neighbor = CreateCell(item.X + item.Z / 2, item.Z);
					PlayerCell.Neighbors[key] = neighbor;
					neighbor.Neighbors[PlayerCell.CellUuid] = (HexCell) PlayerCell;
				}
			}
		}
	}
}