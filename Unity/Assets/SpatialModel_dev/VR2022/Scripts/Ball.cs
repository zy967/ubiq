using System;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.XR;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class Ball : RoomObject, IUseable, IBall
	{
		public float throwStrength = 1f;
		private Hand follow;

		private BallState previousState;


		private void Awake()
		{
			Debug.Log("Ball: Awake");
			base.Awake();
		}

		// Start is called before the first frame update
		private void Start()
		{
			Debug.Log("Ball: Start");
			base.Start();
		}

		// Update is called once per frame
		private void Update()
		{
			base.Update();
			if (follow != null)
			{
				owner = true;
				rb.isKinematic = false;
				transform.position = follow.transform.position;
				transform.rotation = follow.transform.rotation;
			}

			if (owner && stateChanged()) UpdateObjectState();
		}

		public void Attach(Hand hand)
		{
			Debug.Log("Ball: Attach");
			follow = hand;
			owner = true;
		}

		public void UnUse(Hand controller)
		{
		}

		public void Use(Hand controller)
		{
			Debug.Log("Ball: Use");
			if (OwnerUUID == "")
			{
				owner = true;
				UpdateOwner(MyPeerUUID);
			}

			follow = null;
			rb.isKinematic = false;
			rb.AddForce(controller.transform.forward * throwStrength, ForceMode.Impulse);
		}

		private void UpdateObjectState()
		{
			SetObjectProperties();

			previousState = new BallState
			{
				position = transform.position,
				rotation = transform.rotation,
				velocity = currentVelocity,
				grasped = true
			};
			Debug.Log("UpdateObjectState: previousState: " + previousState.position + ", " + previousState.velocity);
			SendToServer("UpdateObjectState", new BallStateMessage
			{
				objectInfo = Me.GetRoomObjectInfo(),
				state = previousState
			});
		}

		private bool stateChanged()
		{
			if (previousState != null && previousState.position == transform.position &&
			    previousState.rotation == transform.rotation)
				return false;
			return true;
		}

		public void Grasp(Hand controller)
		{
			follow = controller;
			if (!owner)
			{
				owner = true;
				UpdateOwner(Me.GetRoomObjectInfo().MyPeer.uuid);
			}
		}

		public void Release(Hand controller)
		{
			follow = null;
			if (rb != null)
			{
				rb.AddForce(-rb.velocity, ForceMode.VelocityChange);
				rb.AddForce(controller.velocity * throwStrength, ForceMode.VelocityChange);
			}
		}

		public override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
		{
			base.ProcessMessage(message);
			if (messageProcessed) return;
			// var state = message.FromJson<RoomObjectInfoMessage>();
			// owner = state.ownerPeerUUID == Me.GetRoomObjectInfo().MyPeer.UUID;

			if (owner)
			{
				rb.isKinematic = false;
				return;
			}

			var container = JsonUtility.FromJson<Message>(message.ToString());
			switch (container.type)
			{
				case "ObjectStateUpdated":
				{
					var state = JsonUtility.FromJson<BallState>(container.args);
					transform.position = state.position;
					transform.rotation = state.rotation;
					rb.velocity = state.velocity;
				}
					break;
			}

			rb.isKinematic = true;
		}


		public override void ObjectSpawned(RoomClient room, string jsonObjectInfo)
		{
			Debug.Log("Ball: ObjectSpawned: " + jsonObjectInfo);
			var objectInfo = JsonUtility.FromJson<RoomDictionaryInfoMessage<BallState>>(jsonObjectInfo);
			SetRoomObjectInfo(objectInfo);

			Me.SetRoom(room);
			RoomName = room.name;
		}

		protected override void SetObjectProperties(bool setVelocityZero = false)
		{
			var info = Me.GetRoomObjectInfo();

			var objectState = new BallState
			{
				position = transform.position,
				rotation = transform.rotation,
				velocity = setVelocityZero ? Vector3.zero : currentVelocity,
				grasped = follow != null
			};


			Me[info.RoomDictionaryKey] = JsonUtility.ToJson(new RoomDictionaryInfoMessage<BallState>
			{
				catalogueIndex = catalogueIndex,
				networkId = networkId,
				persistencyLevel = info.PersistencyLevel,
				state = objectState
			});
		}

		protected override void SetRoomObjectInfo<T>(RoomDictionaryInfoMessage<T> msg)
		{
			Debug.Log("Ball: SetRoomObjectInfo");
			var info = msg;
			if (info.state is BallState)
			{
				Debug.Log("Ball: Is ball state");
				var state = info.state as BallState;
				transform.position = state.position;
				transform.rotation = state.rotation;

				if (rb != null) rb.velocity = state.velocity;
				currentVelocity = state.velocity;
				catalogueIndex = msg.catalogueIndex;
				networkId = msg.networkId;

				persistencyLevel = msg.persistencyLevel.StringToEnum();
				Me.SetPersistencyLevel(persistencyLevel);
			}
		}

		[Serializable]
		public class BallState : IRoomObjectState
		{
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 velocity;
			public bool grasped;
		}

		[Serializable]
		public class BallStateMessage
		{
			public RoomObjectInfo objectInfo;
			public BallState state;
		}
	}
}