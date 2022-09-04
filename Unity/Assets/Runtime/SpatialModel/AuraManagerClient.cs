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

		public void AddFocus(string auraCellUuid, string medium)
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

			SendToServer("AddFocus", new FocusArgs
			{
				auraCellUuid = auraCellUuid,
				medium = medium
			});
		}

		public void RemoveFocus(string auraCellUuid, string medium)
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
						SendToServer("RemoveFocus", new FocusArgs
						{
							auraCellUuid = auraCellUuid,
							medium = medium
						});
				}
			}
		}

		public List<string> GetCellsBySphere(Vector3 center, float radius)
		{
			return cellDetector.GetCellsBySphere(center, radius)
				.ConvertAll(c => c.GetComponentInParent<Cell>().CellUuid);
		}

		public void Send(NetworkId senderId, string medium, List<string> auraCellUuids, string message)
		{
			SendToServer("MessagePass", new MessagePassArgs
			{
				medium = medium,
				auraCellUuids = auraCellUuids.ToArray(),
				senderId = senderId,
				message = message
			});
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
				case "MessagePass":
				{
					var args = JsonUtility.FromJson<MessagePassArgs>(container.args);
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