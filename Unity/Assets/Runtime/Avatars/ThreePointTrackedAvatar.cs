using System;
using Ubiq.Messaging;
using Ubiq.SpatialModel;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.Avatars
{
	[RequireComponent(typeof(Avatar))]
	public class ThreePointTrackedAvatar : NetworkBehaviour
	{
		[Serializable]
		public class TransformUpdateEvent : UnityEvent<Vector3, Quaternion>
		{
		}

		public TransformUpdateEvent OnHeadUpdate;
		public TransformUpdateEvent OnLeftHandUpdate;
		public TransformUpdateEvent OnRightHandUpdate;

		private Transform networkSceneRoot;
		private State[] state = new State[1];
		private Avatar avatar;
		private float lastTransmitTime;
		private AuraManagerClient auraManagerClient;

		[Serializable]
		private struct State
		{
			public PositionRotation leftHand;
			public PositionRotation rightHand;
			public PositionRotation head;
		}

		private void Awake()
		{
			avatar = GetComponent<Avatar>();
		}

		protected override void Started()
		{
			lastTransmitTime = Time.time;
			networkSceneRoot = networkScene.transform;
			auraManagerClient = networkSceneRoot.GetComponentInChildren<AuraManagerClient>();
		}

		private void Update()
		{
			if (avatar.IsLocal)
			{
				// Update state from hints
				state[0].head = GetHintNode(AvatarHints.NodePosRot.Head);
				state[0].leftHand = GetHintNode(AvatarHints.NodePosRot.LeftHand);
				state[0].rightHand = GetHintNode(AvatarHints.NodePosRot.RightHand);

				// Send it through network
				if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
				{
					lastTransmitTime = Time.time;
					Send();
				}

				// Update local listeners
				OnRecv();
			}
		}

		// Local to world space
		private PositionRotation TransformPosRot(PositionRotation local, Transform root)
		{
			var world = new PositionRotation();
			world.position = root.TransformPoint(local.position);
			world.rotation = root.rotation * local.rotation;
			return world;
		}

		// World to local space
		private PositionRotation InverseTransformPosRot(PositionRotation world, Transform root)
		{
			var local = new PositionRotation();
			local.position = root.InverseTransformPoint(world.position);
			local.rotation = Quaternion.Inverse(root.rotation) * world.rotation;
			return local;
		}

		private PositionRotation GetHintNode(AvatarHints.NodePosRot node)
		{
			if (AvatarHints.TryGet(node, avatar, out PositionRotation nodePosRot))
			{
				return new PositionRotation
				{
					position = nodePosRot.position,
					rotation = nodePosRot.rotation
				};
			}

			return new PositionRotation();
		}

		private void Send()
		{
			// Co-ords from hints are already in local to our network scene
			// so we can send them without any changes
			// var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<State>(state));
			//
			// var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
			// transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));
			//
			// var a = message.ToString();
			// var aa = transformBytes.ToString();
			// var aaa = JsonUtility.ToJson(message);
			//
			// Debug.Log(a);
			// Debug.Log(aa);
			// Debug.Log(aaa);

			Vector3 currentCell =
				auraManagerClient.GetCellsBySphere(state[0].head.position, 2.0f)?[0].Coordinates.AsVector3() ??
				Vector3.one;
			// auraManagerClient.Send(networkId, "transform",
			// 	auraManagerClient.TryGetCellsBySphere(state[0].head.position, 3.0f), message.ToString());
			auraManagerClient.SendBySphere(networkId,
				"transform",
				currentCell,
				2,
				JsonUtility.ToJson(state[0]));
		}

		protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
		{
			// MemoryMarshal.Cast<byte, State>(new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
			// 	.CopyTo(new Span<State>(state));
			state[0] = JsonUtility.FromJson<State>(message.ToString());
			// state[0] = message.FromJson<State>();
			OnRecv();
		}

		// State has been set either remotely or locally so update listeners
		private void OnRecv()
		{
			// Transform with our network scene root to get world position/rotation
			var head = TransformPosRot(state[0].head, networkSceneRoot);
			var leftHand = TransformPosRot(state[0].leftHand, networkSceneRoot);
			var rightHand = TransformPosRot(state[0].rightHand, networkSceneRoot);

			OnHeadUpdate.Invoke(head.position, head.rotation);
			OnLeftHandUpdate.Invoke(leftHand.position, leftHand.rotation);
			OnRightHandUpdate.Invoke(rightHand.position, rightHand.rotation);
		}
	}
}