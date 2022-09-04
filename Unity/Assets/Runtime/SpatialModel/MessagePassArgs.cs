using System;
using Ubiq.Messaging;

namespace Ubiq.SpatialModel
{
	[Serializable]
	internal class MessagePassArgs
	{
		public string medium;
		public string[] auraCellUuids;
		public NetworkId senderId;
		public string message;
	}
}