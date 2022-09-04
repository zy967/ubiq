using System;
using UnityEngine;

namespace SpatialModel_dev.Spatial.Scripts
{
	[Serializable]
	internal struct Message
	{
		public string type;
		public string args;

		public Message(string type, object args)
		{
			this.type = type;
			this.args = JsonUtility.ToJson(args);
		}
	}
}