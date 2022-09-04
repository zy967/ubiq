using System.Collections.Generic;
using System.Linq;
using Ubiq.Grid;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using Grid = Ubiq.Grid.Grid;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class GridManager : MonoBehaviour
	{
		public Grid grid;
		public NetworkScene networkScene;
		public RoomClient client;
		public float RoomRefreshRate = 50.0f;
		public Dictionary<string, RoomClient> availableRooms = new Dictionary<string, RoomClient>();

		private bool joinRoom;

		private float timeRoomsLastDiscovered;

		private void Awake()
		{
			if (networkScene == null)
			{
				var n = FindObjectOfType<NetworkScene>();
				if (n != null) networkScene = n;
			}

			if (grid == null)
			{
				var g = FindObjectOfType<Grid>();
				if (g != null) grid = g;
			}

			client = networkScene.GetComponentInChildren<RoomClient>();
		}

		// Start is called before the first frame update
		private void Start()
		{
			client.OnJoinedRoom.AddListener(OnJoinedRoom);
			client.OnJoinedRoom.AddListener(OnRoomCreated);
			grid.OnPlayerCellChanged.AddListener(OnPlayerCellChanged);
			grid.OnObjectEnteredCell.AddListener(OnObjectChangedCell);
			grid.OnObjectLeftCell.AddListener(OnObjectLeftCell);
			grid.OnEnteredCellBorder.AddListener(OnCellBorder);
			grid.OnLeftCellBorder.AddListener(OnLeftCellBorder);
		}

		private void OnObjectLeftCell(CellEventInfo info)
		{
			var roomObject =
				info.Object.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject)
					.FirstOrDefault() as RoomObject;
			if (roomObject != null) roomObject.OnObjectLeftCell(info.Cell);
		}

		private void OnObjectChangedCell(CellEventInfo info)
		{
			var roomObject =
				info.Object.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject)
					.FirstOrDefault() as RoomObject;
			if (roomObject != null)
			{
				if (availableRooms.ContainsKey(info.Cell.CellUuid))
				{
					roomObject.UpdateRoom(availableRooms[info.Cell.CellUuid]);
				}
				else
				{
					// Server will create a new room with the cell name
					// TODO: not actually working
					var newRoom = new RoomClient();
					roomObject.UpdateRoom(newRoom);
				}
			}
		}

		private void OnPlayerCellChanged(CellEventInfo info)
		{
			if (availableRooms.Count == 0 || !availableRooms.ContainsKey(info.Cell.CellUuid))
			{
				joinRoom = true;
				timeRoomsLastDiscovered = Time.time;
			}
			else if (availableRooms.Count > 0)
			{
				joinRoom = false;
				JoinRoom(availableRooms[info.Cell.CellUuid]);
				timeRoomsLastDiscovered = Time.time;
			}
		}

		private void OnCellBorder(CellEventInfo info)
		{
			if (info.Object.name == "Player" || info.Object.tag == "Player")
			{
				/*
			if(!client.RoomsToObserve.ContainsKey(info.cell.CellUUID))
			{
				client.Observe(new RoomInfo(info.cell.Name, info.cell.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
			}
			*/
			}
		}

		private void OnLeftCellBorder(CellEventInfo info)
		{
			if (info.Object.name == "Player" || info.Object.tag == "Player")
			{
				/*
			if(client.RoomsToObserve.ContainsKey(info.cell.CellUUID))
			{
				client.StopObserve(new RoomInfo(info.cell.Name, info.cell.CellUUID, "", true, new Ubiq.Dictionaries.SerializableDictionary()));
			}
			*/
			}
		}

		private void OnRoomsAvailable(List<IRoom> rooms)
		{
			// Debug.Log("GridManager: OnRoomsAvailable");
			var roomFound = false;
			availableRooms.Clear();
			foreach (var room in rooms)
			{
				availableRooms[room.UUID] = (RoomClient) room;
				if (joinRoom && grid.PlayerCell != null && grid.PlayerCell.CellUuid == room.UUID)
				{
					roomFound = true;
					JoinRoom((RoomClient) room);
				}
			}

			if (grid.PlayerCell != null && joinRoom && !roomFound)
			{
				CreateRoom(grid.PlayerCell.Name, grid.PlayerCell.CellUuid);
				availableRooms[grid.PlayerCell.CellUuid] = new RoomClient();
			}

			if (grid.PlayerCell != null && !joinRoom) UpdateObservedRooms();

			joinRoom = false;
		}

		public void UpdateObservedRooms()
		{
		}

		public void JoinRoom(RoomClient room)
		{
			// Start observing to the current room
			if (client.Room.UUID != null && client.Room.UUID != "")
			{
				// Debug.Log("Observe current room");
			}

			// Join the new room, leaves the current room in the process
			// If I'm already observing to the new room, also stops observing it
			client.Join(room.Room.JoinCode);
		}

		public void CreateRoom(string name, string uuid)
		{
		}

		private void OnJoinedRoom(IRoom room)
		{
		}

		private void OnRoomCreated(IRoom room)
		{
			JoinRoom((RoomClient) room);
		}
	}
}