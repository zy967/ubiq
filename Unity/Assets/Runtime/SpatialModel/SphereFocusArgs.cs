using System;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	[Serializable]
	internal class SphereFocusArgs
	{
		public NetworkId senderId;
		public string medium;
		public Vector3 origin;
		public float radius;
	}

	[Serializable]
	internal class SingleFocusArgs
	{
		public string medium;
		public string auraCellUuid;
	}
}