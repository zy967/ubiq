using System;
using System.Collections.Generic;
using Ubiq.Guids;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace SpatialModel_dev.Spatial.Scripts.SpatialModel
{
	public class Medium : MonoBehaviour
	{
		public string mediumName;

		public GameObject observerPrefab;
		private Focus focus;
		private string mediumUUID;
		private List<NetworkScene> networkScenes;

		private Nimbus nimbus;
		private List<RoomClient> subRoomClients;

		private void Awake()
		{
			networkScenes = new List<NetworkScene>();
			subRoomClients = new List<RoomClient>();
			for (var i = 0; i < 3; ++i)
			{
				var observer = Instantiate(observerPrefab, gameObject.transform);
				networkScenes.Add(observer.GetComponent<NetworkScene>());
				subRoomClients.Add(observer.GetComponent<RoomClient>());
			}

			nimbus = GetComponentInChildren<Nimbus>();
			focus = GetComponentInChildren<Focus>();
		}


		private void Start()
		{
			// TODO: Ensure the uniqueness of the medium
			mediumName = gameObject.name;

			// TODO: maybe we should include the aura's name (should also be unique)
			mediumUUID = Guids.Generate(new Guid("ca761232-ed42-11ce-bacd-00aa0057b223"), mediumName).ToString();

			foreach (var subRoomClient in subRoomClients)
				subRoomClient.Join($"Observer Room {IdGenerator.GenerateUnique().ToString()}", true);
		}

		private void Update()
		{
			// UpdateByNimbus();
			// UpdateByFocus();
		}

		private void UpdateByNimbus()
		{
			// Update sending message status
			foreach (var networkScene in networkScenes) networkScene.isSendingMessage = nimbus.IsSpreading();
		}

		private void UpdateByFocus()
		{
			foreach (var subRoomClient in subRoomClients)
				// Check if current focus is activated in the medium
				if (focus.IsFocusing())
				{
					// Receive other's message by join corresponding room if not in it
					if (subRoomClient.Room.UUID != mediumUUID) subRoomClient.Join(mediumUUID);
				}
				else
				{
					// De-focus by leave the room if in it
					if (subRoomClient.JoinedRoom)
					{
						// TODO: Stop connect to the room (to an empty room or stop connection)
					}
				}
		}
	}
}