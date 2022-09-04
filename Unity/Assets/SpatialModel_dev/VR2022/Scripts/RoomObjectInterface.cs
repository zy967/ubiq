using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Rooms;

namespace SpatialModel_dev.VR2022.Scripts
{
	public abstract class RoomObjectInterface
	{
		protected IPeer MyPeer;
		protected NetworkId NetworkId;
		protected string OwnerPeerUUID;
		protected RoomObjectPersistencyLevel PersistencyLevel;

		protected SerializableDictionary properties;
		protected RoomClient Room;
		protected string RoomDictionaryKey;

		public RoomObjectInterface(string name, RoomClient room = null)
		{
			Name = name;
			Room = room;
			if (Room != null)
				MyPeer = room.Me;
			OwnerPeerUUID = "";
			properties = new SerializableDictionary();
		}

		public RoomObjectInterface()
		{
			properties = new SerializableDictionary();
		}

		public string Name { get; protected set; }
		public string UUID { get; protected set; }

		public string this[string key]
		{
			get => properties[key];
			set => properties[key] = value;
		}

		public IEnumerable<KeyValuePair<string, string>> Properties => properties.Enumerator;

		public RoomObjectInfo GetRoomObjectInfo()
		{
			return new RoomObjectInfo(Name, NetworkId,
				Room, MyPeer, OwnerPeerUUID,
				PersistencyLevel.EnumToString(), RoomDictionaryKey, properties);
		}
	}
}