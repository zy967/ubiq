using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialModel
{
	public interface IGrid
	{
		ICell PlayerCell { get; set; }
		Dictionary<string, ICell> Cells { get; }
	}

	public class Grid : MonoBehaviour, IGrid
	{
		public Cell.CellEvent OnPlayerCellChanged;

		public Cell.CellEvent OnObjectEnteredCell;
		public Cell.CellEvent OnObjectLeftCell;

		public Cell.CellEvent OnEnteredCellBorder;
		public Cell.CellEvent OnLeftCellBorder;

		public List<string> activeCells = new List<string>();
		// protected Dictionary<string, int> objectsInCellCounts = new Dictionary<string, int>();

		protected string PlayerCellName; // For inspector view debugging

		protected ICell _PlayerCell;
		public ICell PlayerCell
		{
			get => _PlayerCell;
			set
			{
				 if (_PlayerCell != null)
				 {
					 ((Cell) _PlayerCell).SetBorderPointsActive(false);
				 }

				 _PlayerCell = value;
				 ((Cell) _PlayerCell).SetBorderPointsActive(true);
				 PlayerCellName = _PlayerCell.Name;
			}
		}

		protected Dictionary<string, ICell> CellDictionary;
		public Dictionary<string, ICell> Cells => CellDictionary;

		protected virtual void Awake()
		{
			OnPlayerCellChanged = new Cell.CellEvent();

			OnObjectEnteredCell = new Cell.CellEvent();
			OnObjectLeftCell = new Cell.CellEvent();

			OnEnteredCellBorder = new Cell.CellEvent();
			OnLeftCellBorder = new Cell.CellEvent();

			CellDictionary = new Dictionary<string, ICell>();
		}

		protected virtual void Start()
		{
			foreach (var item in GetComponentsInChildren<ICell>())
			{
				 CellDictionary[item.CellUuid] = item;
				 ((Cell) item).OnEnteredCell.AddListener(OnCellEntered);
				 ((Cell) item).OnLeftCell.AddListener(OnLeftCell);
				 ((Cell) item).OnCloseToCellBorder.AddListener(OnBorder);
				 ((Cell) item).OnNotCloseToCellBorder.AddListener(OnNotBorder);
			}
		}

		protected virtual void OnCellEntered(CellEventInfo info)
		{
			// Debug.Log("Grid: OnCellEntered: " + info.cell.gameObject.name + ", " + info.go.name);

			if (info.ObjectType != "Player")
			{
				 OnObjectEnteredCell.Invoke(info);
				 return;
			}

			if (PlayerCell != null && info.Cell.CellUuid == PlayerCell.CellUuid)
			{
				 return;
			}

			PlayerCell = info.Cell;
			if (OnPlayerCellChanged != null)
			{
				 OnPlayerCellChanged.Invoke(info);
			}
		}

		protected virtual void OnLeftCell(CellEventInfo info)
		{
			// Debug.Log("Cell: OnLeftCell: " + info.cell.gameObject.name);
			if (info.Object != null)
			{
				 OnObjectLeftCell.Invoke(info);
			}
		}

		protected virtual void OnBorder(CellEventInfo info)
		{
			OnEnteredCellBorder.Invoke(info);
		}

		protected virtual void OnNotBorder(CellEventInfo info)
		{
			OnLeftCellBorder.Invoke(info);
		}
	}
}