using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpatialModel_dev.Spatial.Scripts
{
	internal class CellDetector
	{
		private Collider[] cellBuffer;
		private int collectedColliderCount;
		private readonly LayerMask layerMask;

		public CellDetector()
		{
			cellBuffer = new Collider[50];
			layerMask = LayerMask.GetMask("Aura");
		}

		public List<Collider> GetCellsBySphere(Vector3 origin, float radius)
		{
			collectedColliderCount = 0;
			while (collectedColliderCount == 0)
			{
				collectedColliderCount = Physics.OverlapSphereNonAlloc(origin, radius, cellBuffer, layerMask);
				if (collectedColliderCount == cellBuffer.Length)
				{
					ExtendBuffer();
					// perform the overlapping again
					collectedColliderCount = 0;
				}
			}

			return new List<Collider>(cellBuffer).GetRange(0, collectedColliderCount);
		}

		private void ExtendBuffer(int amount = -1)
		{
			// Double the buffer if given invalid amount
			var newBufferSize = amount > 0 ? cellBuffer.Length + amount : cellBuffer.Length * 2;
			Array.Resize(ref cellBuffer, newBufferSize);
		}
	}
}