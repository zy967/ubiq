namespace SpatialModel_dev.VR2022.Scripts
{
	public static class RoomObjectPersistencyLevelExtensions
	{
		public static string EnumToString(this RoomObjectPersistencyLevel me)
		{
			switch (me)
			{
				case RoomObjectPersistencyLevel.Persistent:
					return "persistent";
				case RoomObjectPersistencyLevel.Owner:
					return "owner";
				case RoomObjectPersistencyLevel.Room:
					return "room";
				default:
					return "persistent";
			}
		}

		public static RoomObjectPersistencyLevel StringToEnum(this string me)
		{
			switch (me)
			{
				case "persistent":
					return RoomObjectPersistencyLevel.Persistent;
				case "owner":
					return RoomObjectPersistencyLevel.Owner;
				case "room":
					return RoomObjectPersistencyLevel.Room;
				default:
					return RoomObjectPersistencyLevel.Persistent;
			}
		}
	}
}