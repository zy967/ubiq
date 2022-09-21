using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Grid
{
	public class Grid : MonoBehaviour, IGrid
	{
		public List<string> activeCells = new List<string>();

		protected ICell _playerCell;

		protected Dictionary<string, ICell> CellDictionary;

		public Cell.CellEvent OnEnteredCellBorder;
		public Cell.CellEvent OnLeftCellBorder;

		public Cell.CellEvent OnObjectEnteredCell;
		public Cell.CellEvent OnObjectLeftCell;

		public Cell.CellEvent OnPlayerCellChanged;
		// protected Dictionary<string, int> objectsInCellCounts = new Dictionary<string, int>();

		protected string PlayerCellName; // For inspector view debugging

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
				((Cell) item).OnEntered.AddListener(OnCellEntered);
				((Cell) item).OnExist.AddListener(OnLeftCell);
				((Cell) item).OnCloseToBorder.AddListener(OnBorder);
				((Cell) item).OnNotCloseToBorder.AddListener(OnNotBorder);
			}
		}

		public ICell PlayerCell
		{
			get => _playerCell;
			set
			{
				_playerCell = value;
				PlayerCellName = _playerCell.Name;
			}
		}

		public Dictionary<string, ICell> Cells => CellDictionary;

		protected virtual void OnCellEntered(CellEventInfo info)
		{
			// Debug.Log("Grid: OnCellEntered: " + info.cell.gameObject.name + ", " + info.go.name);

			if (info.ObjectType != "Player")
			{
				OnObjectEnteredCell.Invoke(info);
				return;
			}

			if (PlayerCell != null && info.Cell.CellUuid == PlayerCell.CellUuid) return;

			PlayerCell = info.Cell;
			if (OnPlayerCellChanged != null) OnPlayerCellChanged.Invoke(info);
		}

		protected virtual void OnLeftCell(CellEventInfo info)
		{
			// Debug.Log("Cell: OnExist: " + info.cell.gameObject.name);
			if (info.Object != null) OnObjectLeftCell.Invoke(info);
		}

		protected virtual void OnBorder(CellEventInfo info)
		{
			OnEnteredCellBorder.Invoke(info);
		}

		protected virtual void OnNotBorder(CellEventInfo info)
		{
			OnLeftCellBorder.Invoke(info);
		}

		public ICell GetCell(string cellUuid)
		{
			return CellDictionary[cellUuid];
		}
	}
}