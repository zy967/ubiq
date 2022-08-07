﻿using UnityEngine;
using System;
using Ubiq.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ubiq.Messaging
{
    /// <summary>
    /// Wraps a reference counted message for when interacting with the scene graph bus
    /// </summary>
    /// <remarks>
    /// This is a wrapper rather than a subclass, because the networking code will allocate reference counted objects, with no knowledge of the sgb
    /// </remarks>
    public struct ReferenceCountedSceneGraphMessage : IReferenceCounter
    {
        internal const int header = 8; // 8 Bytes for the ObjectId
        internal ReferenceCountedMessage buffer;

        public ReferenceCountedSceneGraphMessage(ReferenceCountedMessage buffer)
        {
            this.buffer = buffer;
            start = buffer.start + header;
            length = buffer.length - header;
        }

        public int start
        {
            get;
            private set;
        }

        public int length
        {
            get;
            private set;
        }

        public byte[] bytes
        {
            get
            {
                return buffer.bytes;
            }
        }

        public Span<byte> data
        {
            get
            {
                return new Span<byte>(bytes, start, length);
            }
        }

        public NetworkId objectid
        {
            get
            {
                return new NetworkId(buffer.bytes, buffer.start);
            }
            set
            {
                value.ToBytes(buffer.bytes, buffer.start);
            }
        }

        public void Acquire()
        {
            buffer.Acquire();
        }

        public void Release()
        {
            buffer.Release();
        }

        public static ReferenceCountedSceneGraphMessage Rent(int length)
        {
            var msg = new ReferenceCountedSceneGraphMessage(MessagePool.Shared.Rent(length + header));
            msg.objectid = new NetworkId(0);
            return msg;
        }

        public static ReferenceCountedSceneGraphMessage Rent(string content)
        {
            var msg = Rent(Encoding.UTF8.GetByteCount(content));
            Encoding.UTF8.GetBytes(content, 0, content.Length, msg.bytes, msg.start);
            return msg;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(bytes, start, length);
        }

        public T FromJson<T>()
        {
            return JsonUtility.FromJson<T>(this.ToString());
        }
    }

    /// <summary>
    /// This instance holds the identity of a Network Object - like a passport. The NetworkId may change at runtime.
    /// Ids can be unique on the network, or shared, depending on whether messages should be one-to-one or one-to-many
    /// between instances.
    /// </summary>
    [Serializable]
    public struct NetworkId : IEquatable<NetworkId>, IEqualityComparer<NetworkId>
    {
        [SerializeField]
        private UInt32 a;
        [SerializeField]
        private UInt32 b;

        public const int Size = 8;

        public NetworkId(UInt32 id)
        {
            a = 0;
            b = id;
        }

        public NetworkId(string id)
        {
            a = 0;
            b = 0;

            if (id == null)
            {
                return;
            }

            ulong numeric;
            id = id.Replace("-", "");
            if (ulong.TryParse(id, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out numeric))
            {
                a = (uint)(numeric >> 32);
                b = (uint)(numeric & 0xffffffffL);
                return;
            }
            if (ulong.TryParse(id, System.Globalization.NumberStyles.AllowHexSpecifier, System.Globalization.CultureInfo.CurrentCulture, out numeric))
            {
                a = (uint)(numeric >> 32);
                b = (uint)(numeric & 0xffffffffL);
                return;
            }

            Debug.LogException(new ArgumentException($"Invalid Network Id Format {id}"));
        }

        public NetworkId(byte[] buffer, int offset)
        {
            a = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 0, 4));
            b = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset + 4, 4));
        }

        public override string ToString()
        {
            return a.ToString("x8") + "-" + b.ToString("x8");
        }

        public void ToBytes(byte[] buffer, int offset)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 0, 4), a);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(buffer, offset + 4, 4), b);
        }

        public bool Equals(NetworkId other)
        {
            return a == other.a && b == other.b;
        }

        public bool Equals(NetworkId x, NetworkId y)
        {
            return x.a == y.a && x.b == y.b;
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public override bool Equals(object obj)
        {
            return Equals((NetworkId)obj);
        }

        public int GetHashCode(NetworkId obj)
        {
            return obj.a.GetHashCode();
        }

        public bool Valid
        {
            get
            {
                return b != 0;
            }
        }

        public static bool operator ==(NetworkId a, NetworkId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(NetworkId a, NetworkId b)
        {
            return !a.Equals(b);
        }

        public static NetworkId Unique()
        {
            return IdGenerator.GenerateUnique();
        }

        public static implicit operator bool(NetworkId id)
        {
            return (id.a != 0 || id.b != 0);
        }

        public static NetworkId Null
        {
            get
            {
                return new NetworkId(0);
            }
        }

        public static NetworkId Create(NetworkId nameSpace, string service)
        {
            // Quick Hash..

            NetworkId id;
            id.a = nameSpace.a;
            id.b = nameSpace.b;
            var bytes = Encoding.UTF8.GetBytes(service);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i % 2 != 0)
                {
                    id.a = id.a * bytes[i] + id.b;
                }
                else
                {
                    id.b = id.b * bytes[i] + id.a;
                }
            }

            return id;
        }
    }
}
