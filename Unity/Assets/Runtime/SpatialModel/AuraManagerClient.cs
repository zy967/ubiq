using System;
using System.Collections.Generic;
using Ubiq.Grid;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	#region Network Arguments

	#endregion

	public class AuraManagerClient : MonoBehaviour
	{
		private readonly NetworkId auraServerObjectId = new NetworkId(3993);

		private CellDetector cellDetector;
		private NetworkId clientId;

		private Dictionary<string, Dictionary<string, int>> focusedCells;
		private NetworkScene scene;

		private void Awake()
		{
			// Aura - Medium - Focused count
			focusedCells = new Dictionary<string, Dictionary<string, int>>();
			cellDetector = new CellDetector();
		}

		private void Start()
		{
			scene = NetworkScene.FindNetworkScene(this);
			clientId = NetworkId.Create(scene.Id, "AuraClient");
			scene.AddProcessor(clientId, ProcessMessage);
		}

		public void AddSingleFocus(string auraCellUuid, string medium)
		{
			Dictionary<string, int> focusedMediumsOfCell;
			if (focusedCells.TryGetValue(auraCellUuid, out focusedMediumsOfCell))
			{
				int focusedCount;
				if (focusedMediumsOfCell.TryGetValue(medium, out focusedCount))
				{
					if (focusedCount > 0)
					{
						focusedMediumsOfCell[medium] = focusedCount + 1;
						return; //not send to server the already focused cell
					}
				}
				else
				{
					focusedMediumsOfCell.Add(medium, 1);
				}
			}
			else
			{
				focusedMediumsOfCell = new Dictionary<string, int>();
				focusedMediumsOfCell.Add(medium, 1);
				focusedCells.Add(auraCellUuid, focusedMediumsOfCell);
			}

			SendToServer("AddFocus", new SingleFocusArgs
			{
				auraCellUuid = auraCellUuid,
				medium = medium
			});
		}

		public void RemoveSingleFocus(string auraCellUuid, string medium)
		{
			Dictionary<string, int> focusedMediumsOfCell;
			if (focusedCells.TryGetValue(auraCellUuid, out focusedMediumsOfCell))
			{
				int focusedCount;
				if (focusedMediumsOfCell.TryGetValue(medium, out focusedCount))
				{
					focusedCount = Math.Max(0, focusedCount - 1);
					focusedMediumsOfCell[medium] = focusedCount;

					if (focusedCount == 0)
						SendToServer("RemoveFocus", new SingleFocusArgs
						{
							auraCellUuid = auraCellUuid,
							medium = medium
						});
				}
			}
		}


		public void AddSphereFocus(NetworkId senderId, string medium, Vector3 origin, float radius)
		{
			SendToServer("AddSphereFocus", new SphereFocusArgs()
			{
				senderId = senderId,
				medium = medium,
				origin = origin,
				radius = radius
			});
		}

		public List<Cell> GetCellsBySphere(Vector3 center, float radius)
		{
			return cellDetector.TryGetCellsBySphere<Cell>(center, radius);
		}

		public void SendBySphere(NetworkId senderId, string medium, Vector3 center, float radius,
			string message)
		{
			var args = new SpreadMessageBySphereArgs
			{
				senderId = senderId,
				origin = center,
				radius = radius,
				medium = medium,
				message = message
			};

			var okay = JsonUtility.ToJson(args);
			Debug.Log(okay);
			SendToServer("SpreadMessage", args);
		}

		private void SendToServer(string type, object argument)
		{
			scene.SendJson(auraServerObjectId, new Message(type, argument));
		}

		protected void ProcessMessage(ReferenceCountedSceneGraphMessage auraMessage)
		{
			var container = JsonUtility.FromJson<Message>(auraMessage.ToString());
			switch (container.type)
			{
				case "SpreadMessage":
				{
					var args = JsonUtility.FromJson<SpreadMessageBySphereArgs>(container.args);
					var processors = scene.GetProcessor(args.senderId);
					foreach (var processor in processors)
					{
						var message = ReferenceCountedSceneGraphMessage.Rent(args.message);
						message.objectid = args.senderId;
						processor(message);
					}

					break;
				}
			}
		}
	}
}