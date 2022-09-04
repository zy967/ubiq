using Ubiq.Logging;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
    public class DataCollectionController : MonoBehaviour
    {
        public LogCollector logCollector;
        public bool Collect = false;

        void Awake()
        {
            logCollector = GetComponent<LogCollector>();
        }

        // Start is called before the first frame update
        void Start()
        {
            if(Collect)
                logCollector.StartCollection();
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
