using System;
using System.IO;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace SpatialModel_dev.VR2022.Scripts
{
	public class FPSMonitor : MonoBehaviour
	{
		public RoomClient client;

		public float updateInterval = 0.5F;

		// private EventLogger logger;
		private int fps;

		private int FPSLowLimit = 30;
		private int frames;

		private string id;
		private double lastInterval;
		private float remaining;
		private NetworkScene scene;


		private StreamWriter stream;
		private float TimeBeforeQuit = 10;
		private bool timerStarted = false;
		private float timerStartTime;

		private void Start()
		{
			if (client == null) client = transform.parent.GetComponentInChildren<RoomClient>();

			if (client != null) id = client.Me.uuid;
			Application.targetFrameRate = 60;
			// QualitySettings.vSyncCount = 1;
			// logger = new UserEventLogger(this);
			lastInterval = Time.realtimeSinceStartup;
			frames = 0;

			stream = new StreamWriter(OpenNewFile());
		}

		// Update is called once per frame
		private void Update()
		{
			++frames;
			var timeNow = Time.realtimeSinceStartup;
			if (timeNow > lastInterval + updateInterval)
			{
				fps = (int) (frames / (timeNow - lastInterval));
				// logger.Log("FPS", Time.realtimeSinceStartup, fps > 0 ? fps : 0, id);
				try
				{
					stream.WriteLine($"{Time.realtimeSinceStartup}, {fps}, {id}");
				}
				catch (Exception)
				{
					return;
				}

				frames = 0;
				lastInterval = timeNow;
			}
		}

		private void OnDestroy()
		{
			stream.Close();
		}

		private Stream OpenNewFile()
		{
			var filename = Filepath();

			return File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
		}

		private string Filepath()
		{
			return Path.Combine(Application.persistentDataPath,
				$"FPS_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{id}.csv");
		}
	}
}