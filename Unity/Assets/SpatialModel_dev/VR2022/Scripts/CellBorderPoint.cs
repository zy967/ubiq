using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SpatialModel_dev.VR2022.Scripts
{
	public struct CellBorderEventInfo
	{
		public CellBorderPoint BorderPoint;
		public GameObject GameObject;
		public string ObjectType;
	}

	[RequireComponent(typeof(Collider))]
	public class CellBorderPoint : MonoBehaviour
	{
		[SerializeField] public Cell fromCell;
		[SerializeField] public Cell toCell;

		[HideInInspector] public float distanceToCell; //Only used in HexCell when creating Borders automatically

		public float exitDelay;

		public class CellBorderEvent : UnityEvent<CellBorderEventInfo>
		{
		};

		public CellBorderEvent OnBorderTriggerEntered;
		public CellBorderEvent OnBorderTriggerExited;

		Dictionary<string, GameObject> triggeredObjects = new Dictionary<string, GameObject>();
		Dictionary<string, IEnumerator> removeCoroutines = new Dictionary<string, IEnumerator>();

		void Awake()
		{
			OnBorderTriggerEntered = new CellBorderEvent();
			OnBorderTriggerExited = new CellBorderEvent();

			enabled = false;
		}

		private void Start()
		{
			name = "Border To Cell " + toCell.Name;
			GetComponent<Collider>().isTrigger = true;
		}

		private void OnDisable()
		{
			triggeredObjects.Clear();
			foreach (var item in removeCoroutines.Values)
			{
				StopCoroutine(item);
			}

			removeCoroutines.Clear();
		}

		private void OnTriggerEnter(Collider other)
		{
			bool invokeEvent = false;

			CellBorderEventInfo cellBorderEventInfo = new CellBorderEventInfo
			{
				BorderPoint = this,
				GameObject = other.gameObject
			};
			if (other.CompareTag("Player"))
			{
				// Debug.Log("Player Entered Border: " + this.name);
				if (removeCoroutines.ContainsKey("Player"))
				{
					// Debug.Log("Stop Player Remove Coroutine");
					StopCoroutine(removeCoroutines["Player"]);
				}

				if (!triggeredObjects.ContainsKey("Player"))
				{
					// Debug.Log("Invoke Event For Player");
					invokeEvent = true;
					triggeredObjects.Add("Player", other.gameObject);
					cellBorderEventInfo.ObjectType = "Player";
				}
			}
			else
			{
				RoomObject roomObject =
					other.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject)
						.FirstOrDefault() as RoomObject;
				if (roomObject != null)
				{
					if (removeCoroutines.ContainsKey(roomObject.networkId.ToString()))
					{
						// Debug.Log("Stop Client Agent Remove Coroutine");
						StopCoroutine(removeCoroutines[roomObject.networkId.ToString()]);
					}

					// Debug.Log("HexCell: roomObject object " + triggerInfo.triggeredObject.name + " entered trigger: " + triggerInfo.trigger.name);
					if (!triggeredObjects.ContainsKey(roomObject.networkId.ToString()))
					{
						invokeEvent = true;
						triggeredObjects.Add(roomObject.networkId.ToString(), other.gameObject);
						cellBorderEventInfo.ObjectType = "RoomObject";
					}
				}
			}

			if (invokeEvent)
			{
				OnBorderTriggerEntered.Invoke(cellBorderEventInfo);
			}
		}

		void OnTriggerExit(Collider other)
		{
			if (other.CompareTag("Player"))
			{
				// Debug.Log("Player Exited Border: " + this.name);
				if (triggeredObjects.ContainsKey("Player"))
				{
					// Debug.Log("Add Coroutine For Removing Player");
					removeCoroutines["Player"] = WaitAndRemove(exitDelay, "Player", "Player");
					StartCoroutine(removeCoroutines["Player"]);
				}
			}
			else
			{
				RoomObject roomObject =
					other.gameObject.GetComponentsInChildren<MonoBehaviour>().Where(mb => mb is RoomObject)
						.FirstOrDefault() as RoomObject;
				if (roomObject != null)
				{
					// Debug.Log("Room Object Exited Border: " + this.name);
					if (triggeredObjects.ContainsKey(roomObject.networkId.ToString()))
					{
						// Debug.Log("Add Coroutine For Removing Client Agent");
						removeCoroutines[roomObject.networkId.ToString()] =
							WaitAndRemove(0, roomObject.networkId.ToString(), "RoomObject");
						StartCoroutine(removeCoroutines[roomObject.networkId.ToString()]);
					}
				}
			}
		}

		// TODO: error here "key is null"
		// maybe caused by the delay with multiple triggering
		public IEnumerator WaitAndRemove(float waitTime, string key, string type)
		{
			yield return new WaitForSeconds(waitTime);

			// Debug.Log("Remove Player Invoke");

			OnBorderTriggerExited.Invoke(new CellBorderEventInfo
			{
				BorderPoint = this,
				GameObject = triggeredObjects[key],
				ObjectType = type
			});

			triggeredObjects.Remove(key);
		}
	}
}