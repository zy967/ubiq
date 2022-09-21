using System;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	[Serializable]
	internal struct SpreadMessageBySphereArgs
	{
		public NetworkId senderId;
		public Vector3 origin;
		public float radius;
		public string medium;
		public string message;
	}
}