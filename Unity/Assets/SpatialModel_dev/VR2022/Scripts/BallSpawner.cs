using Ubiq.XR;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class BallSpawner : MonoBehaviour, IUseable
	{
		public GameObject BallPrefab;
		private Rigidbody body;

		private Hand follow;

		private void Awake()
		{
			body = GetComponent<Rigidbody>();
		}

		public void UnUse(Hand controller)
		{
		}

		public void Use(Hand controller)
		{
			Debug.Log("BallSpawner: Use");

			// TODO: Not working code
			Debug.LogError("Ball Spawner not working");

			// var ball = NetworkSpawner.SpawnPersistent(this, BallPrefab).GetComponents<MonoBehaviour>().Where(mb => mb is IBall).FirstOrDefault() as IBall;
			// if (ball != null)
			// {
			//     ball.Attach(controller);
			// }
		}
	}
}