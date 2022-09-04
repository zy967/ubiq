using Ubiq.Logging;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class DataCollectionController : MonoBehaviour
	{
		public LogCollector logCollector;
		public bool Collect;

		private void Awake()
		{
			logCollector = GetComponent<LogCollector>();
		}

		// Start is called before the first frame update
		private void Start()
		{
			if (Collect)
				logCollector.StartCollection();
		}

		// Update is called once per frame
		private void Update()
		{
		}
	}
}