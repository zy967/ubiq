using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms;
using Ubiq.Samples;
using Ubiq.Spawning;
using Ubiq.XR;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
    public interface IBall
    {
        void Attach(Hand hand);
    }
    public class BallSpawner : MonoBehaviour, IUseable
    {
        public GameObject BallPrefab;

        private Hand follow;
        private Rigidbody body;

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