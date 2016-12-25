using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System;
using System.Collections.Generic;

namespace ZG.Network
{
    public class Client : MonoBehaviour, IHost
    {
        public event NetworkMessageDelegate onError;
        public event NetworkMessageDelegate onConnect;
        public event NetworkMessageDelegate onDisconnect;

        public event Action<Node> onRegistered;
        public event Action<Node> onUnregistered;

        public int port = 443;
        public string ipAddress = "localhost";
        public HostTopology hostTopology;

        public Node[] prefabs;

        private NetworkClient __client;
        private NetworkWriter __writer;
        private HostMessage __message;
        private System.Collections.Generic.Dictionary<int, Node> __nodes;

        public bool isConnected
        {
            get
            {
                return __client != null && __client.isConnected;
            }
        }

        public int count
        {
            get
            {
                return __nodes == null ? 0 : __nodes.Count;
            }
        }

        public IEnumerable<Node> nodes
        {
            get
            {
                return __nodes == null ? null : __nodes.Values;
            }
        }

        public Node localPlayer
        {
            get
            {
                if (__nodes == null)
                    return null;

                Node player;
                foreach (KeyValuePair<int, Node> pair in __nodes)
                {
                    player = pair.Value;
                    if (player != null && player._isLocalPlayer)
                        return player;
                }

                return null;
            }
        }

        public Node Get(int index)
        {
            if (__nodes == null)
                return null;

            Node result;
            if (__nodes.TryGetValue(index, out result))
                return result;

            return null;
        }

        public bool Replace(int index, Node target)
        {
            if (__nodes == null)
                return false;

            Node temp;
            if (!__nodes.TryGetValue(index, out temp))
                return false;

            if (target != null)
                target.CopyFrom(temp);

            if (temp is Node)
            {
                if (temp._onDestroy != null)
                    temp._onDestroy();
            }

            if (temp != null)
                Destroy(temp.gameObject);

            __nodes[index] = target;

            if (target._onCreate != null)
                target._onCreate();

            return true;
        }

        public void Create()
        {
            if (__client == null)
            {
                __client = new NetworkClient();

                if (onError != null)
                    __client.RegisterHandler(MsgType.Error, onError);

                if (onConnect != null)
                    __client.RegisterHandler(MsgType.Connect, onConnect);

                __client.RegisterHandler(MsgType.Disconnect, __OnDisconnect);

                __client.RegisterHandler((short)HostMessageType.Register, __OnRegistered);
                __client.RegisterHandler((short)HostMessageType.Unregister, __OnUnregistered);
                __client.RegisterHandler((short)HostMessageType.Rpc, __OnRpc);

                __client.Configure(hostTopology);
            }

            __client.Connect(ipAddress, port);
        }

        public virtual void Shutdown()
        {
            if (__nodes != null)
            {
                Node instance;
                foreach (KeyValuePair<int, Node> pair in __nodes)
                {
                    instance = pair.Value;
                    if (instance is Node)
                    {
                        if (instance._onDestroy != null)
                            instance._onDestroy();
                    }

                    if (instance != null)
                        Destroy(instance.gameObject);
                }

                __nodes.Clear();
            }

            if (__client != null)
            {
                //__client.Disconnect();
                __client.Shutdown();
                __client = null;
            }
        }

        public void Send(short messageType, MessageBase message)
        {

#if DEBUG
            if (__client == null)
                throw new InvalidOperationException();
#endif

            __client.Send(messageType, message == null ? new EmptyMessage() : message);
        }

        public void UnregisterHandler(short messageType)
        {
            if (__client != null)
                __client.UnregisterHandler(MsgType.Disconnect);
        }

        public void RegisterHandler(short messageType, NetworkMessageDelegate handler)
        {
#if DEBUG
            if (__client == null)
                throw new InvalidOperationException();
#endif

            __client.RegisterHandler(messageType, handler);
        }

        public void Register(MessageBase message)
        {
#if DEBUG
            if (__client == null)
                throw new InvalidOperationException();
#endif
            
            __client.Send((short)HostMessageType.Register, message);
        }

        public void Unregister(short index, int count, byte[] bytes)
        {
#if DEBUG
            if (__client == null)
                throw new InvalidOperationException();
#endif

            __client.Send((short)HostMessageType.Unregister, __GetMessage(index, count, bytes));
        }

        public void Unregister(byte[] bytes, int count)
        {
            Node player = localPlayer;
            if (player != null)
                Unregister(player._index, count, bytes);
        }

        public void Rpc(short index, short handle, byte[] bytes, int count)
        {
            NetworkConnection connection = __client == null ? null : __client.connection;
            if (connection == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(bytes, count);

            connection.Send((short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }

        public void Rpc(short index, short handle, MessageBase message)
        {
            NetworkConnection connection = __client == null ? null : __client.connection;
            if (connection == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(message);

            connection.Send((short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }

        private void __OnRegistered(NetworkMessage message)
        {
            HostMessage temp = message == null ? null : message.ReadMessage<HostMessage>();
            if (temp == null || temp.index < 0)
            {
                Debug.LogError("Register Fail.");

                return;
            }

            NetworkReader reader = new NetworkReader(temp.bytes);
            short type = reader.ReadInt16();
            Node instance = prefabs == null || type < 0 || type >= prefabs.Length ? null : prefabs[type];
            instance = instance == null ? null : Instantiate(instance);

#if DEBUG
            if (instance == null)
                throw new InvalidOperationException();
#endif

            instance._isLocalPlayer = reader.ReadBoolean();
            instance._type = type;
            instance._index = temp.index;
            instance._host = this;

            if (__nodes == null)
                __nodes = new System.Collections.Generic.Dictionary<int, Node>();

            __nodes.Add(temp.index, instance);

            if (instance._onCreate != null)
                instance._onCreate();

            if (onRegistered != null)
                onRegistered(instance);
        }

        private void __OnUnregistered(NetworkMessage message)
        {
            HostMessage temp = message == null ? null : message.ReadMessage<HostMessage>();
            Node target;
            if (temp == null || temp.index < 0 || __nodes == null || !__nodes.TryGetValue(temp.index, out target))
            {
                Debug.LogError("Unregister Fail.");

                return;
            }

            if (onUnregistered != null)
                onUnregistered(target);

            if (target is Node)
            {
                if (target._onDestroy != null)
                    target._onDestroy();
            }

            if (target != null)
                Destroy(target.gameObject);

            __nodes.Remove(temp.index);
        }

        private void __OnRpc(NetworkMessage message)
        {

            NetworkConnection connection = message == null ? null : message.conn;
#if DEBUG
            if (connection == null)
                throw new InvalidOperationException();
#endif

            HostMessage temp = message.ReadMessage<HostMessage>();
            Node target;
            if (__nodes == null ||
                temp == null ||
                !__nodes.TryGetValue(temp.index, out target) ||
                (target == null && !(target is UnityEngine.Object)) ||
                temp.bytes == null ||
                temp.bytes.Length < 2)
            {
                Debug.LogError("Rpc Fail.");

                return;
            }

            NetworkReader reader = new NetworkReader(temp.bytes);

            target.InvokeHandler(reader.ReadInt16(), connection.connectionId, reader);
        }

        private void __OnDisconnect(NetworkMessage message)
        {
            if (__nodes != null)
            {
                Node instance;
                foreach (KeyValuePair<int, Node> pair in __nodes)
                {
                    instance = pair.Value;

                    if (instance is Node)
                    {
                        if (instance._onDestroy != null)
                            instance._onDestroy();
                    }

                    if (instance != null)
                        Destroy(instance.gameObject);
                }

                __nodes.Clear();
            }

            if (onDisconnect != null)
                onDisconnect(message);
        }

        private NetworkWriter __GetWriter()
        {
            if (__writer == null)
                __writer = new NetworkWriter();
            else
                __writer.SeekZero();

            return __writer;
        }

        private HostMessage __GetMessage(short index, int count, byte[] bytes)
        {
            if (__message == null)
                __message = new HostMessage(index, count, bytes);
            else
            {
                __message.index = index;
                __message.count = count;
                __message.bytes = bytes;
            }

            return __message;
        }

        void OnDestroy()
        {
            Shutdown();
        }
    }
}