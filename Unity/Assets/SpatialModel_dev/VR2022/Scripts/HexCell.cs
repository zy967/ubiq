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
		public CellBorderPoint cellBorderPointPrefab;
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
				name = "Hex Cell " + Coordinates.ToString();
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

					CreateBorderPoints(trigger);

					idx++;
				}

				if (CellCanvas != null)
				{
					CellLabel.text = ((HexCoordinates) Coordinates).ToStringOnSeparateLines();
					CellCanvas.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f) * Grid.outerRadius;
				}
			}
		}

		private void CreateBorderPoints(CellTrigger trigger)
		{
			Vector3[] borderPoints = trigger.GetBorderPoints();
			foreach (var borderPoint in borderPoints)
			{
				var distanceToNeighborCell = float.MaxValue;

				foreach (var neighbor in _neighbors)
				{
					var distance = Vector3.Distance(((HexCell) neighbor.Value).transform.position, borderPoint);
					if (distance < distanceToNeighborCell)
					{
						CellBorderPoint cellBorderPoint;
						if (BorderPoints.ContainsKey(neighbor.Key) &&
						    BorderPoints[neighbor.Key].distanceToCell < distance)
						{
							continue;
						}

						if (BorderPoints.ContainsKey(neighbor.Key))
						{
							cellBorderPoint = BorderPoints[neighbor.Key];
						}
						else
						{
							cellBorderPoint = Instantiate(cellBorderPointPrefab);
							cellBorderPoint.enabled = false;
							cellBorderPoint.fromCell = this;
							cellBorderPoint.OnBorderTriggerEntered.AddListener(OnBorderEnter);
							cellBorderPoint.OnBorderTriggerExited.AddListener(OnBorderExit);
							cellBorderPoint.gameObject.AddComponent<BoxCollider>();
						}

						cellBorderPoint.transform.parent = transform;

						BoxCollider borderCollider = cellBorderPoint.gameObject.GetComponent<BoxCollider>();
						borderCollider.enabled = false;
						Vector3 colliderSize = trigger.gameObject.GetComponent<BoxCollider>().size;
						var triggerTransform = trigger.transform;
						var triggerLocalScale = triggerTransform.localScale;

						colliderSize.x *= triggerLocalScale.x * 1.0f;
						colliderSize.y *= triggerLocalScale.y;
						colliderSize.z *= (triggerLocalScale.z / 5);
						borderCollider.size = colliderSize;

						Vector3 newPosition = Vector3.MoveTowards(borderPoint, transform.position, colliderSize.z / 2);
						newPosition.y = triggerTransform.position.y;
						cellBorderPoint.transform.position = newPosition;
						borderCollider.transform.rotation = triggerTransform.rotation;

						cellBorderPoint.toCell = (Cell) neighbor.Value;
						cellBorderPoint.name = "Border To Cell " + cellBorderPoint.toCell.Name;
						cellBorderPoint.distanceToCell = distance;
						distanceToNeighborCell = distance;
						BorderPoints[neighbor.Key] = cellBorderPoint;
						SetBorderPointsActive(false);
					}
				}
			}
		}

		public void SetVisible(bool visible)
		{
			hexMesh.GetComponent<MeshRenderer>().enabled = visible;
		}
	}
}