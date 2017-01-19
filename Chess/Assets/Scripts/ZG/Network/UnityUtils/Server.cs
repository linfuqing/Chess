using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG.Network
{
    public class Server : MonoBehaviour, IHost
    {
        public struct Node
        {
            public int roomIndex;
            public int playerIndex;
            public int connectionId;
            public int connectionIndex;
        }

        private class Room : IEnumerable<KeyValuePair<int, Network.Node>>
        {
            private Pool<Network.Node> __players;
            private IEnumerable<int> __roomIndices;

            public int count
            {
                get
                {
                    return __players == null ? 0 : __players.count;
                }
            }

            public int nextIndex
            {
                get
                {
                    return __players == null ? 0 : __players.nextIndex;
                }
            }

            public IEnumerable<int> neighborRoomIndices
            {
                get
                {
                    return __roomIndices;
                }
            }

            public Room(IEnumerable<int> roomIndices)
            {
                __roomIndices = roomIndices == null ? null : new List<int>(roomIndices).ToArray();
            }

            public Network.Node Get(int index)
            {
                if (__players == null)
                    return null;

                Network.Node player;
                if (__players.TryGetValue(index, out player))
                    return player;

                return null;
            }

            public void Set(Network.Node player, int index)
            {
                if (__players == null)
                    __players = new Pool<Network.Node>();

                __players.Insert(index, player);
            }

            public bool Delete(int index)
            {
                if (__players == null)
                    return false;

                return __players.RemoveAt(index);
            }

            public void Send(short type, MessageBase message, Predicate<Network.Node> predicate)
            {
                if (__players == null)
                    return;

                Server host;
                Node node;
                foreach (Network.Node player in __players)
                {
                    host = (player is UnityEngine.Object && (predicate == null || predicate(player))) ? player._host as Server : null;
                    if (host == null || !host.GetNode(player._index, out node))
                        continue;

                    if(node.connectionId >= 0)
                        NetworkServer.SendToClient(node.connectionId, type, message);
                }
            }

            public IEnumerator<KeyValuePair<int, Network.Node>> GetEnumerator()
            {
                return __players == null ? null : ((IEnumerable<KeyValuePair<int, Network.Node>>)__players).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public event NetworkMessageDelegate onError;
        public event NetworkMessageDelegate onConnect;
        public event NetworkMessageDelegate onDisconnect;

        public event Action<Network.Node> onRegistered;
        public event Action<Network.Node> onUnregistered;
        
        public int port = 443;
        public HostTopology hostTopology;

        public Network.Node[] prefabs;

        private NetworkWriter __writer;
        private HostMessage __message;

        private Pool<Node> __nodes;
        private Pool<Room> __rooms;
        private Dictionary<int, Pool<int>> __nodeIndices;

        public bool isActive
        {
            get
            {
                return NetworkServer.active;
            }
        }

        public int nodeCount
        {
            get
            {
                return __nodes == null ? 0 : __nodes.count;
            }
        }

        public int roomCount
        {
            get
            {
                return __rooms == null ? 0 : __rooms.count;
            }
        }

        public int nextRoomIndex
        {
            get
            {
                return __rooms == null ? 0 : __rooms.nextIndex;
            }
        }

        public int nextNodeIndex
        {
            get
            {
                return __nodes == null ? 0 : __nodes.nextIndex;
            }
        }

        public IEnumerable<KeyValuePair<int, Node>> nodes
        {
            get
            {
                return __nodes;
            }
        }

        public bool GetNode(int index, out Node node)
        {
            if (__nodes == null)
            {
                node = default(Node);

                return false;
            }

            return __nodes.TryGetValue(index, out node);
        }

        public IEnumerable<int> GetNodeIndices(int connectionId)
        {
            if (__nodeIndices == null)
                return null;

            Pool<int> indices;
            if (__nodeIndices.TryGetValue(connectionId, out indices))
                return indices;

            return null;
        }

        public IEnumerable<KeyValuePair<int, Network.Node>> GetRoom(int index)
        {
            if (__rooms == null)
                return null;

            Room room;
            if (__rooms.TryGetValue(index, out room))
                return room;

            return null;
        }

        public IEnumerable<int> GetNeighborRoomIndices(int index)
        {
            if (__rooms == null)
                return null;

            Room room;
            if (__rooms.TryGetValue(index, out room) && room != null)
                return room.neighborRoomIndices;

            return null;
        }

        public int GetRoomNextIndex(int roomIndex)
        {
            if (__rooms == null)
                return 0;

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room) || room == null)
                return 0;

            return room.nextIndex;
        }

        public int GetCount(int roomIndex)
        {
            if (__rooms == null)
                return 0;

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room) || room == null)
                return 0;

            return room.count;
        }

        public int GetConnectionCount(int roomIndex)
        {
            if (__nodes == null || __rooms == null)
                return 0;

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room) || room == null)
                return 0;

            int count = 0;
            Node node;
            Network.Node instance;
            foreach (KeyValuePair<int, Network.Node> pair in room)
            {
                instance = pair.Value;
                if (instance != null && __nodes.TryGetValue(instance.index, out node) && node.connectionId >= 0)
                    ++count;
            }

            return count;
        }

        public Network.Node Get(int roomIndex, int playerIndex)
        {
            if (__rooms == null)
                return null;

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room) || room == null)
                return null;

            return room.Get(playerIndex);
        }

        public Network.Node Get(int index)
        {
            if (__nodes == null || __rooms == null)
                return null;

            Node node;
            Room room;
            if (__nodes.TryGetValue(index, out node) &&
                __rooms.TryGetValue(node.roomIndex, out room) &&
                room != null)
                return room.Get(node.playerIndex);

            return null;
        }

        public void Create()
        {
            NetworkServer.Configure(hostTopology);
            NetworkServer.Listen(port);

            if (onError != null)
                NetworkServer.RegisterHandler(MsgType.Error, onError);

            if (onConnect != null)
                NetworkServer.RegisterHandler(MsgType.Connect, onConnect);

            NetworkServer.RegisterHandler(MsgType.Disconnect, __OnDisconnect);

            NetworkServer.RegisterHandler((short)HostMessageType.Register, __OnRegistered);
            NetworkServer.RegisterHandler((short)HostMessageType.Unregister, __OnUnregistered);
            NetworkServer.RegisterHandler((short)HostMessageType.Rpc, __OnRpc);

            DontDestroyOnLoad(gameObject);
        }

        public void Shutdown()
        {
            NetworkServer.Shutdown();
        }

        public bool Unregister(short index, NetworkReader reader)
        {
            if (__nodes == null)
                return false;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return false;

            return __Unregister(reader, node.connectionId, index);
        }

        public bool Register(NetworkReader reader, Network.Node npc)
        {
            short type;
            Node node;
            if (!_Register(reader, -1, out type, out node.roomIndex, out node.playerIndex))
                return false;
            
            if (__rooms == null)
                __rooms = new Pool<Room>();

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
            {
                room = new Room(_GetNeighborRoomIndices(node.roomIndex));

                __rooms.Insert(node.roomIndex, room);
            }
            
            Network.Node temp = room.Get(node.playerIndex);
            if (npc == null)
            {
                if (temp == null)
                {
                    npc = type < 0 || prefabs == null || prefabs.Length <= type ? null : Instantiate(prefabs[type]);

                    if (npc == null)
                        return false;

                    if (__nodes == null)
                        __nodes = new Pool<Node>();

                    npc._index = (short)__nodes.nextIndex;
                    
                    room.Set(npc, node.playerIndex);

                    node.connectionId = -1;
                    node.connectionIndex = -1;

                    __nodes.Add(node);

                }
                else
                    npc = temp;
            }
            else
            {
                if(temp == null)
                {
                    if (__nodes == null)
                        __nodes = new Pool<Node>();

                    npc._index = (short)__nodes.nextIndex;
                    
                    room.Set(npc, node.playerIndex);
                    
                    node.connectionId = -1;
                    node.connectionIndex = -1;

                    __nodes.Add(node);
                }
                else
                {
                    npc.CopyFrom(temp);

                    if (temp._onDestroy != null)
                        temp._onDestroy();

                    Destroy(temp.gameObject);

                    room.Set(npc, node.playerIndex);

                    node.connectionId = -1;
                    node.connectionIndex = -1;
                    __nodes.Insert(npc._index, node);
                }
            }
            
            npc._isLocalPlayer = false;
            npc._type = type;

            npc._host = this;

            NetworkWriter writer = __GetWriter();
            writer.Write(type);
            writer.Write(false);

            HostMessage message = __GetMessage(npc._index, writer.Position, writer.AsArray());
            room.Send((short)HostMessageType.Register, message, null);
            
            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if(neighborRoomIndices != null)
            {
                Room neighborRoom;
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out neighborRoom) && neighborRoom != null)
                        neighborRoom.Send((short)HostMessageType.Register, message, null);
                }
            }

            if (npc._onCreate != null)
                npc._onCreate();

            if (onRegistered != null)
                onRegistered(npc);

            return true;
        }
        
        public void RegisterHandler(short messageType, NetworkMessageDelegate handler)
        {
            NetworkServer.RegisterHandler(messageType, handler);
        }

        public void UnregisterHandler(short messageType, NetworkMessageDelegate handler)
        {
            NetworkServer.UnregisterHandler(messageType);
        }

        public bool Move(short index, int roomIndex, Action<Network.Node> add, Action<Network.Node> remove)
        {
            if (roomIndex < 0)
                return false;

            if (__nodes == null || __rooms == null)
                return false;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return false;

            if (node.roomIndex == roomIndex)
                return false;

            Room sourceRoom;
            if (!__rooms.TryGetValue(node.roomIndex, out sourceRoom) || sourceRoom == null)
                return false;

            Network.Node target = sourceRoom.Get(node.playerIndex);
            if (target == null)
                return false;

            if (!sourceRoom.Delete(node.playerIndex))
                return false;
            
            Room destinationRoom;
            if (!__rooms.TryGetValue(roomIndex, out destinationRoom) || destinationRoom == null)
            {
                destinationRoom = new Room(_GetNeighborRoomIndices(roomIndex));

                __rooms.Insert(roomIndex, destinationRoom);
            }
            
            IEnumerable<int> sourceNeighborRoomIndices = sourceRoom.neighborRoomIndices, destinationNeighborRoomIndices = destinationRoom.neighborRoomIndices;
            if(sourceNeighborRoomIndices != null)
            {
                bool isContains;
                Node temp;
                Network.Node instance;
                if (destinationRoom.count > 0)
                {
                    isContains = false;
                    foreach (int sourceNeighborRoomIndex in sourceNeighborRoomIndices)
                    {
                        if (roomIndex == sourceNeighborRoomIndex)
                        {
                            isContains = true;

                            break;
                        }
                    }
                    
                    if (!isContains)
                    {
                        NetworkWriter writer;
                        foreach (KeyValuePair<int, Network.Node> pair in destinationRoom)
                        {
                            instance = pair.Value;
                            if (instance == null)
                                continue;

                            if (__nodes.TryGetValue(instance._index, out temp) && temp.connectionId >= 0)
                            {
                                writer = __GetWriter();
                                writer.Write(target._type);
                                writer.Write(false);
                                NetworkServer.SendToClient(temp.connectionId, (short)HostMessageType.Register, __GetMessage(index, writer.Position, writer.AsArray()));
                            }

                            if (node.connectionId >= 0)
                            {
                                writer = __GetWriter();
                                writer.Write(instance._type);
                                writer.Write(false);
                                NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Register, __GetMessage(instance._index, writer.Position, writer.AsArray()));
                            }

                            if (add != null)
                                add(instance);
                        }
                    }
                }

                Room room;
                foreach (int sourceNeighborRoomIndex in sourceNeighborRoomIndices)
                {
                    isContains = sourceNeighborRoomIndex == roomIndex;
                    if (!isContains && destinationNeighborRoomIndices != null)
                    {
                        foreach(int destinationNeighborRoomIndex in destinationNeighborRoomIndices)
                        {
                            if (sourceNeighborRoomIndex == destinationNeighborRoomIndex)
                            {
                                isContains = true;

                                break;
                            }
                        }
                    }

                    if(!isContains)
                    {
                        if (__rooms.TryGetValue(sourceNeighborRoomIndex, out room) && room != null)
                        {
                            room.Send((short)HostMessageType.Unregister, __GetMessage(index, 0, null), null);

                            if(room.count > 0 && (node.connectionId >= 0 || remove != null))
                            {
                                foreach (KeyValuePair<int, Network.Node> pair in room)
                                {
                                    instance = pair.Value;
                                    if (instance == null)
                                        continue;
                                    
                                    if (node.connectionId >= 0)
                                        NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Unregister, __GetMessage(instance._index, 0, null));

                                    if (remove != null)
                                        remove(instance);
                                }
                            }
                        }
                    }
                }
            }

            if(destinationNeighborRoomIndices != null)
            {
                bool isContains = false;
                foreach (int destinationNeighborRoomIndex in destinationNeighborRoomIndices)
                {
                    if (node.roomIndex == destinationNeighborRoomIndex)
                    {
                        isContains = true;

                        break;
                    }
                }


                Node temp;
                Network.Node instance;
                if (!isContains)
                {
                    sourceRoom.Send((short)HostMessageType.Unregister, __GetMessage(index, 0, null), null);

                    if (sourceRoom.count > 0 && (node.connectionId >= 0 || remove != null))
                    {
                        foreach (KeyValuePair<int, Network.Node> pair in sourceRoom)
                        {
                            instance = pair.Value;
                            if (instance == null)
                                continue;
                            
                            if (node.connectionId >= 0)
                                NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Unregister, __GetMessage(instance._index, 0, null));
                            
                            if (remove != null)
                                remove(instance);
                        }
                    }
                }
                
                Room room;
                NetworkWriter writer;
                foreach (int destinationNeighborRoomIndex in destinationNeighborRoomIndices)
                {
                    isContains = destinationNeighborRoomIndex == node.roomIndex;
                    if (!isContains && sourceNeighborRoomIndices != null)
                    {
                        foreach (int sourceNeighborRoomIndex in sourceNeighborRoomIndices)
                        {
                            if (sourceNeighborRoomIndex == destinationNeighborRoomIndex)
                            {
                                isContains = true;

                                break;
                            }
                        }
                    }

                    if (!isContains)
                    {
                        if (__rooms.TryGetValue(destinationNeighborRoomIndex, out room) && room != null && room.count > 0)
                        {
                            foreach (KeyValuePair<int, Network.Node> pair in room)
                            {
                                instance = pair.Value;
                                if (instance == null)
                                    continue;

                                if(__nodes.TryGetValue(instance._index, out temp) && temp.connectionId >= 0)
                                {
                                    writer = __GetWriter();
                                    writer.Write(target._type);
                                    writer.Write(false);
                                    NetworkServer.SendToClient(temp.connectionId, (short)HostMessageType.Register, __GetMessage(index, writer.Position, writer.AsArray()));
                                }

                                if (node.connectionId >= 0)
                                {
                                    writer = __GetWriter();
                                    writer.Write(instance._type);
                                    writer.Write(false);
                                    NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Register, __GetMessage(instance._index, writer.Position, writer.AsArray()));
                                }

                                if (add != null)
                                    add(instance);
                            }
                        }
                    }
                }
            }

            node.roomIndex = roomIndex;
            node.playerIndex = destinationRoom.nextIndex;

            destinationRoom.Set(target, node.playerIndex);
            
            __nodes.Insert(index, node);

            return true;
        }

        public bool Replace(short index, short type)
        {
            if (__nodes == null || __rooms == null)
                return false;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return false;
            
            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return false;

            Network.Node target = room.Get(node.playerIndex);
            if (target == null)
                return false;

            target._type = type;

            HostMessage message = __GetMessage(index, 0, null);
            room.Send((short)HostMessageType.Unregister, message, null);

            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                        room.Send((short)HostMessageType.Unregister, message, null);
                }
            }

            NetworkWriter writer = __GetWriter();
            if (node.connectionId < 0)
            {
                writer.Write(type);
                writer.Write(false);
                message.count = writer.Position;
                message.bytes = writer.AsArray();
                room.Send((short)HostMessageType.Register, message, null);
            }
            else
            {
                writer.Write(type);
                writer.Write(true);
                message.count = writer.Position;
                message.bytes = writer.AsArray();
                NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Register, message);

                writer.SeekZero();
                writer.Write(type);
                writer.Write(false);
                message.count = writer.Position;
                message.bytes = writer.AsArray();
                Network.Node temp;
                foreach (KeyValuePair<int, Network.Node> pair in room)
                {
                    temp = pair.Value;
                    if (temp != target && temp is Network.Node && __nodes.TryGetValue(temp._index, out node) && node.connectionId >= 0)
                        NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Register, message);
                }
            }
            
            if (neighborRoomIndices != null)
            {
                Room neighborRoom;
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out neighborRoom) && neighborRoom != null)
                        neighborRoom.Send((short)HostMessageType.Register, message, null);
                }
            }

            return true;
        }

        public void Send(int connectionId, short index, short handle, byte[] bytes, int count)
        {
            if (__nodes == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(bytes, count);

            NetworkServer.SendToClient(connectionId, (short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }
        
        public void Send(int connectionId, short index, short handle, MessageBase message)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(message);

            NetworkServer.SendToClient(connectionId, (short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }
        
        public void Send(int connectionId, short messageType, MessageBase message)
        {
            NetworkServer.SendToClient(connectionId, messageType, message);
        }

        public void Send(short index, short handle, byte[] bytes, int count)
        {
            if (__nodes == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(bytes, count);

            NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }

        public void Send(short index, short handle, MessageBase message)
        {
            if (__nodes == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(message);

            NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }

        public void SendToOthers(short index, short handle, byte[] bytes, int count, Predicate<Network.Node> predicate)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);

            if (bytes != null)
                writer.Write(bytes, count);

            Network.Node temp;
            HostMessage message = __GetMessage(index, writer.Position, writer.AsArray());
            foreach(KeyValuePair<int, Network.Node> pair in room)
            {
                temp = pair.Value;
                if (temp is Network.Node &&
                    temp._index != index && 
                    (predicate == null || predicate(temp)) && 
                    __nodes.TryGetValue(index, out node) &&
                    node.connectionId >= 0)
                    NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, message);
            }
            
            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                        room.Send((short)HostMessageType.Rpc, message, predicate);
                }
            }
        }

        public void SendToOthers(short index, short handle, MessageBase message, Predicate<Network.Node> predicate)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            if(message != null)
                writer.Write(message);

            Network.Node temp;
            HostMessage hostMessage = __GetMessage(index, writer.Position, writer.AsArray());
            foreach (KeyValuePair<int, Network.Node> pair in room)
            {
                temp = pair.Value;
                if (temp is Network.Node &&
                    temp._index != index &&
                    __nodes.TryGetValue(index, out node) &&
                    node.connectionId >= 0 && 
                    (predicate == null || predicate(temp)))
                    NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, hostMessage);
            }
            
            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                        room.Send((short)HostMessageType.Rpc, hostMessage, predicate);
                }
            }
        }

        public void SendToOthers(short index, short handle, byte[] bytes, int count)
        {
            SendToOthers(index, handle, bytes, count, null);
        }

        public void SendToOthers(short index, short handle, MessageBase message)
        {
            SendToOthers(index, handle, message, null);
        }

        public void Rpc(short index, short handle, byte[] bytes, int count, Predicate<Network.Node> predicate)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);

            if(bytes != null)
                writer.Write(bytes, count);

            HostMessage message = __GetMessage(index, writer.Position, writer.AsArray());
            room.Send((short)HostMessageType.Rpc, message, predicate);

            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                        room.Send((short)HostMessageType.Rpc, message, predicate);
                }
            }
        }

        public void Rpc(short index, short handle, byte[] bytes, int count)
        {
            Rpc(index, handle, bytes, count, null);
        }

        public void Rpc(short index, short handle, MessageBase message, Predicate<Network.Node> predicate)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(message);

            HostMessage temp = __GetMessage(index, writer.Position, writer.AsArray());
            room.Send((short)HostMessageType.Rpc, temp, predicate);

            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                        room.Send((short)HostMessageType.Rpc, temp, predicate);
                }
            }
        }

        public void Rpc(short index, short handle, MessageBase message)
        {
            Rpc(index, handle, message, null);
        }

        public void Rpc(int playerIndex, short index, short handle, MessageBase message)
        {
            if (__nodes == null || __rooms == null)
                return;

            Node node;
            if (!__nodes.TryGetValue(index, out node))
                return;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return;

            Network.Node player = room.Get(playerIndex);
            if (player == null)
                return;

            if (!__nodes.TryGetValue(player._index, out node))
                return;

            NetworkWriter writer = __GetWriter();
            writer.Write(handle);
            writer.Write(message);

            NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, __GetMessage(index, writer.Position, writer.AsArray()));
        }
        
        protected virtual IEnumerable<int> _GetNeighborRoomIndices(int roomIndex)
        {
            return null;
        }

        protected virtual bool _Register(NetworkReader reader, int connectionId, out short type, out int roomIndex, out int playerIndex)
        {
            type = 0;
            roomIndex = 0;
            playerIndex = GetRoomNextIndex(roomIndex);

            return true;
        }

        protected virtual bool _Unregister(NetworkReader reader, int connectionId, short index)
        {
            return true;
        }
        
        private bool __Unregister(NetworkReader reader, int connectionId, short index)
        {
            bool result = true;
            if (__nodes == null || __rooms == null)
                result = false;
            else
            {
                Node node;
                if (__nodes.TryGetValue(index, out node))
                {
                    Room room;
                    if (__rooms.TryGetValue(node.roomIndex, out room))
                    {
                        Network.Node player = room == null ? null : room.Get(node.playerIndex);
                        if (onUnregistered != null)
                            onUnregistered(player);

                        if (player != null)
                        {
                            if (player._onDestroy != null)
                                player._onDestroy();
                        }

                        if (__nodeIndices != null)
                        {
                            Pool<int> indices;
                            if (__nodeIndices.TryGetValue(node.connectionId, out indices) && indices != null)
                            {
                                if (indices.RemoveAt(node.connectionIndex))
                                {
                                    if (indices.count < 1)
                                        __nodeIndices.Remove(node.connectionId);
                                }
                                else
                                    result = false;
                            }
                        }

                        if (_Unregister(reader, connectionId, index))
                        {
                            if (player != null)
                                Destroy(player.gameObject);

                            result = room != null && room.Delete(node.playerIndex) && result;


                            HostMessage message = __GetMessage(index, 0, null);
                            if (room == null || room.count < 1)
                                __rooms.RemoveAt(node.roomIndex);
                            else
                                room.Send((short)HostMessageType.Unregister, message, null);

                            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
                            if (neighborRoomIndices != null)
                            {
                                foreach (int neighborRoomIndex in neighborRoomIndices)
                                {
                                    if (__rooms.TryGetValue(neighborRoomIndex, out room) && room != null)
                                        room.Send((short)HostMessageType.Unregister, message, null);
                                }
                            }

                            result = __nodes.RemoveAt(index) && result;
                        }
                        else
                        {
                            node.connectionId = -1;
                            node.connectionIndex = -1;

                            __nodes.Insert(index, node);
                        }
                    }
                    else
                        result = false;
                }
                else
                    result = false;
            }

            return result;
        }
        
        private void __OnRegistered(NetworkMessage message)
        {
            NetworkConnection connection = message == null ? null : message.conn;
#if DEBUG
            if (connection == null)
                throw new InvalidOperationException();
#endif

            short type;
            Node node;
            node.connectionId = connection.connectionId;
            if (!_Register(message.reader, node.connectionId, out type, out node.roomIndex, out node.playerIndex))
            {
                connection.Send((short)HostMessageType.Register, __GetMessage(-1, 0, null));

                Debug.LogError("Register Fail.");

                return;
            }


            if (__rooms == null)
                __rooms = new Pool<Room>();

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
            {
                room = new Room(_GetNeighborRoomIndices(node.roomIndex));

                __rooms.Insert(node.roomIndex, room);
            }

            Network.Node player = room.Get(node.playerIndex);
            if (player == null)
            {
                int numPrefabs = prefabs == null ? 0 : prefabs.Length;
                player = numPrefabs > type ? Instantiate(prefabs[type]) : (numPrefabs == 1 ? Instantiate(prefabs[0]) : null);
                if (player == null)
                {
                    connection.Send((short)HostMessageType.Register, __GetMessage(-1, 0, null));

                    Debug.LogError("Register Fail: Instantiate Error.");

                    return;
                }
                
                if (__nodes == null)
                    __nodes = new Pool<Node>();

                player._index = (short)__nodes.nextIndex;
                
                room.Set(player, node.playerIndex);

                if (__nodeIndices == null)
                    __nodeIndices = new Dictionary<int, Pool<int>>();

                Pool<int> indices;
                if (!__nodeIndices.TryGetValue(node.connectionId, out indices) || indices == null)
                {
                    indices = new Pool<int>();
                    __nodeIndices[node.connectionId] = indices;
                }

                node.connectionIndex = indices.nextIndex;

                indices.Add(__nodes.Add(node));
            }
            else
            {
                Node temp;
                if (__nodes == null || !__nodes.TryGetValue(player._index, out temp) || temp.connectionId >= 0)
                {
                    connection.Send((short)HostMessageType.Register, __GetMessage(-1, 0, null));

                    Debug.LogError("Register Fail: Index Conflict.");

                    return;
                }

                if (__nodeIndices == null)
                    __nodeIndices = new Dictionary<int, Pool<int>>();

                Pool<int> indices;
                if (!__nodeIndices.TryGetValue(node.connectionId, out indices) || indices == null)
                {
                    indices = new Pool<int>();
                    __nodeIndices[node.connectionId] = indices;
                }

                node.connectionIndex = indices.nextIndex;

                __nodes.Insert(player._index, node);

                indices.Add(player._index);
            }

            player._isLocalPlayer = false;
            player._type = type;

            player._host = this;

            NetworkWriter writer = __GetWriter();
            writer.Write(type);
            writer.Write(true);
            HostMessage hostMessage = __GetMessage(player._index, writer.Position, writer.AsArray());
            connection.Send((short)HostMessageType.Register, hostMessage);

            writer.SeekZero();
            writer.Write(type);
            writer.Write(false);
            hostMessage.count = writer.Position;
            hostMessage.bytes = writer.AsArray();
            Network.Node instance;
            foreach (KeyValuePair<int, Network.Node> pair in room)
            {
                instance = pair.Value;
                if (instance == null || instance == player)
                    continue;

                if (!GetNode(instance._index, out node))
                    continue;

                if (node.connectionId >= 0)
                    NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Register, hostMessage);
            }
            
            IEnumerable<int> neighborRoomIndices = room.neighborRoomIndices;
            if (neighborRoomIndices != null)
            {
                Room neighborRoom;
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out neighborRoom) && neighborRoom != null)
                        neighborRoom.Send((short)HostMessageType.Register, hostMessage, null);
                }
            }

            if (room.count > 0)
            {
                foreach (KeyValuePair<int, Network.Node> pair in room)
                {
                    instance = pair.Value;
                    if (instance == null || instance == player)
                        continue;

                    hostMessage.index = instance._index;

                    writer.SeekZero();
                    writer.Write(instance._type);
                    writer.Write(false);
                    hostMessage.count = writer.Position;
                    hostMessage.bytes = writer.AsArray();

                    connection.Send((short)HostMessageType.Register, hostMessage);
                }
            }

            if (neighborRoomIndices != null)
            {
                Room neighborRoom;
                foreach (int neighborRoomIndex in neighborRoomIndices)
                {
                    if (__rooms.TryGetValue(neighborRoomIndex, out neighborRoom) && neighborRoom != null && neighborRoom.count > 0)
                    {
                        foreach (KeyValuePair<int, Network.Node> pair in neighborRoom)
                        {
                            instance = pair.Value;
                            if (instance == null)
                                continue;

                            hostMessage.index = instance._index;
                            
                            writer.SeekZero();
                            writer.Write(instance._type);
                            writer.Write(false);
                            hostMessage.count = writer.Position;
                            hostMessage.bytes = writer.AsArray();

                            connection.Send((short)HostMessageType.Register, hostMessage);
                        }
                    }
                }
            }

            if (player._onCreate != null)
                player._onCreate();

            if (onRegistered != null)
                onRegistered(player);
        }

        private void __OnUnregistered(NetworkMessage message)
        {
            NetworkConnection connection = message == null ? null : message.conn;
#if DEBUG
            if (connection == null)
                throw new InvalidOperationException();
#endif

            HostMessage temp = message.ReadMessage<HostMessage>();
            if (temp == null || 
                !__Unregister(temp.count > 0 ? new NetworkReader(temp.bytes) : null, connection.connectionId, temp.index))
            {
                Debug.LogError("Unregister Fail.");

                if (temp != null)
                    temp.index = -1;
            }

            connection.Send((short)HostMessageType.Unregister, temp);
        }

        private void __OnRpc(NetworkMessage message)
        {
            NetworkConnection connection = message == null ? null : message.conn;
#if DEBUG
            if (connection == null)
                throw new InvalidOperationException();
#endif

            HostMessage temp = message.ReadMessage<HostMessage>();
            Node node;
            Room room;
            if (temp == null ||
                __nodes == null ||
                __rooms == null ||
                !__nodes.TryGetValue(temp.index, out node) ||
                !__rooms.TryGetValue(node.roomIndex, out room) ||
                room == null)
            {
                Debug.LogError("Rpc Fail.");

                return;
            }

            Network.Node player = room.Get(node.playerIndex);
            if (player == null || temp.bytes == null || temp.bytes.Length < 2)
            {
                Debug.LogError("Rpc Fail.");

                return;
            }

            NetworkReader reader = new NetworkReader(temp.bytes);

            player.InvokeHandler(reader.ReadInt16(), connection.connectionId, reader);
        }

        private void __OnDisconnect(NetworkMessage message)
        {
            if (onDisconnect != null)
                onDisconnect(message);

            NetworkConnection connection = message == null ? null : message.conn;
#if DEBUG
            if (connection == null)
                throw new InvalidOperationException();
#endif
            Pool<int> indices;
            if (__nodeIndices == null || !__nodeIndices.TryGetValue(connection.connectionId, out indices) || indices == null)
                return;

            int[] temp = indices.ToArray();
            foreach (short index in temp)
                __Unregister(null, -1, index);
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
    }
}