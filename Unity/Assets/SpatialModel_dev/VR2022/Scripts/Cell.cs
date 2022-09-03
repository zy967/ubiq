using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SpatialModel
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

		public CellEvent OnEnteredCell;
		public CellEvent OnLeftCell;

		public CellEvent OnCloseToCellBorder;
		public CellEvent OnNotCloseToCellBorder;

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

		// Keeps track of which objects have fired which triggers
		// to make sure cell entered/left events are invoked only at appropriate times
		protected Dictionary<string, List<string>> activeTriggers = new Dictionary<string, List<string>>();

		protected List<string> activeBorders = new List<string>();

		protected Dictionary<string, ICell> _neighbors;
		public Dictionary<string, ICell> Neighbors => _neighbors;

		public List<string> NeighborNames
		{
			get { return Neighbors.Values.ToList().Select(n => n.Name).ToList(); }
		}

		// List of neighboring cell game objects, these need to be set in the editor
		[SerializeField] public List<Cell> neighborCellObjects = new List<Cell>();

		// List of neighboring cell game objects, these need to be set in the editor
		[SerializeField] public List<CellBorderPoint> borderPointObjects;

		public Dictionary<string, CellBorderPoint> BorderPoints = new Dictionary<string, CellBorderPoint>();

		protected void SetupCell()
		{
			if (OnEnteredCell == null)
			{
				OnEnteredCell = new CellEvent();
			}

			if (OnLeftCell == null)
			{
				OnLeftCell = new CellEvent();
			}

			if (OnCloseToCellBorder == null)
			{
				OnCloseToCellBorder = new CellEvent();
			}

			if (OnNotCloseToCellBorder == null)
			{
				OnNotCloseToCellBorder = new CellEvent();
			}

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

			foreach (CellBorderPoint borderPoint in borderPointObjects)
			{
				borderPoint.fromCell = this;
				BorderPoints[borderPoint.toCell.CellUuid] = borderPoint;
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

		public void SetBorderPointsActive(bool active)
		{
			foreach (var item in BorderPoints.Values)
			{
				item.enabled = active;
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

			bool invokeEvent = false;
			bool playerTriggered = (triggerInfo.TriggeredObject.name == "Player" ||
			                        triggerInfo.TriggeredObject.CompareTag("Player"));
			if (playerTriggered) //Player triggered
			{
				if (!activeTriggers.ContainsKey("Player"))
				{
					invokeEvent = true;
					activeTriggers["Player"] = new List<string>();
					cellEventInfo.ObjectType = "Player";
				}

				activeTriggers["Player"].Add(triggerInfo.Trigger.name);
			}
			else //Something else triggered
			{
				RoomObject roomObject =
					triggerInfo.TriggeredObject.gameObject.GetComponentsInChildren<MonoBehaviour>()
						.Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
				if (roomObject != null)
				{
					// Debug.Log("HexCell: roomObject object " + triggerInfo.triggeredObject.name + " entered trigger: " + triggerInfo.trigger.name);
					if (!activeTriggers.ContainsKey(roomObject.networkId.ToString()))
					{
						// Debug.Log("HexCell: new object, create new empty list with key: " + roomObject.networkId.ToString());
						roomObject.OnObjectDestroyed.AddListener(OnObjectDestroyed);
						invokeEvent = true;
						activeTriggers[roomObject.networkId.ToString()] = new List<string>();
						cellEventInfo.ObjectType = "RoomObject";
					}

					// Debug.Log("HexCell: add trigger: " + triggerInfo.trigger.name + " to active triggers for the object");
					activeTriggers[roomObject.networkId.ToString()].Add(triggerInfo.Trigger.name);
				}
			}

			if (invokeEvent)
			{
				// Debug.Log("Hex Cell On Trigger Enter: object: " +triggerInfo.triggeredObject.name + ", entered cell: " + this.name);
				OnEnteredCell.Invoke(cellEventInfo);
			}
		}

		// Handle trigger exited events
		// Player leaving a cell does not cause an event to be invoked, player moving from one cell to another is handled only by OnCellEntered events
		protected virtual void CellTriggerExited(CellTriggerInfo triggerInfo)
		{
			bool playerTriggered = (triggerInfo.TriggeredObject.name == "Player" ||
			                        triggerInfo.TriggeredObject.CompareTag("Player"));
			if (playerTriggered)
			{
				// As player may already stay in a trigger at initial state, the key may not always exist
				if (activeTriggers?["Player"]?.Remove(triggerInfo.Trigger.name) != null)
				{
					 if (activeTriggers["Player"].Count == 0)
					 {
						 activeTriggers.Remove("Player");
						 // Debug.Log("Hex Cell On Trigger Exit: Player, left cell: " + this.name);
					 }
				}
			}
			else //Something else triggered
			{
				RoomObject roomObject =
					triggerInfo.TriggeredObject.gameObject.GetComponentsInChildren<MonoBehaviour>()
						.Where(mb => mb is RoomObject).FirstOrDefault() as RoomObject;
				if (roomObject != null && activeTriggers.ContainsKey(roomObject.networkId.ToString()))
				{
					activeTriggers[roomObject.networkId.ToString()].Remove(triggerInfo.Trigger.name);
					// Debug.Log("Hex Cell On Trigger Exit: " + triggerInfo.triggeredObject.name + " left trigger: " + triggerInfo.trigger.name);
					if (activeTriggers[roomObject.networkId.ToString()].Count == 0)
					{
						activeTriggers.Remove(roomObject.networkId.ToString());
						OnLeftCell.Invoke(new CellEventInfo
						{
							Cell = this,
							Object = triggerInfo.TriggeredObject,
							ObjectType = "RoomObject"
						});
						// Debug.Log("Hex Cell On Trigger Exit: " + triggerInfo.triggeredObject.name + " left cell: " + this.name);
					}
				}
			}
			// UpdateActiveState();
		}

		protected virtual void OnBorderEnter(CellBorderEventInfo info)
		{
			if (!activeBorders.Contains(info.BorderPoint.toCell.CellUuid))
			{
				activeBorders.Add(info.BorderPoint.toCell.CellUuid);
			}

			OnCloseToCellBorder.Invoke(new CellEventInfo
			{
				Cell = info.BorderPoint.toCell,
				Object = info.GameObject,
				ObjectType = info.ObjectType
			});
		}

		protected virtual void OnBorderExit(CellBorderEventInfo info)
		{
			activeBorders.Remove(info.BorderPoint.toCell.CellUuid);
			OnNotCloseToCellBorder.Invoke(new CellEventInfo
			{
				Cell = info.BorderPoint.toCell,
				Object = info.GameObject,
				ObjectType = info.ObjectType
			});
		}

		protected virtual void OnObjectDestroyed(RoomObjectInfo objectInfo)
		{
			// Debug.Log("HexCell: " + name + " OnObjectDestroyed: " + objectInfo.Name);
			if (activeTriggers.ContainsKey(objectInfo.Id.ToString()))
			{
				activeTriggers.Remove(objectInfo.Id.ToString());
				OnLeftCell.Invoke(new CellEventInfo
				{
					Cell = this,
					ObjectType = "RoomObject"
				});
			}
		}

		public static string GetCellUuid(ICell cell)
		{
			return new Guid(Animator.StringToHash(cell.Name),
				(short) Animator.StringToHash(cell.Coordinates.ToString()),
				(short) Animator.StringToHash(SceneManager.GetActiveScene().name),
				new byte[]
				{
					(byte) cell.Coordinates.X,
					(byte) cell.Coordinates.Y,
					(byte) cell.Coordinates.Z,
					(byte) (cell.Coordinates.X + cell.Coordinates.Y),
					(byte) (cell.Coordinates.X + cell.Coordinates.Z),
					(byte) (cell.Coordinates.Y + cell.Coordinates.Z),
					(byte) (cell.Coordinates.X + cell.Coordinates.X),
					(byte) (cell.Coordinates.Z + cell.Coordinates.Z)
				}).ToString("N");
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