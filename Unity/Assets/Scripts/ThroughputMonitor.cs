using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Voip;
using UnityEngine;

public class ThroughputMonitor : MonoBehaviour
{
    private NetworkScene scene;
    private Ubiq.Networking.JmBucknall.Structures.LockFreeQueue<MessageEvent> queue;
    private StreamWriter stream;
    private Task task;
    private bool run;
    private float timeNow;

    private void Awake()
    {
        queue = new Ubiq.Networking.JmBucknall.Structures.LockFreeQueue<MessageEvent>();
    }

    // Start is called before the first frame update
    void Start()
    {
        scene = NetworkScene.FindNetworkScene(this);
        scene.OnMessageSent.AddListener(Sent);
        scene.OnMessageReceived.AddListener(Received);
        task = new Task(Worker);
        run = true;
        stream = new StreamWriter(OpenNewFile());
        task.Start();

        G722AudioDecoder.OnDecode = OnDecode;
        G722AudioEncoder.OnEncode = OnEncode;
    }

    void Update()
    {
        timeNow = Time.realtimeSinceStartup;
    }

    private void OnDecode(byte[] encoded)
    {
        MessageEvent e;
        e.time = timeNow;
        e.length = encoded.Length;
        e.direction = false;
        e.id = "0";
        queue.Enqueue(e);
    }

    private void OnEncode(byte[] encoded)
    {
        MessageEvent e;
        e.time = timeNow;
        e.length = encoded.Length;
        e.direction = true;
        e.id = "0";
        queue.Enqueue(e);
    }

    public struct MessageEvent
    {
        public float time;
        public int length;
        public bool direction;
        public string id;
    }

    void Sent(ReferenceCountedMessage message)
    {
        MessageEvent e;
        e.time = Time.realtimeSinceStartup;
        e.length = message.length;
        e.direction = true;
        e.id = new ReferenceCountedSceneGraphMessage(message).objectid.ToString();
        queue.Enqueue(e);
    }
    
    void Received(ReferenceCountedMessage message)
    {
        MessageEvent e;
        e.time = Time.realtimeSinceStartup;
        e.length = message.length;
        e.direction = false;
        e.id = new ReferenceCountedSceneGraphMessage(message).objectid.ToString();
        queue.Enqueue(e);
    }

    void Worker()
    {        
        while(run)
        {
            MessageEvent e;
            if(queue.Dequeue(out e))
            {
                try
                {
                    stream.WriteLine($"{e.time}, {e.length}, {e.direction}, {e.id}");
                }
                catch(Exception)
                {
                    return;
                }
            }
        }
    }

    Stream OpenNewFile()
    {
        int i = 0;
        string filename = Filepath(i);
        while (File.Exists(filename))
        {
            filename = Filepath(i++);
        }

        return File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    private string Filepath(int i)
    {
        return Path.Combine(Application.persistentDataPath, $"UbiqThroughputMonitor_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{i}.csv");
    }

    private void OnDestroy()
    {
        run = false;
        stream.Close();
        task.Wait();
    }

}
