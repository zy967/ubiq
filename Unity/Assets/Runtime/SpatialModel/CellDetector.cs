using System;
using System.Collections.Generic;
using Ubiq.Grid;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	internal class CellDetector
	{
		private readonly LayerMask layerMask;
		private Collider[] cellBuffer;
		private int collectedColliderCount;

		public CellDetector()
		{
			cellBuffer = new Collider[50];
			layerMask = LayerMask.GetMask("Aura");
		}

		private bool TryGetCellsBySphere(Vector3 origin, float radius)
		{
			collectedColliderCount = -1;
			while (collectedColliderCount < 0)
			{
				collectedColliderCount = Physics.OverlapSphereNonAlloc(origin, radius, cellBuffer, layerMask);
				// Debug.DrawLine(origin, origin + Vector3.forward * radius, Color.magenta, 1.0f);
				if (collectedColliderCount == cellBuffer.Length)
				{
					ExtendBuffer();
					// perform the overlapping again
					collectedColliderCount = -1;
				}
			}

			return collectedColliderCount > 0;
		}

		public List<T> TryGetCellsBySphere<T>(Vector3 origin, float radius)
		{
			if (TryGetCellsBySphere(origin, radius))
			{
				List<T> cells = new List<T>();
				for (int i = 0; i < collectedColliderCount; i++)
				{
					cells.Add(cellBuffer[i].GetComponentInParent<T>());
				}

				return cells;
			}
			else
			{
				return null;
			}
		}

		public List<string> GetCellsCoordBySphere(Vector3 origin, float radius)
		{
			TryGetCellsBySphere(origin, radius);

			List<string> cells = new List<string>();
			for (int i = 0; i < collectedColliderCount; i++)
			{
				cells.Add(JsonUtility.ToJson(cellBuffer[i].GetComponentInParent<Cell>().Coordinates));
			}

			return cells;
		}

		private void ExtendBuffer(int amount = -1)
		{
			// Double the buffer if given invalid amount
			var newBufferSize = amount > 0 ? cellBuffer.Length + amount : cellBuffer.Length * 2;
			Array.Resize(ref cellBuffer, newBufferSize);
		}
	}
}