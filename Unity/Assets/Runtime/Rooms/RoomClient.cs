﻿using System;
using System.Collections.Generic;
using Ubiq.Networking;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Dictionaries;
using Ubiq.XR.Notifications;
using System.Linq;
using Ubiq.Rooms.Messages;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Facilitates joining and working with Rooms via a RoomServer somewhere on the Network.
    /// The Rooms system provides the concept of Peers as Remote Players, with their own sets of
    /// Components and properties.
    /// With the use of a server, RoomClient can join a network with other Peers.
    /// </summary>
    [RequireComponent(typeof(NetworkScene))]
    public class RoomClient : NetworkBehaviour
    {
        [Serializable]
        private class PeerInfo
        {
            public string uuid;
            public NetworkId networkId;
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class RoomInfo
        {
            public string uuid;
            public string joincode;
            public bool publish;
            public string name;
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

#region Send to Server
        [Serializable]
        private class JoinArgs
        {
            public string joincode;
            public string uuid;
            public string name;
            public bool publish;
            public PeerInfo peer;
        }

        [Serializable]
        private class AppendPeerPropertiesArgs
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class AppendRoomPropertiesArgs
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class DiscoverRoomsArgs
        {
            public NetworkId networkId;
            public string joincode;
        }

        [Serializable]
        private class SetBlobArgs
        {
            public string uuid;
            public string blob;
        }

        [Serializable]
        private class GetBlobArgs
        {
            public NetworkId networkId;
            public string uuid;
        }

        [Serializable]
        private struct PingArgs
        {
            public NetworkId networkId;
        }
#endregion

#region Recv from Server
        [Serializable]
        private class RejectedArgs
        {
            public string reason;
            public JoinArgs joinArgs;
        }

        [Serializable]
        private class SetRoomArgs
        {
            public RoomInfo room;
            public PeerInfo[] peers;
        }

        [Serializable]
        private class RoomsArgs
        {
            public List<RoomInfo> rooms;
            public string version;
            public DiscoverRoomsArgs request;
        }

        [Serializable]
        private class PeerAddedArgs
        {
            public PeerInfo peer;
        }

        [Serializable]
        private class PeerRemovedArgs
        {
            public string uuid;
        }

        [Serializable]
        private class RoomPropertiesAppendedArgs
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class PeerPropertiesAppendedArgs
        {
            public string uuid;
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        private class BlobArgs
        {
            public string uuid;
            public string blob;
        }

        private class PingResponseArgs
        {
            public string sessionId;
        }
#endregion

        // Avoid garbage by re-using the lists in these messages
        private AppendPeerPropertiesArgs _appendPeerPropertiesArgs = new AppendPeerPropertiesArgs();
        private AppendRoomPropertiesArgs _appendRoomPropertiesArgs = new AppendRoomPropertiesArgs();

        public IRoom Room { get => room; }

        /// <summary>
        /// A reference to the Peer that represents this local player. Me will be the same reference through the life of the RoomClient.
        /// It is valid after Awake() (can be used in Start()).
        /// </summary>
        public ILocalPeer Me { get => me; }

        /// <summary>
        /// The Session Id identifies a persistent connection to a RoomServer. If the Session Id returned by the Room Server changes,
        /// it indicates that the RoomClient/RoomServer have become desynchronised and the client must rejoin.
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Emitted when a Peer appears in the Room for the first time.
        /// </summary>
        public PeerEvent OnPeerAdded = new PeerEvent();

        /// <summary>
        /// Emitted when the properties of a Peer change.
        /// </summary>
        public PeerUpdatedEvent OnPeerUpdated = new PeerUpdatedEvent();

        /// <summary>
        /// Emitted when a Peer goes out of scope (e.g. it leaves the room)
        /// </summary>
        public PeerEvent OnPeerRemoved = new PeerEvent();

        /// <summary>
        /// Emitted when this peer has joined a room
        /// </summary>
        /// <remarks>
        /// There is no OnLeftRoom equivalent; leaving a room is the same as joining a new, empty room.
        /// </remarks>
        public RoomEvent OnJoinedRoom = new RoomEvent();

        /// <summary>
        /// Emitted when a Room this peer is a member of has updated its properties
        /// </summary>
        public RoomEvent OnRoomUpdated = new RoomEvent();

        /// <summary>
        /// Emitted when this peer attempts to join a room and is rejected
        /// </summary>
        public RejectedEvent OnJoinRejected = new RejectedEvent();

        /// <summary>
        /// Emitted in response to a discovery request
        /// </summary>
        public RoomsEvent OnRooms = new RoomsEvent();

        /// <summary>
        /// A list of all the Peers in the Room. This does not include the local Peer, Me.
        /// </summary>
        public IEnumerable<IPeer> Peers
        {
            get
            {
                return peers.Values;
            }
        }

        /// <summary>
        /// A list of default servers to connect to on start-up. The network must only have one RoomServer or
        /// undefined behaviour will result.
        /// </summary>
        [SerializeField]
        private ConnectionDefinition[] servers;

        /// <summary>
        /// The version the client defines which message schema it supports. Servers must match that schema
        /// in order to be able to communicate with it.
        /// </summary>
        private const string roomClientVersion = "0.0.4";

        private NetworkId roomServerObjectId = new NetworkId(1);
        private Dictionary<string, Action<string>> blobCallbacks = new Dictionary<string, Action<string>>();
        private float pingSent;
        private float pingReceived;
        private float heartbeatReceived => Time.realtimeSinceStartup - pingReceived;
        private float heartbeatSent => Time.realtimeSinceStartup - pingSent;
        public static float HeartbeatTimeout = 5f;
        public static float HeartbeatInterval = 1f;
        private float lastGetRoomsTime;
        private PeerInterfaceFriend me = new PeerInterfaceFriend(Guid.NewGuid().ToString());
        private RoomInterfaceFriend room = new RoomInterfaceFriend();

        private List<Action> actions = new List<Action>();

        private Dictionary<string, PeerInterfaceFriend> peers = new Dictionary<string, PeerInterfaceFriend>();

        public class TimeoutNotification : Notification
        {
            private RoomClient client;

            public TimeoutNotification(RoomClient client)
            {
                this.client = client;
            }

            public override string Message
            {
                get
                {
                    return $"No Connection ({ client.heartbeatReceived.ToString("0") } seconds ago)";
                }
            }
        }

        private TimeoutNotification notification;

        private class AppendPropertyLog : PropertyLog
        {
            public void Append (string key, string value)
            {
                keys.Add(key);
                values.Add(value);
            }
        }

        private class PropertyLog
        {
            protected List<string> keys = new List<string>();
            protected List<string> values = new List<string>();

            public void Clear ()
            {
                keys.Clear();
                values.Clear();
            }

            public bool TryPop (out string key, out string value)
            {
                if (Count > 0)
                {
                    key = keys[keys.Count-1];
                    value = values[values.Count-1];
                    keys.RemoveAt(keys.Count-1);
                    values.RemoveAt(values.Count-1);
                    return true;
                }
                key = string.Empty;
                value = string.Empty;
                return false;
            }

            public int Count => keys.Count;
        }

        private class RoomInterfaceFriend : IRoom
        {
            public string Name { get; protected set; }
            public string UUID { get; protected set; }
            public string JoinCode { get; protected set; }
            public bool Publish { get; protected set; }
            public string this[string key]
            {
                get => properties[key];
                set
                {
                    // Record that the request was made but do not update
                    // properties until we hear back from the server
                    _log.Append(key,value);
                }
            }

            public PropertyLog log => _log;
            public PropertyCollection properties = new PropertyCollection();

            private AppendPropertyLog _log = new AppendPropertyLog();

            public void Set(RoomInfo info)
            {
                Name = info.name;
                UUID = info.uuid;
                JoinCode = info.joincode;
                Publish = info.publish;
                properties.Set(info.keys,info.values);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return properties.GetEnumerator();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return properties.GetEnumerator();
            }
        }

        /// <summary>
        /// Internal peer interface, used for all types of peers. When peer
        /// objects leave this class they are cast to the correct subclass.
        /// </summary>
        private interface IInternalPeer : ILocalPeer {}

        private class PeerInterfaceFriend : IInternalPeer
        {
            public NetworkId networkId { get; set; }
            public string uuid { get; set; }
            public string this[string key]
            {
                get => properties[key];
                set
                {
                    if (properties.Set(key,value))
                    {
                        _log.Append(key,value);
                    }
                }
            }

            public PropertyLog log => _log;
            public PropertyCollection properties = new PropertyCollection();

            private AppendPropertyLog _log = new AppendPropertyLog();

            public PeerInterfaceFriend(string uuid)
            {
                this.uuid = uuid;
            }

            public PeerInterfaceFriend(PeerInfo info)
            {
                networkId = info.networkId;
                uuid = info.uuid;
                properties.Set(info.keys,info.values);
            }

            public PeerInfo GetPeerInfo()
            {
                var peerInfo = new PeerInfo();
                peerInfo.networkId = networkId;
                peerInfo.uuid = uuid;
                peerInfo.keys = new List<string>(properties.keys);
                peerInfo.values = new List<string>(properties.values);
                return peerInfo;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return properties.GetEnumerator();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return properties.GetEnumerator();
            }
        }

        private void Reset()
        {
            servers = new ConnectionDefinition[]
            {
                new ConnectionDefinition()
                {
                    send_to_ip = "",
                    send_to_port = "",
                    type = ConnectionType.tcp_client
                }
            };
        }

        public bool JoinedRoom
        {
            get
            {
                return Room != null && Room.UUID != null && Room.UUID != ""; // Null check because this may be called in the Editor
            }
        }

        private void Awake()
        {
            OnJoinedRoom.AddListener((room) => Debug.Log("Joined Room " + room.Name));
            OnPeerUpdated.SetExisting(me);
        }

        protected override void Started()
        {
            me.networkId = networkId;

            foreach (var item in servers)
            {
                Connect(item);
            }
        }

        protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var container = JsonUtility.FromJson<Message>(message.ToString());
            switch (container.type)
            {
                case "Rejected":
                    {
                        var args = JsonUtility.FromJson<RejectedArgs>(container.args);
                        OnJoinRejected.Invoke(new Rejection()
                        {
                            reason = args.reason,
                            uuid = args.joinArgs.uuid,
                            joincode = args.joinArgs.joincode,
                            name = args.joinArgs.name,
                            publish = args.joinArgs.publish
                        });
                    }
                    break;
                case "SetRoom":
                    {
                        var args = JsonUtility.FromJson<SetRoomArgs>(container.args);
                        room.Set(args.room);

                        var newPeerUuids = args.peers.Select(x => x.uuid);
                        var peersToRemove = new List<string>();

                        // Remove existing peers that are not in the current room
                        foreach (var existingPeer in peers.Keys)
                        {
                            if (!newPeerUuids.Contains(existingPeer))
                            {
                                peersToRemove.Add(existingPeer);
                            }
                        }
                        foreach (var uuid in peersToRemove)
                        {
                            var peer = peers[uuid];
                            peers.Remove(uuid);
                            OnPeerRemoved.Invoke(peer);
                        }

                        // Potentially update properties for existing peers
                        foreach (var newPeer in args.peers)
                        {
                            if (peers.TryGetValue(newPeer.uuid,out var peer) &&
                                peer.properties.Set(newPeer.keys,newPeer.values))
                            {
                                OnPeerUpdated.Invoke(peer);
                            }
                        }

                        // Add room peers which are not yet existing peers
                        foreach (var newPeer in args.peers)
                        {
                            if (newPeer.uuid == me.uuid)
                            {
                                continue;
                            }

                            if (!peers.ContainsKey(newPeer.uuid))
                            {
                                var peer = new PeerInterfaceFriend(newPeer);
                                peers.Add(newPeer.uuid,peer);
                                OnPeerAdded.Invoke(peer);
                                OnPeerUpdated.Invoke(peer);
                            }
                        }

                        OnJoinedRoom.Invoke(room);
                        OnRoomUpdated.Invoke(room);
                    }
                    break;
                case "Rooms":
                    {
                        var args = JsonUtility.FromJson<RoomsArgs>(container.args);
                        if (roomClientVersion != args.version)
                        {
                            Debug.LogError($"Your version {roomClientVersion} of Ubiq doesn't match the server version {args.version}.");
                        }
                        var rooms = new List<IRoom>();
                        for (int i = 0; i < args.rooms.Count; i++)
                        {
                            var rif = new RoomInterfaceFriend();
                            rif.Set(args.rooms[i]);
                            rooms.Add(rif);
                        }

                        var request = new RoomsDiscoveredRequest();
                        request.joincode = args.request.joincode;

                        OnRooms.Invoke(rooms,request);
                    }
                    break;
                case "PeerAdded":
                    {
                        var args = JsonUtility.FromJson<PeerAddedArgs>(container.args);
                        if (!peers.ContainsKey(args.peer.uuid))
                        {
                            var peer = new PeerInterfaceFriend(args.peer);
                            peers.Add(peer.uuid, peer);
                            OnPeerAdded.Invoke(peer);
                            OnPeerUpdated.Invoke(peer);
                        }
                    }
                    break;
                case "PeerRemoved":
                    {
                        var args = JsonUtility.FromJson<PeerRemovedArgs>(container.args);
                        if (peers.TryGetValue(args.uuid,out var peer))
                        {
                            peers.Remove(args.uuid);
                            OnPeerRemoved.Invoke(peer);
                        }
                    }
                    break;
                case "RoomPropertiesAppended":
                    {
                        var args = JsonUtility.FromJson<RoomPropertiesAppendedArgs>(container.args);
                        if (room.properties.Append(args.keys,args.values))
                        {
                            OnRoomUpdated.Invoke(room);
                        }
                    }
                    break;
                case "PeerPropertiesAppended":
                    {
                        var args = JsonUtility.FromJson<PeerPropertiesAppendedArgs>(container.args);
                        if (peers.TryGetValue(args.uuid,out var peer) &&
                            peer.properties.Append(args.keys,args.values))
                        {
                            OnPeerUpdated.Invoke(peer);
                        }
                    }
                    break;
                case "Blob":
                    {
                        var args = JsonUtility.FromJson<BlobArgs>(container.args);
                        if(blobCallbacks.TryGetValue(args.uuid,out var callback))
                        {
                            callback(args.blob);
                            blobCallbacks.Remove(args.uuid);
                        }
                    }
                    break;
                case "Ping":
                    {
                        pingReceived = Time.realtimeSinceStartup;
                        PlayerNotifications.Delete(ref notification);
                        var response = JsonUtility.FromJson<PingResponseArgs>(container.args);
                        OnPingResponse(response);
                    }
                    break;
            }
        }

        private void SendToServer(string type, object argument)
        {
            networkScene.SendJson(roomServerObjectId, new Message(type, argument));
        }

        /// <summary>
        /// Create a new room with the specified name and visibility
        /// </summary>
        /// <param name="name">The name of the new room</param>
        /// <param name="bool">Whether others should be able to browse for this room</param>
        public void Join(string name, bool publish)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinArgs()
                {
                    name = name,
                    publish = publish,
                    peer = me.GetPeerInfo()
                });
                me.log.Clear(); // Already sent server up-to-date properties
            });
        }

        /// <summary>
        /// Joins an existing room using a join code.
        /// </summary>
        public void Join(string joincode)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinArgs()
                {
                    joincode = joincode,
                    peer = me.GetPeerInfo()
                });
                me.log.Clear(); // Already sent server up-to-date properties
            });
        }

        /// <summary>
        /// Joins an existing room using a join code.
        /// </summary>
        public void Join(Guid guid)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinArgs()
                {
                    uuid = guid.ToString(),
                    peer = me.GetPeerInfo()
                });
                me.log.Clear(); // Already sent server up-to-date properties
            });
        }

        /// <summary>
        /// Creates a new connection on the Network Scene
        /// </summary>
        /// <remarks>
        /// RoomClient is one of a few components able to create new connections. Usually it will be user code that makes such connections.
        /// </remarks>
        public void Connect(ConnectionDefinition connection)
        {
            networkScene.AddConnection(Connections.Resolve(connection));
        }

        private void Update()
        {
            actions.ForEach(a => a());
            actions.Clear();

            if (me.log.Count > 0)
            {
                _appendPeerPropertiesArgs.keys.Clear();
                _appendPeerPropertiesArgs.values.Clear();
                while (me.log.TryPop(out var key, out var value))
                {
                    _appendPeerPropertiesArgs.keys.Add(key);
                    _appendPeerPropertiesArgs.values.Add(value);
                }

                SendToServer("AppendPeerProperties", _appendPeerPropertiesArgs);
                OnPeerUpdated.Invoke(me);
            }

            if (room.log.Count > 0)
            {
                _appendRoomPropertiesArgs.keys.Clear();
                _appendRoomPropertiesArgs.values.Clear();
                while (room.log.TryPop(out var key, out var value))
                {
                    _appendRoomPropertiesArgs.keys.Add(key);
                    _appendRoomPropertiesArgs.values.Add(value);
                }

                SendToServer("AppendRoomProperties", _appendRoomPropertiesArgs);
            }

            if (heartbeatSent > HeartbeatInterval)
            {
                Ping();
            }

            if (heartbeatReceived > HeartbeatTimeout)
            {
                if (notification == null)
                {
                    notification = PlayerNotifications.Show(new TimeoutNotification(this));
                }
            }
        }

        /// <summary>
        /// </summary>
        public void DiscoverRooms(string joincode = "")
        {
            var args = new DiscoverRoomsArgs();
            args.networkId = networkId;
            args.joincode = joincode;
            SendToServer("DiscoverRooms", args);
        }

        /// <summary>
        /// Retrieves the value of a blob from a room. When the server responds a call will be made to callback.
        /// Only one callback is made per server response, regardless of how many times GetBlob was called between.
        /// If a blob does not exist, callback will be called with an empty string. (In this case, callback may issue
        /// another GetBlob call, to poll the server, if it is known that a value will eventually be set.)
        /// Blobs are by convention immutable, so it is safe to cache the result, once a valid result is returned.
        /// </summary>
        public void GetBlob(string room, string uuid, Action<string> callback)
        {
            var blob = new Blob()
            {
                room = room,
                uuid = uuid
            };
            var key = blob.GetKey();
            if(!blobCallbacks.ContainsKey(key))
            {
                blobCallbacks.Add(key, callback);
            }
            var request = new GetBlobRequest()
            {
                networkId = networkId,
                blob = blob
            };
            SendToServer("GetBlob", request);
        }

        private void SetBlob(string room, string uuid, string blob) // private because this could encourage re-using uuids, which is not allowed because blobs are meant to be immutable
        {
            if (blob.Length > 0)
            {
                SendToServer("SetBlob", new SetBlobRequest()
                {
                    blob = new Blob()
                    {
                        room = room,
                        uuid = uuid,
                        blob = blob
                    }
                });

            }
        }

        /// <summary>
        /// Sets a persistent variable that exists for as long as the room does. This variable is not sent with
        /// Room Updates, but rather only when requested, making this method suitable for larger data.
        /// If the room does not exist, the data is discarded.
        /// </summary>
        public string SetBlob(string room, string blob)
        {
            var uuid = Guid.NewGuid().ToString();
            SetBlob(room, uuid, blob);
            return uuid;
        }

        public void Ping()
        {
            pingSent = Time.realtimeSinceStartup;
            SendToServer("Ping", new PingArgs() { networkId = networkId });
        }

        private void OnPingResponse(PingResponseArgs args)
        {
            if(SessionId != args.sessionId && SessionId != null)
            {
                Join(name:"",publish:false); // The RoomClient has re-established connectivity with the RoomServer, but under a different state. So, leave the room and let the user code re-establish any state.
            }

            SessionId = args.sessionId;
        }

        /// <summary>
        /// Sets the default Server this should connect to on Start.
        /// </summary>
        /// <remarks>
        /// Replaces the existing setting, if any. Must be called before Start; will have no effect after Start.
        /// </remarks>
        public void SetDefaultServer(ConnectionDefinition definition)
        {
            servers = new ConnectionDefinition[] { definition };
        }

        public static RoomClient Find(MonoBehaviour Component)
        {
            return NetworkScene.FindNetworkScene(Component).GetComponent<RoomClient>();
        }
    }
}