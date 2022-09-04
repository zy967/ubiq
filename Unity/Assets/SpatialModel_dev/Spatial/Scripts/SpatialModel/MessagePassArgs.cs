﻿using System;
using Ubiq.Messaging;

namespace SpatialModel_dev.Spatial.Scripts.SpatialModel
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