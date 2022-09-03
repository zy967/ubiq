using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialModel
{
	public struct CellTriggerInfo
	{
		public CellTrigger Trigger;
		public GameObject TriggeredObject;
	}


	[RequireComponent(typeof(Collider))]
	public class CellTrigger : MonoBehaviour
	{
		public class CellTriggerEvent : UnityEvent<CellTriggerInfo>
		{
		};

		public CellTriggerEvent OnTriggerEntered;
		public CellTriggerEvent OnTriggerExited;
		public Vector3[] borderPoints;

		public void Awake()
		{
			OnTriggerEntered = new CellTriggerEvent();
			OnTriggerExited = new CellTriggerEvent();

			GetComponent<Collider>().isTrigger = true;
			GetComponent<Collider>().enabled = true;

			MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
			if (meshRenderer != null)
			{
				Destroy(meshRenderer);
			}
		}

		public Vector3[] GetBorderPoints()
		{
			var boxCollider = GetComponent<BoxCollider>();
			var trans = boxCollider.transform;
			var min = boxCollider.center - boxCollider.size * 0.5f;
			var max = boxCollider.center + boxCollider.size * 0.5f;
			var vertices = new Vector3[2];

			vertices[0] = trans.TransformPoint(new Vector3((max.x + min.x) / 2, min.y, max.z));
			vertices[1] = trans.TransformPoint(new Vector3((max.x + min.x) / 2, min.y, min.z));

			return vertices;
		}

		private void OnTriggerEnter(Collider other)
		{
			OnTriggerEntered.Invoke(new CellTriggerInfo
			{
				Trigger = this,
				TriggeredObject = other.gameObject
			});
		}

		private void OnTriggerExit(Collider other)
		{
			// Debug.Log(this.name + " OnTriggerExit: " + other.gameObject.name);
			OnTriggerExited.Invoke(new CellTriggerInfo
			{
				Trigger = this,
				TriggeredObject = other.gameObject
			});
		}
	}
}