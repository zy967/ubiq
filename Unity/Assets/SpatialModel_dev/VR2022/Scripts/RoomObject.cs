using System;
using SpatialModel_dev.Spatial.Scripts.Grid;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialModel_dev.VR2022.Scripts
{
	public abstract class RoomObject : MonoBehaviour, INetworkSpawnable
	{
		public RoomObjectPersistencyLevel persistencyLevel;

		public int catalogueIndex;

		public bool owner;

		public bool spawned;
		public NetworkScene context;

		public bool updateServer;
		public Rigidbody rb;
		public Vector3 currentVelocity;
		public float currentSpeed;

		public string RoomName;
		public string OwnerUUID;
		public string MyPeerUUID;

		public bool notInRoom;
		public bool inEmptyRoom;

		public float timeToDestroyObject = 10;

		private readonly NetworkId RoomServerObjectId = new NetworkId(1);

		private readonly bool stopDestoryLocalObject = false;
		private bool destroyMethodInvoked;

		protected MeshRenderer meshRenderer;

		protected bool messageProcessed;
		private bool needsSpawning = true;
		public ObjectEvent OnObjectDestroyed;
		private IPeer OwnerToAssign;

		private Vector3 previousPosition;
		private ushort RoomServerComponentId = 1;

		private RoomClient RoomToAssign;
		protected float timeRemaining;

		public string roomDictionaryKey
		{
			get => Me.GetRoomObjectInfo().RoomDictionaryKey;

			set => Me.SetRoomDictionaryKey(value);
		}

		public RoomObjectInterfaceFriend Me { get; private set; }

		public void Awake()
		{
			Debug.Log("RoomObject: Awake");

			if (OnObjectDestroyed == null) OnObjectDestroyed = new ObjectEvent();

			Me = new RoomObjectInterfaceFriend(Guid.NewGuid().ToString(), gameObject.name);

			if (context == null) context = NetworkScene.FindNetworkScene(gameObject.transform);
			rb = GetComponent<Rigidbody>();
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

			meshRenderer = GetComponent<MeshRenderer>();

			Me.SetPersistencyLevel(persistencyLevel);
		}

		// Start is called before the first frame update
		public void Start()
		{
			Debug.Log("RoomObject: Start");
			if (context == null) context = NetworkScene.FindNetworkScene(gameObject.transform);

			if (Me.GetRoomObjectInfo().MyPeer.uuid == null || Me.GetRoomObjectInfo().MyPeer.uuid == "") GetMyPeer();


			if (rb == null)
			{
				previousPosition = transform.position;
				currentVelocity = Vector3.zero;
			}
			else
			{
				currentVelocity = rb.velocity;
			}
		}

		// Update is called once per frame
		public void Update()
		{
			// if(Me.GetRoomObjectInfo().PersistencyLevel != this.persistencyLevel.EnumToString()){
			//     Me.SetPersistencyLevel(this.persistencyLevel);
			// }
			if (rb == null)
			{
				var currFrameVelocity = (transform.position - previousPosition) / Time.deltaTime;
				currentVelocity = Vector3.Lerp(currentVelocity, currFrameVelocity, 0.1f);
				previousPosition = transform.position;
			}
			else
			{
				currentVelocity = rb.velocity;
			}

			currentSpeed = currentVelocity.magnitude;
			if (inEmptyRoom && timeRemaining > 0 && owner)
				timeRemaining -= Time.deltaTime;
			// UpdateObject();
			else if (inEmptyRoom && owner) DestroyLocalObject();

			if (!inEmptyRoom && notInRoom && timeRemaining > 0)
				timeRemaining -= Time.deltaTime;

			else if (!inEmptyRoom && notInRoom && timeRemaining <= 0 && !owner)
				DestroyLocalObject();

			else if (!inEmptyRoom && notInRoom && timeRemaining <= 0 && owner) RemoveObject();
		}

		public NetworkId networkId
		{
			get => Me.GetRoomObjectInfo().Id;

			set
			{
				Me.SetNetworkId(value);
				if (roomDictionaryKey == null || roomDictionaryKey == "") roomDictionaryKey = $"SpawnedObject-{value}";
			}
		}

		// public virtual void OnSpawned(bool local)
		// {
		//     spawned = true;
		//     needsSpawning = false;
		//     owner = local;
		// }

		public void Spawned(bool local)
		{
			spawned = true;
			needsSpawning = false;
			owner = local;
		}

		public virtual void OnObjectLeftCell(Cell cell)
		{
			if (Me.GetRoomObjectInfo().Room.Me.uuid == cell.CellUuid)
			{
				timeRemaining = timeToDestroyObject;
				notInRoom = true;
			}
		}

		protected void RemoveObject()
		{
			Debug.Log("RoomObject: RemoveObject");
			SetObjectProperties(true);
			SendToServer("RemoveObject", Me.GetRoomObjectInfo());
			OnObjectDestroyed.Invoke(Me.GetRoomObjectInfo());
			Destroy(gameObject, 0.5f);
		}

		public void DestroyLocalObject()
		{
			Debug.Log("RoomObject: DestroyLocalObject");

			if (stopDestoryLocalObject) return;

			if (owner)
			{
				Me.SetOwnerUUID("");
				Me["inEmptyRoom"] = JsonUtility.ToJson(false);
				SetObjectProperties(true);
				SendToServer("LocalObjectDestroyed", Me.GetRoomObjectInfo());
			}

			inEmptyRoom = false;
			owner = false;
			destroyMethodInvoked = false;
			OnObjectDestroyed.Invoke(Me.GetRoomObjectInfo());
			// transform.position = new Vector3(transform.position.x, transform.position.y - 100.0f, transform.position.z);
			Destroy(gameObject, 0.5f);
		}


		protected virtual void SetObjectProperties(bool setVelocityZero = false)
		{
			var info = Me.GetRoomObjectInfo();
			var objectState = new RoomObjectState
			{
				position = transform.position,
				rotation = transform.rotation,
				velocity = setVelocityZero ? Vector3.zero : currentVelocity
			};
			Me[info.RoomDictionaryKey] = JsonUtility.ToJson(new RoomDictionaryInfoMessage<RoomObjectState>
			{
				catalogueIndex = catalogueIndex,
				networkId = networkId,
				persistencyLevel = info.PersistencyLevel,
				state = objectState
			});
		}

		protected virtual void SetRoomObjectInfo<T>(RoomDictionaryInfoMessage<T> msg) where T : IRoomObjectState
		{
			// RoomDictionaryInfoMessage<RoomObjectState> info = msg<RoomObjectState>;
			var info = msg;
			if (info.state is RoomObjectState)
			{
				// T state = (T) msg.state;
				var state = msg.state as RoomObjectState;
				// Debug.Log("RoomObject: SetRoomObjectInfo: catalogueIndex: " + info.catalogueIndex + ", networkId: " + info.networkId + ", position: " + info.state.position + ", rotation: " + info.state.rotation + ", velocity: " + info.state.velocity);
				transform.position = state.position;
				transform.rotation = state.rotation;

				if (rb != null) rb.velocity = state.velocity;
				currentVelocity = state.velocity;
				catalogueIndex = info.catalogueIndex;
				networkId = info.networkId;

				persistencyLevel = info.persistencyLevel.StringToEnum();
				Me.SetPersistencyLevel(persistencyLevel);
			}
		}

		protected void GetMyPeer()
		{
			SendToServer("GetObjectPeer", Me.GetRoomObjectInfo());
		}


		public void UpdateRoom(RoomClient newRoom)
		{
			notInRoom = false;
			timeRemaining = timeToDestroyObject;
			var info = Me.GetRoomObjectInfo();
			if (!owner && info.OwnerPeerUUID != "" && spawned)
			{
				Debug.Log("RoomObject: UpdateRoom: I'm not the owner, return");
				return;
			}

			Debug.Log("RoomObject: UpdateRoom: current room: " + info.Room.name + ", new room: " + newRoom.name);

			if (info.Room.Me.uuid == newRoom.Me.uuid || RoomToAssign.Me.uuid == newRoom.Me.uuid) return;
			RoomToAssign = newRoom;
			if (networkId == new NetworkId())
			{
				Debug.Log("RoomObject ID is null, create new one");

				networkId = IdGenerator.GenerateFromName(name + newRoom.name);
				Me.SetNetworkId(networkId);
			}

			SetObjectProperties();
			Debug.Log("Me[roomDictionaryKey]: " + Me[roomDictionaryKey]);

			SendToServer("UpdateObjectRoom", new RoomObjectRoomMessage
			{
				room = newRoom,
				objectInfo = Me.GetRoomObjectInfo()
			});
		}

		public void SetMyPeer(IPeer peer)
		{
			Me.SetMyPeer(peer);
			if (owner)
			{
				OwnerUUID = peer.uuid;
				Me.SetOwnerUUID(peer.uuid);
			}
		}

		public void UpdateOwner(string ownerUUID)
		{
			if (!owner) return;

			SetObjectProperties();

			SendToServer("UpdateObjectOwner", new RoomObjectOwnerMessage
			{
				uuid = ownerUUID,
				objectInfo = Me.GetRoomObjectInfo()
			});
		}


		public void UpdateObject()
		{
			Debug.Log("UpdateObject");
			SetObjectProperties();
			var info = Me.GetRoomObjectInfo();
			Debug.Log("UpdateObject: Me[roomDictionaryKey]: " + Me[roomDictionaryKey]);
			SendToServer("UpdateObject", info);
		}

		protected void SendToServer(string type, object argument)
		{
			if (context == null) context = NetworkScene.FindNetworkScene(gameObject.transform);

			Debug.Log("RoomObject: SendToServer: type: " + type + ", args: " + argument);
			context.Send(RoomServerObjectId, JsonUtility.ToJson(new Message(type, argument)));
		}


		public virtual void ProcessMessage(ReferenceCountedSceneGraphMessage message)
		{
			messageProcessed = false;
			var container = JsonUtility.FromJson<Message>(message.ToString());
			switch (container.type)
			{
				case "SetMyPeer":
				{
					Debug.Log("RoomObject: " + name + " received message to set my peer, owner: " + owner);
					var peer = JsonUtility.FromJson<IPeer>(container.args);
					Me.SetMyPeer(peer);
					MyPeerUUID = peer.uuid;

					var info = Me.GetRoomObjectInfo();
					if (owner && info.Room.Me.uuid != null && info.Room.Me.uuid != "") UpdateOwner(peer.uuid);

					if (owner)
					{
						Me.SetOwnerUUID(peer.uuid);
						OwnerUUID = peer.uuid;
					}

					messageProcessed = true;
				}
					break;
				case "OwnerUpdated":
				{
					Debug.Log("RoomObject: " + name + " received message to update owner");
					var args = JsonUtility.FromJson<RoomObjectOwnerMessage>(container.args);
					Debug.Log("RoomObject: " + name + ": msg.myPeer: " + args.myPeer.uuid);
					Debug.Log("RoomObject: " + name + ": msg.ownerPeer: " + args.uuid);
					Debug.Log("RoomObject: " + name + ": msg.objectInfo.Name: " + args.objectInfo.Name);

					if (inEmptyRoom && args.myPeer.uuid != args.uuid)
					{
						owner = false;
						DestroyLocalObject();
					}

					inEmptyRoom = false;
					Me["inEmptyRoom"] = JsonUtility.ToJson(false);
					if (owner && args.myPeer.uuid == args.uuid)
					{
						if (meshRenderer != null) meshRenderer.enabled = true;
						OwnerUUID = args.uuid;
						Me.SetOwnerUUID(args.uuid);
						messageProcessed = true;
						return;
					}

					owner = args.myPeer.uuid == args.uuid;
					OwnerUUID = args.uuid;
					Me.SetOwnerUUID(args.uuid);
					if (owner)
					{
						Me.Update(args.objectInfo);
						Debug.Log("RoomObject: object info: " + args.objectInfo);
						Debug.Log("RoomObject: RoomDictionaryKey: " + Me.GetRoomObjectInfo().RoomDictionaryKey);
						Debug.Log("RoomObject: Properties to json: " + Me[Me.GetRoomObjectInfo().RoomDictionaryKey]);
						var info = JsonUtility.FromJson<RoomDictionaryInfoMessage<IRoomObjectState>>(
							Me[Me.GetRoomObjectInfo().RoomDictionaryKey]);

						// this.catalogueIndex = info.catalogueIndex;
						SetRoomObjectInfo(info);
					}

					messageProcessed = true;
				}
					break;
				case "RoomUpdated":
				{
					Debug.Log("RoomObject: " + name + " received message to update room");
					var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);
					Debug.Log("RoomObject: Assign new room: " + args.room.name);
					Debug.Log("RoomObject: " + name + ": msg.myPeer: " + args.myPeer.uuid);
					Debug.Log("RoomObject: " + name + ": msg.objectInfo.Name: " + args.objectInfo.Name);
					Me.SetRoom(args.room);
					RoomName = args.room.name;
					notInRoom = args.room.Me.uuid == "" || args.room.Me.uuid == null;
					if (!notInRoom) timeRemaining = timeToDestroyObject;
					RoomToAssign = new RoomClient();
					var info = Me.GetRoomObjectInfo();
					if (owner && info.OwnerPeerUUID != info.MyPeer.uuid && info.Room.Me.uuid != null &&
					    info.Room.Me.uuid != "") UpdateOwner(args.myPeer.uuid);

					//The owner of the object updated the room, update this peer object server side
					if (!owner && args.room.Me.uuid != null && args.room.Me.uuid != "")
					{
						Debug.Log("RoomObject: I'm not owner check if my peer is in the same room as the object");
						SetObjectProperties();

						SendToServer("CheckPeerInObjectRoom", Me.GetRoomObjectInfo());
					}

					messageProcessed = true;
				}
					break;
				case "ObjectUpdated":
				{
					Debug.Log("RoomObject: " + name + " received message to update object");
					var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);

					Me.SetRoom(args.room);
					Me.Update(args.objectInfo);
					messageProcessed = true;
				}
					break;
				case "InEmptyRoom":
				{
					Debug.Log("RoomObject: " + name + " received message InEmptyRoom");
					var args = JsonUtility.FromJson<RoomObjectRoomMessage>(container.args);
					Me["inEmptyRoom"] = JsonUtility.ToJson(true);
					inEmptyRoom = true;
					timeRemaining = timeToDestroyObject;

					if (meshRenderer != null) meshRenderer.enabled = false;

					messageProcessed = true;
				}
					break;
				case "DestroyLocalObject":
				{
					Debug.Log("RoomObject: " + name + " received message DestroyLocalObject");
					var args = JsonUtility.FromJson<RoomObjectInfo>(container.args);

					DestroyLocalObject();
					messageProcessed = true;
				}
					break;
				case "ObjectOwnerLeft":
				{
					Debug.Log("RoomObject: " + name + " received message ObjectOwnerLeft");
					rb.isKinematic = false;
					messageProcessed = true;
				}
					break;
			}
		}

		public virtual void ObjectSpawned(RoomClient room, string jsonObjectInfo)
		{
			Debug.Log("RoomObject: ObjectSpawned: room " + room.name);
			var objectInfo = JsonUtility.FromJson<RoomDictionaryInfoMessage<IRoomObjectState>>(jsonObjectInfo);
			Debug.Log("RoomObject: ObjectSpawned: jsonObjectInfo: " + jsonObjectInfo);
			SetRoomObjectInfo(objectInfo);

			Me.SetRoom(room);
			RoomName = room.name;
		}

		[Serializable]
		protected struct Message
		{
			public string type;
			public string args;

			public Message(string type, object args)
			{
				this.type = type;
				this.args = JsonUtility.ToJson(args);
			}
		}

		public class RoomObjectInterfaceFriend : RoomObjectInterface
		{
			public RoomObjectInterfaceFriend(string uuid, string name) : base(name)
			{
			}

			public void Update(RoomObjectInfo args)
			{
				Name = args.Name;
				NetworkId = args.Id;
				RoomDictionaryKey = args.RoomDictionaryKey;
				PersistencyLevel = args.PersistencyLevel.StringToEnum();
				OwnerPeerUUID = args.OwnerPeerUUID;
				properties = new SerializableDictionary(args.Properties);
			}

			public void SetRoom(RoomClient room)
			{
				Room = room;
			}

			public void SetMyPeer(IPeer peer)
			{
				MyPeer = peer;
			}

			public void SetOwnerUUID(string uuid)
			{
				OwnerPeerUUID = uuid;
			}

			public void SetNetworkId(NetworkId id)
			{
				NetworkId = id;
			}

			public void SetRoomDictionaryKey(string key)
			{
				RoomDictionaryKey = key;
			}

			public void SetPersistencyLevel(RoomObjectPersistencyLevel persistencyLevel)
			{
				PersistencyLevel = persistencyLevel;
			}
		}

		[Serializable]
		public class RoomDictionaryInfoMessage<T>
			where T : IRoomObjectState // This data will be stored in the room dictionary
		{
			public int catalogueIndex;
			public NetworkId networkId;
			public string persistencyLevel;
			public T state;
		}

		[Serializable]
		public struct RoomObjectOwnerMessage // Structure for updating object onwer message
		{
			public string uuid;
			public RoomObjectInfo objectInfo;
			public IPeer myPeer;
		}

		[Serializable]
		public struct RoomObjectRoomMessage // Structure for updating object room message
		{
			public RoomClient room;
			public RoomObjectInfo objectInfo;
			public IPeer myPeer;
		}

		[Serializable]
		public class RoomObjectStateMessage // Structure for updating object state message
		{
			public RoomObjectInfo objectInfo;
			public IRoomObjectState state;
		}

		// Marker interface to enable function/struct inheritance
		// Child class that creates a new State structure should mark the struct as IRoomObjectState
		[Serializable]
		public class IRoomObjectState
		{
		}


		[Serializable]
		public class
			RoomObjectState : IRoomObjectState // Structure for object state, child objects likely to have their own state implementation
		{
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 velocity;
		}

		public class ObjectEvent : UnityEvent<RoomObjectInfo>
		{
		}
	}
}