using System;
using UnityEngine;

namespace Ubiq.SpatialModel
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