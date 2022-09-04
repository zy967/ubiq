using UnityEngine;
using UnityEngine.AI;

namespace SpatialModel_dev.Spatial
{
	/// <summary>
	///     Based on previous work in Sample.Bot.BotAgent
	///     The main controller for a Bot; this Component controls the behaviour of the bot, moving it around the scene
	///     and emulating interaction.
	/// </summary>
	[RequireComponent(typeof(NavMeshAgent))]
	public class BotAgentPlayer : MonoBehaviour
	{
		public float Radius = 10f;
		private Vector3 destination;

		private bool isWandering = true;

		private NavMeshAgent navMeshAgent;
		private float wanderingTime;

		private void Awake()
		{
			navMeshAgent = GetComponent<NavMeshAgent>();
		}

		// Start is called before the first frame update
		private void Start()
		{
			Wander();
		}

		// Update is called once per frame
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) isWandering = !isWandering;

			if (isWandering)
			{
				// Have the agent wander around the scene
				if (Vector3.Distance(destination, transform.position) < 1f || wanderingTime > 5f) Wander();

				wanderingTime += Time.deltaTime;
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(destination, 0.5f);
		}

		public void Wander()
		{
			NavMeshHit hit;

			var i = 0;
			do
			{
				i++;
				destination = transform.position + Random.insideUnitSphere * Radius;
			} while (!NavMesh.SamplePosition(destination, out hit, float.MaxValue, NavMesh.AllAreas) && i < 100);

			destination = hit.position;
			navMeshAgent.destination = destination;
			wanderingTime = 0;
		}
	}
}