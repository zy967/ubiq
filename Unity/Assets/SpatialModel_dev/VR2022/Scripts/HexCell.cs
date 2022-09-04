using System.Collections.Generic;
using UnityEngine;

namespace SpatialModel
{
	[System.Serializable]
	public struct HexCoordinates : ICellCoordinates
	{
		[SerializeField] private int x, z;

		public int X => x;
		public int Y => 0;
		public int Z => z;

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
				neighborCoord.Add(new HexCoordinates(this.X, this.Z + 1));
				neighborCoord.Add(new HexCoordinates(this.X + 1, this.Z));
				neighborCoord.Add(new HexCoordinates(this.X + 1, this.Z - 1));
				neighborCoord.Add(new HexCoordinates(this.X, this.Z - 1));
				neighborCoord.Add(new HexCoordinates(this.X - 1, this.Z));
				neighborCoord.Add(new HexCoordinates(this.X - 1, this.Z + 1));
			}

			return neighborCoord;
		}
	}

	public class HexCell : Cell
	{
		private HexMesh hexMesh;
		private new HexGrid Grid => (HexGrid) base.Grid;

		protected override void Awake()
		{
			SetupCell();

			hexMesh = GetComponentInChildren<HexMesh>();
			// cellCanvas = GetComponentInChildren<Canvas>();
			if (base.Grid != null && !(base.Grid is HexGrid))
			{
				Coordinates = new CellCoordinates(transform.position);
				name = "Hex Cell " + Coordinates;
			}
		}

		// Start is called before the first frame update
		protected override void Start()
		{
			// Debug.Log("Hex Cell Start: " + name + " uuid: " + CellUUID);

			if (base.Grid is HexGrid)
			{
				hexMesh.Triangulate(this, Grid);
				// Debug.Log("Hex Cell Start: " + name + " cell dictionary count: " + Grid.cellDictionary.Count);
				foreach (var item in ((HexCoordinates) Coordinates).GetNeighborCoordinates())
				{
					string key = GetCellUuid("Hex Cell " + item.ToString(), item);

					if (Grid.Cells.ContainsKey(key))
					{
						_neighbors[key] = (HexCell) Grid.Cells[key];
					}
				}

				var idx = 1;
				foreach (var trigger in GetComponentsInChildren<CellTrigger>())
				{
					var triggerTransform = trigger.transform;
					triggerTransform.localPosition = new Vector3(0, 0, 0);
					// triggerTransform.localScale = new Vector3(Grid.outerRadius, 2f, Grid.InnerRadius * 2 - 0.5f);
					triggerTransform.localScale = new Vector3(Grid.InnerRadius, Grid.InnerRadius, Grid.InnerRadius);
					trigger.name = name + " Trigger " + idx;

					trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
					trigger.OnTriggerExited.AddListener(CellTriggerExited);

					// CreateBorderPoints(trigger);

					idx++;
				}

				if (CellCanvas != null)
				{
					CellLabel.text = ((HexCoordinates) Coordinates).ToStringOnSeparateLines();
					CellCanvas.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f) * Grid.outerRadius;
				}
			}
		}

		public void SetVisible(bool visible)
		{
			hexMesh.GetComponent<MeshRenderer>().enabled = visible;
			CellCanvas.enabled = visible;
		}
	}
}