using System;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	[Serializable]
	internal struct SphereFocusArgs
	{
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