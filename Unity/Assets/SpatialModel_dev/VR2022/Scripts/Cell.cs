using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class Cell : MonoBehaviour, ICell
	{
		// List of neighboring cell game objects, these need to be set in the editor
		[SerializeField] public List<Cell> neighborCellObjects = new List<Cell>();

		protected ICellCoordinates _Coordinates;

		protected IGrid _Grid;

		protected Dictionary<string, ICell> _neighbors;

		// List of neighboring cell game objects, these need to be set in the editor
		// [SerializeField] public List<CellBorderPoint> borderPointObjects;

		public Dictionary<string, CellBorderPoint> BorderPoints = new Dictionary<string, CellBorderPoint>();

		// For debug visualisation
		protected Canvas CellCanvas;
		protected Text CellLabel;

		public CellEvent OnCloseToBorder;

		public CellEvent OnEntered;
		public CellEvent OnExist;
		public CellEvent OnNotCloseToBorder;


		protected virtual void Awake()
		{
			SetupCell();
			Coordinates = new CellCoordinates(transform.position);
		}

		protected virtual void Start()
		{
			foreach (var cell in neighborCellObjects) _neighbors[cell.CellUuid] = cell;

			if (CellCanvas != null) CellLabel.text = ((CellCoordinates) Coordinates).ToStringOnSeparateLines();

			foreach (var trigger in GetComponentsInChildren<CellTrigger>())
			{
				trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
				trigger.OnTriggerExited.AddListener(CellTriggerExited);
			}
		}

		public string CellUuid => GetCellUuid(this);

		public string Name => name;

		public IGrid Grid
		{
			get => _Grid;
			set => _Grid = value;
		}

		public ICellCoordinates Coordinates
		{
			get => _Coordinates;
			set => _Coordinates = value;
		}

		public Dictionary<string, ICell> Neighbors => _neighbors;

		public List<string> NeighborNames
		{
			get { return Neighbors.Values.ToList().Select(n => n.Name).ToList(); }
		}

		protected void SetupCell()
		{
			OnEntered = new CellEvent();
			OnExist = new CellEvent();
			OnCloseToBorder = new CellEvent();
			OnNotCloseToBorder = new CellEvent();

			Grid = GetComponentInParent<IGrid>();

			_neighbors = new Dictionary<string, ICell>();

			CellCanvas = GetComponentInChildren<Canvas>();
			CellLabel = GetComponentInChildren<Text>();
		}

		// Handle cell trigger entered events, if a new object has entered, invokes OnCellEntered event
		protected virtual void CellTriggerEntered(CellTriggerInfo triggerInfo)
		{
			var cellEventInfo = new CellEventInfo
			{
				Cell = this,
				Object = triggerInfo.TriggeredObject
			};

			if (triggerInfo.TriggeredObject.CompareTag("Player")) //Player triggered
			{
				cellEventInfo.ObjectType = "Player";
				OnEntered.Invoke(cellEventInfo);
			}
		}

		// Handle trigger exited events
		// Player leaving a cell does not cause an event to be invoked, player moving from one cell to another is handled only by OnCellEntered events
		protected virtual void CellTriggerExited(CellTriggerInfo triggerInfo)
		{
		}

		protected virtual void OnObjectDestroyed(RoomObjectInfo objectInfo)
		{
			// Debug.Log("HexCell: " + name + " OnObjectDestroyed: " + objectInfo.Name);
			OnExist.Invoke(new CellEventInfo
			{
				Cell = this,
				ObjectType = "RoomObject"
			});
		}

		public static string GetCellUuid(ICell cell)
		{
			return GetCellUuid(cell.Name, cell.Coordinates);
		}

		public static string GetCellUuid(string cellName, ICellCoordinates coords)
		{
			return new Guid(Animator.StringToHash(cellName),
				(short) Animator.StringToHash(coords.ToString()),
				(short) Animator.StringToHash(SceneManager.GetActiveScene().name),
				new[]
				{
					(byte) coords.X,
					(byte) coords.Y,
					(byte) coords.Z,
					(byte) (coords.X + coords.Y),
					(byte) (coords.X + coords.Z),
					(byte) (coords.Y + coords.Z),
					(byte) (coords.X + coords.X),
					(byte) (coords.Z + coords.Z)
				}).ToString("N");
		}

		public class CellEvent : UnityEvent<CellEventInfo>
		{
		}
	}
}