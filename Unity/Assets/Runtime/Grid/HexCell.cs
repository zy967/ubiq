using UnityEngine;

namespace Ubiq.Grid
{
	public class HexCell : Cell
	{
		private new HexGrid Grid => (HexGrid) base.Grid;

		protected override void Awake()
		{
			SetupCell();
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
			// cellMeshesName = cellMeshes.Values.ToList();

			// Debug.Log("Hex Cell Start: " + name + " uuid: " + CellUUID);

			if (base.Grid is HexGrid)
			{
				// hexMesh.Triangulate(this, Grid);
				// Debug.Log("Hex Cell Start: " + name + " cell dictionary count: " + Grid.cellDictionary.Count);
				foreach (var item in ((HexCoordinates) Coordinates).GetNeighborCoordinates())
				{
					var key = GetCellUuid("Hex Cell " + item, item);

					if (Grid.Cells.ContainsKey(key)) _neighbors[key] = (HexCell) Grid.Cells[key];
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

				foreach (var kvp in cellMeshes)
				{
					kvp.Value.transform.localScale = new Vector3(Grid.outerRadius * 2, Grid.outerRadius * 2, 1);
				}

				if (CellCanvas != null)
				{
					CellLabel.text = ((HexCoordinates) Coordinates).ToStringOnSeparateLines();
					CellCanvas.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f) * Grid.outerRadius;
				}
			}
		}

		public void FixedUpdate()
		{
			ResetShow();
		}
	}
}