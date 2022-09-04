using System;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	[Serializable]
	public struct RoomObjectInfo
	{
		[SerializeField] private string name;

		[SerializeField] private NetworkId networkId;

		[SerializeField] private RoomClient room;

		[SerializeField] private string ownerPeerUUID;

		[SerializeField] private string persistencyLevel;

		[SerializeField] private string roomDictionaryKey;

		[SerializeField] private SerializableDictionary properties;

		[SerializeField] private IPeer myPeer;

		public RoomObjectInfo(string name,
			NetworkId networkId,
			RoomClient room, IPeer myPeer,
			string ownerPeerUUID, string persistencyLevel,
			string roomDictionaryKey, SerializableDictionary properties)
		{
			this.name = name;
			this.networkId = networkId;
			this.room = room;
			this.myPeer = myPeer;
			this.ownerPeerUUID = ownerPeerUUID;
			this.persistencyLevel = persistencyLevel;
			this.roomDictionaryKey = roomDictionaryKey;
			this.properties = properties;
		}

		public string Name => name;

		public NetworkId Id => networkId;

		public RoomClient Room => room;

		public IPeer MyPeer => myPeer;

		public string OwnerPeerUUID => ownerPeerUUID;

		public string PersistencyLevel => persistencyLevel;

		public string RoomDictionaryKey => roomDictionaryKey;

		public string this[string key] => properties != null ? properties[key] : null;

		public IEnumerable<KeyValuePair<string, string>> Properties => properties != null
			? properties.Enumerator
			: Enumerable.Empty<KeyValuePair<string, string>>();
	}
}