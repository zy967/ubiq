using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.SpatialModel
{
	public class AreaIndicator : MonoBehaviour
	{
		private Color playerLinkColor;
		private List<GameObject> players;

		private void Awake()
		{
			players = new List<GameObject>();
			playerLinkColor = Random.ColorHSV(0, 1, 1, 1, 1, 1, 0.7f, 0.7f);
		}

		private void Update()
		{
			if (players.Count < 0) return;

			for (var i = 0; i < players.Count - 1; i++)
			for (var j = i + 1; j < players.Count; j++)
				Debug.DrawLine(players[i].transform.position, players[j].transform.position,
					playerLinkColor);
		}

		private void OnTriggerEnter(Collider other)
		{
			Debug.Log("In");
			if (!other.gameObject.CompareTag("Player")) return;

			players.Add(other.gameObject);
		}

		private void OnTriggerExit(Collider other)
		{
			Debug.Log("Out");
			if (!other.gameObject.CompareTag("Player")) return;

			players.Remove(other.gameObject);
		}
	}
}