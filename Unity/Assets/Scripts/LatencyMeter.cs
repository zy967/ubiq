using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

public class LatencyMeter : MonoBehaviour, INetworkComponent
{
    private NetworkContext context;
    private RoomClient client;
    private Dictionary<string, Stopwatch> transmissionTimes;
    private EventLogger latencies;
    private float lastMeasureTime;

    public float MeasurePeriod;

    private void Awake()
    {
        transmissionTimes = new Dictionary<string, Stopwatch>();
        lastMeasureTime = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        client = context.scene.GetComponentInChildren<RoomClient>();
        latencies = new UserEventLogger(this);
        client.OnRoom.AddListener(OnRoom);
    }

    void OnRoom(RoomInfo room)
    {
        float measurePeriod;
        if(float.TryParse(room["exp.latencymeter.period"], out measurePeriod))
        {
            MeasurePeriod = measurePeriod;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(MeasurePeriod > 0)
        {
            var measureInterval = Time.time - lastMeasureTime;
            if(measureInterval > MeasurePeriod)
            {
                lastMeasureTime = Time.time;
                MeasurePeerLatencies();
            }
        }
    }

    private struct Message
    {
        public NetworkId source;
        public string token;
        public bool reply;
    }

    public void MeasurePeerLatencies()
    {
        foreach (var item in client.Peers)
        {
            if(item.UUID == client.Me.UUID)
            {
                continue;
            }

            Message message;
            message.source = context.scene.Id;
            message.token = Guid.NewGuid().ToString();
            message.reply = false;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            transmissionTimes[message.token] = stopwatch;
            context.SendJson(item.NetworkObjectId, message);
            latencies.Log("RTTA", context.scene.Id, item.NetworkObjectId, message.token);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        var source = msg.source;

        if (msg.reply)
        {
            latencies.Log("RTTC", context.scene.Id, msg.token);
            Stopwatch stopwatch;
            if(transmissionTimes.TryGetValue(msg.token, out stopwatch))
            {
                stopwatch.Stop();
                latencies.Log("RTTM", context.scene.Id, source, stopwatch.ElapsedMilliseconds);
            }
            transmissionTimes.Remove(msg.token);
        }
        else
        {
            latencies.Log("RTTB", context.scene.Id, msg.token);
            msg.source = context.scene.Id;
            msg.reply = true;
            context.SendJson(source, msg);
        }
    }

    public void SetPeriod(float period)
    {
        MeasurePeriod = period;
        client.Room["exp.latencymeter.period"] = period.ToString();
    }
}
