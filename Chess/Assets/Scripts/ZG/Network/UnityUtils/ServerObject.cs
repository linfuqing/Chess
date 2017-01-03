using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Assertions;

namespace ZG.Network
{
    [RequireComponent(typeof(Node))]
    public class ServerObject : MonoBehaviour
    {
        public event Func<NetworkWriter, short> onInit; 
        public event Action onEnable;
        public event Action onDisable;

        private Node __node;
        private NetworkWriter __writer;
        private int __refCount;

        public Node node
        {
            get
            {
                return __node;
            }
        }

        public Server host
        {
            get
            {
                return __node == null ? null : __node.host as Server;
            }
        }

        public short type
        {
            get
            {
                return __node == null ? (short)-1 : __node.type;
            }

            set
            {
                Server host = __node == null ? null : __node.host as Server;
                if (host == null)
                    return;

                bool result = host.Replace(__node.index, value);
                Assert.IsTrue(result);
                if (result)
                {
                    Delegate[] invocationList = this.onInit == null ? null : this.onInit.GetInvocationList();
                    if (invocationList != null)
                    {
                        Func<NetworkWriter, short> onInit;
                        foreach (Delegate invocation in invocationList)
                        {
                            onInit = invocation as Func<NetworkWriter, short>;
                            if (onInit != null)
                                RpcEnd(onInit(RpcStart()));
                        }
                    }
                }
            }
        }

        public int roomIndex
        {
            get
            {
                Server host = __node == null ? null : __node.host as Server;
                if (host == null)
                    return -1;

                Server.Node node;
                if (!host.GetNode(__node.index, out node))
                    return -1;

                return node.roomIndex;
            }

            set
            {
                Server host = __node == null ? null : __node.host as Server;
                Assert.IsNotNull(host);
                if (host == null)
                    return;
                
                host.Move(__node.index, value, __Add, __Remove);
            }
        }
        
        public void Awake()
        {
            __node = GetComponent<Node>();
            if (__node != null)
            {
                __node.onCreate += __OnCreate;
                __node.onDestroy += __OnDestroy;
            }
        }
        
        public NetworkWriter RpcStart()
        {
            if (__writer == null)
                __writer = new NetworkWriter();
            else
                __writer.SeekZero();

            return __writer;
        }

        public void RpcEnd(short handle)
        {
            Assert.IsNotNull(__node);
            if (__node == null)
                return;

            if (__writer == null)
                __node.Rpc(handle, null, 0);
            else
                __node.Rpc(handle, __writer.AsArray(), __writer.Position);
        }
        
        public void RegisterHandler(short handle, Action<NetworkReader> action)
        {
            Assert.IsNotNull(__node);
            if (__node == null)
                return;

            __node.RegisterHandler(handle, action == null ? null : (Action<int, NetworkReader>)delegate (int connectionId, NetworkReader reader)
            {
                Server host = __node == null ? null : __node.host as Server;
                if (host != null)
                {
                    bool isContains = false;
                    IEnumerable<int> nodeIndices = host.GetNodeIndices(connectionId);
                    if (nodeIndices != null)
                    {
                        foreach (int nodeIndex in nodeIndices)
                        {
                            if (nodeIndex == __node.index)
                            {
                                isContains = true;

                                break;
                            }
                        }
                    }

                    if (!isContains)
                        return;
                }

                action(reader);
            });
        }
        
        private void __Add(Node node)
        {
            ServerObject instance = node == null ? null : node.GetComponent<ServerObject>();
            Assert.IsNotNull(instance);
            if (instance == null || instance == this)
                return;

            Server host = __node == null ? null : __node.host as Server;
            Assert.IsNotNull(host);
            if (host == null)
                return;

            short index = __node.index;
            Server.Node temp;
            bool result = host.GetNode(index, out temp);
            Assert.IsTrue(result);
            if (!result)
                return;

            if (temp.connectionId >= 0)
            {
                if (__refCount == 0 && onEnable != null)
                    onEnable();

                ++__refCount;

                if (instance.__refCount == 0 && instance.onEnable != null)
                    instance.onEnable();

                ++instance.__refCount;

                Delegate[] invocationList = instance.onInit == null ? null : instance.onInit.GetInvocationList();
                if (invocationList != null)
                {
                    Func<NetworkWriter, short> onInit;
                    foreach (Delegate invocation in invocationList)
                    {
                        onInit = invocation as Func<NetworkWriter, short>;
                        if (onInit != null)
                        {
                            host.Send(
                                temp.connectionId,
                                node.index,
                                onInit(instance.RpcStart()),
                                instance.__writer.AsArray(),
                                instance.__writer.Position);
                        }
                    }
                }
            }
            else
            {
                if (!host.GetNode(node.index, out temp))
                    return;

                if (temp.connectionId >= 0)
                {
                    if (__refCount == 0 && onEnable != null)
                        onEnable();

                    ++__refCount;

                    ++instance.__refCount;
                    
                    Delegate[] invocationList = this.onInit == null ? null : this.onInit.GetInvocationList();
                    if (invocationList != null)
                    {
                        Func<NetworkWriter, short> onInit;
                        foreach (Delegate invocation in invocationList)
                        {
                            onInit = invocation as Func<NetworkWriter, short>;
                            if (onInit != null)
                                host.Send(
                                    temp.connectionId,
                                    index, 
                                    onInit(RpcStart()), 
                                    __writer.AsArray(), 
                                    __writer.Position);
                        }
                    }
                }
            }
        }

        private void __Remove(Node node)
        {
            ServerObject instance = node == null ? null : node.GetComponent<ServerObject>();
            Assert.IsNotNull(instance);
            if (instance == null || instance == this)
                return;

            Server host = __node == null ? null : __node.host as Server;
            Assert.IsNotNull(host);
            if (host == null)
                return;

            Server.Node temp;
            bool result = host.GetNode(__node.index, out temp);
            Assert.IsTrue(result);
            if (!result)
                return;

            if (temp.connectionId >= 0)
            {
                --instance.__refCount;

                if (instance.__refCount == 0 && instance.onDisable != null)
                    instance.onDisable();

                --__refCount;
                if (__refCount == 0 && onDisable != null)
                    onDisable();
            }
            else
            {
                if (!host.GetNode(node.index, out temp))
                    return;

                if (temp.connectionId >= 0)
                {
                    --__refCount;
                    if (__refCount == 0 && onDisable != null)
                        onDisable();

                    --instance.__refCount;
                }
            }
        }

        private void __OnCreate()
        {
            Server host = __node == null ? null : __node.host as Server;
            Assert.IsNotNull(host);
            if (host == null)
                return;
            
            Server.Node node;
            if (host.GetNode(__node.index, out node))
            {
                IEnumerable<KeyValuePair<int, Node>> room = host.GetRoom(node.roomIndex);
                if (room != null)
                {
                    foreach (KeyValuePair<int, Node> pair in room)
                        __Add(pair.Value);
                }

                IEnumerable<int> neighborRoomIndices = host.GetNeighborRoomIndices(node.roomIndex);
                if (neighborRoomIndices != null)
                {
                    foreach (int neighborRoomIndex in neighborRoomIndices)
                    {
                        room = host.GetRoom(neighborRoomIndex);
                        if (room != null)
                        {
                            foreach (KeyValuePair<int, Node> pair in room)
                                __Add(pair.Value);
                        }
                    }
                }
            }
        }

        private void __OnDestroy()
        {
            Server host = __node == null ? null : __node.host as Server;
            Assert.IsNotNull(host);
            if (host == null)
                return;
            
            Server.Node node;
            if (host.GetNode(__node.index, out node))
            {
                IEnumerable<KeyValuePair<int, Network.Node>> room = host.GetRoom(node.roomIndex);
                if (room != null)
                {
                    foreach (KeyValuePair<int, Network.Node> pair in room)
                        __Remove(pair.Value);
                }

                IEnumerable<int> neighborRoomIndices = host.GetNeighborRoomIndices(node.roomIndex);
                if (neighborRoomIndices != null)
                {
                    foreach (int neighborRoomIndex in neighborRoomIndices)
                    {
                        room = host.GetRoom(neighborRoomIndex);
                        if (room != null)
                        {
                            foreach (KeyValuePair<int, Network.Node> pair in room)
                                __Remove(pair.Value);
                        }
                    }
                }
            }
        }
    }
}
