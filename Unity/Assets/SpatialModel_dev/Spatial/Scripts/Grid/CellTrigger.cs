using UnityEngine;
using UnityEngine.Events;

namespace SpatialModel_dev.Spatial.Scripts.Grid
{
	[RequireComponent(typeof(Collider))]
	public class CellTrigger : MonoBehaviour
	{
		public CellTriggerEvent OnTriggerEntered;

		public CellTriggerEvent OnTriggerExited;
		// public Vector3[] borderPoints;

		public void Awake()
		{
			OnTriggerEntered = new CellTriggerEvent();
			OnTriggerExited = new CellTriggerEvent();

			GetComponent<Collider>().isTrigger = true;
			GetComponent<Collider>().enabled = true;

			// GetComponent<Collider>().enabled = false;

			var meshRenderer = GetComponent<MeshRenderer>();
			if (meshRenderer != null) Destroy(meshRenderer);
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

		public class CellTriggerEvent : UnityEvent<CellTriggerInfo>
		{
		}
	}
}