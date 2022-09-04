using System;

namespace SpatialModel_dev.VR2022.Scripts
{
	[Serializable]
	public enum RoomObjectPersistencyLevel
	{
		// Persistent level objects remain in the room even when there are no peers/observers in the room
		Persistent,

		// Owner level objects remain in the room as long as the owner of the object is in the room
		Owner,

		// Room level objects remain in the room as long as there are peers or observers in the room
		Room
	}
}