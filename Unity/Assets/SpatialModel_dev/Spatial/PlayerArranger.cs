using System.Collections.Generic;
using UnityEngine;

public class PlayerArranger : MonoBehaviour
{
	private List<GameObject> players;
	private Color areaColor;

	private void Awake()
	{
		players = new List<GameObject>();
		areaColor = Random.ColorHSV(0, 1, 1, 1, 1, 1, 0.7f, 0.7f);
	}

	private void Update()
	{
		if (players.Count < 0) return;

		for (int i = 0; i < players.Count - 1; i++)
		{
			for (int j = i+1; j < players.Count; j++)
			{
				Debug.DrawLine(players[i].transform.position, players[j].transform.position,
					areaColor);
			}
		}
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