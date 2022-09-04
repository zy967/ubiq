using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpatialModel_dev.VR2022.Scripts
{
	public interface ICellCoordinates
	{
		int X { get; }
		int Y { get; }
		int Z { get; }
	}

	[Serializable]
	public struct CellCoordinates : ICellCoordinates
	{
		[SerializeField] private int x, y, z;

		public int X => x;
		public int Y => y;
		public int Z => z;

		public CellCoordinates(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public CellCoordinates(Vector3 coord)
		{
			x = (int) coord.x;
			y = (int) coord.y;
			z = (int) coord.z;
		}

		public override string ToString()
		{
			return $"({X.ToString()}, {Y.ToString()}, {Z.ToString()})";
		}

		public string ToStringOnSeparateLines()
		{
			return $"{X.ToString()}\n{Y.ToString()}\n{Z.ToString()}";
		}
	}

	public struct CellEventInfo
	{
		public Cell Cell;
		public GameObject Object;
		public string ObjectType;
	}

	public interface ICell
	{
		List<string> NeighborNames { get; }
		Dictionary<string, ICell> Neighbors { get; }
		ICellCoordinates Coordinates { get; set; }
		string Name { get; }
		string CellUuid { get; }
		IGrid Grid { get; set; }
	}

	public class Cell : MonoBehaviour, ICell
	{
		public class CellEvent : UnityEvent<CellEventInfo>
		{
		};

		public CellEvent OnEntered;
		public CellEvent OnExist;

		public CellEvent OnCloseToBorder;
		public CellEvent OnNotCloseToBorder;

		public string CellUuid => GetCellUuid(this);

		public string Name => this.name;

		protected IGrid _Grid;
		public IGrid Grid
		{
			get => _Grid;
			set => _Grid = value;
		}

		protected ICellCoordinates _Coordinates;
		public ICellCoordinates Coordinates
		{
			get => _Coordinates;
			set => _Coordinates = value;
		}

		// For debug visualisation
		protected Canvas CellCanvas;
		protected Text CellLabel;

		protected Dictionary<string, ICell> _neighbors;
		public Dictionary<string, ICell> Neighbors => _neighbors;

		public List<string> NeighborNames
		{
			get { return Neighbors.Values.ToList().Select(n => n.Name).ToList(); }
		}

		// List of neighboring cell game objects, these need to be set in the editor
		[SerializeField] public List<Cell> neighborCellObjects = new List<Cell>();

		// List of neighboring cell game objects, these need to be set in the editor
		// [SerializeField] public List<CellBorderPoint> borderPointObjects;

		public Dictionary<string, CellBorderPoint> BorderPoints = new Dictionary<string, CellBorderPoint>();

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


		protected virtual void Awake()
		{
			SetupCell();
			Coordinates = new CellCoordinates(transform.position);
		}

		protected virtual void Start()
		{
			foreach (Cell cell in neighborCellObjects)
			{
				_neighbors[cell.CellUuid] = cell;
			}

			if (CellCanvas != null)
			{
				CellLabel.text = ((CellCoordinates) Coordinates).ToStringOnSeparateLines();
			}

			foreach (var trigger in GetComponentsInChildren<CellTrigger>())
			{
				trigger.OnTriggerEntered.AddListener(CellTriggerEntered);
				trigger.OnTriggerExited.AddListener(CellTriggerExited);
			}
		}

		// Handle cell trigger entered events, if a new object has entered, invokes OnCellEntered event
		protected virtual void CellTriggerEntered(CellTriggerInfo triggerInfo)
		{
			CellEventInfo cellEventInfo = new CellEventInfo
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
				new byte[]
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
	}
}