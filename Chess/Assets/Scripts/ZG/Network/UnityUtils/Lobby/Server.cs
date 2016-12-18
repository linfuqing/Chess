using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System;
using System.Collections.Generic;

namespace ZG.Network.Lobby
{
    public class Server : Network.Server
    {
        private struct Room
        {
            public int readyCount;
            public int loadCount;
            public bool isLoaded;
        }

        public event Action<int> onReady;
        public event Action<int> onNotReady;
        public event Action<int> onLoad;
        public event Action<int> onUnload;

        public int minPlayers;
        public int maxPlayers;

        private Pool<Room> __rooms;

        public bool IsRoomLoaded(int index)
        {
            if (__rooms == null)
                return false;

            Room room;
            if (!__rooms.TryGetValue(index, out room))
                return false;

            return room.isLoaded;
        }

        public bool CheckRoom(int index)
        {
            return !IsRoomLoaded(index) && GetCount(index) < maxPlayers;
        }

        public void Awake()
        {
            onRegistered += __OnRegistered;
            onUnregistered += __OnUnregistered;
        }

        protected virtual bool _GetInfo(NetworkReader reader, int connectionId, out short type, out int roomIndex)
        {
            type = 0;
            roomIndex = 0;
            if (__rooms != null)
            {
                foreach (KeyValuePair<int, Room> pair in ((IEnumerable<KeyValuePair<int, Room>>)__rooms))
                {
                    roomIndex = pair.Key;
                    if (CheckRoom(roomIndex))
                        return true;
                }

                roomIndex = __rooms.nextIndex;
            }

            return true;
        }

        protected override bool _Register(NetworkReader reader, int connectionId, out short type, out int roomIndex)
        {
            if (!_GetInfo(reader, connectionId, out type, out roomIndex))
                return false;

            if (__rooms == null)
                __rooms = new Pool<Room>();

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room))
            {
                room = new Room();

                __rooms.Insert(roomIndex, room);
            }

            return true;
        }

        private void __OnRegistered(Network.Node player)
        {
            if (player == null)
                return;

            Lobby.Node temp = player as Lobby.Node;
            Node node;
            player.RegisterHandler((short)HostMessageHandle.Ready, delegate (NetworkReader reader)
            {
                if (player != null)
                {
                    player.Rpc((short)HostMessageHandle.Ready, new ReadyMessage());
                    
                    if (GetNode(player.index, out node))
                    {
                        Room room;
                        if (__rooms.TryGetValue(node.roomIndex, out room))
                        {
                            ++room.readyCount;
                            if (room.readyCount >= minPlayers)
                            {
                                room.isLoaded = true;

                                if (onReady != null)
                                    onReady(node.roomIndex);
                            }

                            __rooms.Insert(node.roomIndex, room);
                        }
                    }
                }
                
                if (temp != null)
                {
                    if (temp._isReady)
                    {
                        Debug.LogError("Ready Fail.");

                        return;
                    }

                    temp._isReady = true;

                    if (temp._onReady != null)
                        temp._onReady();
                }
            });

            player.RegisterHandler((short)HostMessageHandle.NotReady, delegate(NetworkReader reader)
            {
                if (player != null)
                {
                    player.Rpc((short)HostMessageHandle.NotReady, new ReadyMessage());
                    
                    if (GetNode(player.index, out node))
                    {
                        Room room;
                        if (__rooms.TryGetValue(node.roomIndex, out room))
                        {
                            --room.readyCount;
                            if (room.readyCount < 1)
                            {
                                room.isLoaded = false;

                                if (onNotReady != null)
                                    onNotReady(node.roomIndex);
                            }

                            __rooms.Insert(node.roomIndex, room);
                        }
                    }
                }
                
                if (temp != null)
                {
                    if (!temp._isReady)
                    {
                        Debug.LogError("Not Ready Fail.");

                        return;
                    }

                    temp._isReady = false;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }
            });

            player.RegisterHandler((short)HostMessageHandle.Load, delegate(NetworkReader reader)
            {
                if (temp != null)
                {
                    if (temp._isLoad)
                    {
                        Debug.LogError("Load Fail.");

                        return;
                    }

                    temp._isLoad = true;

                    if (temp._onLoad != null)
                        temp._onLoad();
                }

                if (player != null)
                {
                    player.Rpc((short)HostMessageHandle.Load, new EmptyMessage());
                    
                    if (GetNode(player.index, out node))
                    {
                        Room room;
                        if (__rooms.TryGetValue(node.roomIndex, out room))
                        {
                            ++room.loadCount;

                            __rooms.Insert(node.roomIndex, room);

                            if (room.loadCount >= GetCount(node.roomIndex))
                            {
                                if (onLoad != null)
                                    onLoad(node.roomIndex);
                            }
                        }
                    }
                }
            });

            player.RegisterHandler((short)HostMessageHandle.Unload, delegate (NetworkReader reader)
            {
                if (temp != null)
                {
                    if (!temp._isLoad)
                    {
                        Debug.LogError("Unload Fail.");

                        return;
                    }

                    temp._isLoad = false;

                    if (temp._onUnload != null)
                        temp._onUnload();
                }

                if (player != null)
                {
                    player.Rpc((short)HostMessageHandle.Unload, new EmptyMessage());
                    
                    if (GetNode(player.index, out node))
                    {
                        Room room;
                        if (__rooms.TryGetValue(node.roomIndex, out room))
                        {
                            --room.loadCount;

                            __rooms.Insert(node.roomIndex, room);

                            if (room.loadCount < 1)
                            {
                                if (onUnload != null)
                                    onUnload(node.roomIndex);
                            }
                        }
                    }
                }
            });

            if (player is Lobby.Node)
            {
                Action onStart = ((Lobby.Node)player)._onStart;
                if (onStart != null)
                    onStart();
            }

            if (GetNode(player.index, out node))
            {
                IEnumerable<KeyValuePair<int, Network.Node>> room = GetRoom(node.roomIndex);
                if (room != null)
                {
                    NetworkWriter writer = new NetworkWriter();
                    writer.Write((short)HostMessageHandle.Ready);
                    writer.Write(new ReadyMessage());
                    HostMessage hostMessage = new HostMessage(-1, writer.Position, writer.AsArray());
                    Lobby.Node instance;
                    foreach (KeyValuePair<int, Network.Node> pair in room)
                    {
                        instance = pair.Value as Lobby.Node;
                        if ((instance is Lobby.Node) && instance._isReady)
                        {
                            hostMessage.index = instance.index;
                            NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, hostMessage);
                        }
                    }

                    writer.SeekZero();
                    writer.Write((short)HostMessageHandle.Load);
                    writer.Write(new EmptyMessage());
                    hostMessage.bytes = writer.ToArray();
                    foreach (KeyValuePair<int, Network.Node> pair in room)
                    {
                        instance = pair.Value as Lobby.Node;
                        if ((instance is Lobby.Node) && instance._isLoad)
                        {
                            hostMessage.index = instance.index;
                            NetworkServer.SendToClient(node.connectionId, (short)HostMessageType.Rpc, hostMessage);
                        }
                    }
                }
            }
        }

        private void __OnUnregistered(Network.Node player)
        {
            if (!(player is Network.Node))
                return;

            int index = player.index;
            Node node;
            if (GetNode(index, out node))
            {
                //bool isReady = false, isLoad = false;
                if (__rooms != null)
                {
                    Lobby.Node temp;
                    Room room;
                    if (__rooms.TryGetValue(node.roomIndex, out room))
                    {
                        if (player is Lobby.Node)
                        {
                            temp = (Lobby.Node)player;
                            if (temp._isReady)
                            {
                                temp._isReady = false;

                                if (temp._onNotReady != null)
                                    temp._onNotReady();

                                --room.readyCount;

                                if (room.readyCount < 1)
                                    room.isLoaded = false;
                            }

                            if (temp._isLoad)
                            {
                                temp._isLoad = false;

                                if (temp._onUnload != null)
                                    temp._onUnload();

                                --room.loadCount;
                                if (room.loadCount < 1)
                                {
                                    if (onUnload != null)
                                        onUnload(node.roomIndex);
                                }
                            }
                        }

                        __rooms.Insert(node.roomIndex, room);
                    }
                }

                /*if (isReady)
                {
                    IEnumerable<KeyValuePair<int, MasterBehaviour>> room = GetRoom(node.roomIndex);
                    if (room != null)
                    {
                        NetworkWriter writer = new NetworkWriter();
                        writer.Write((short)MasterLobbyMessageHandle.NotReady);
                        writer.Write(new ReadyMessage());
                        MasterMessage masterMessage = new MasterMessage(index, writer.ToArray());
                        MasterBehaviour temp;
                        foreach (KeyValuePair<int, MasterBehaviour> pair in room)
                        {
                            temp = pair.Value;
                            if (temp != behaviour && temp is MasterBehaviour)
                            {
                                if (GetNode(temp.index, out node))
                                    NetworkServer.SendToClient(node.connectionId, (short)MasterMessageType.Rpc, masterMessage);
                            }
                        }
                    }
                }

                if (isLoad)
                {
                    IEnumerable<KeyValuePair<int, MasterBehaviour>> room = GetRoom(node.roomIndex);
                    if (room != null)
                    {
                        NetworkWriter writer = new NetworkWriter();
                        writer.Write((short)MasterLobbyMessageHandle.Unload);
                        writer.Write(new EmptyMessage());
                        MasterMessage masterMessage = new MasterMessage(index, writer.ToArray());
                        MasterBehaviour temp;
                        foreach (KeyValuePair<int, MasterBehaviour> pair in room)
                        {
                            temp = pair.Value;
                            if (temp != behaviour && temp is MasterBehaviour)
                            {
                                if (GetNode(temp.index, out node))
                                    NetworkServer.SendToClient(node.connectionId, (short)MasterMessageType.Rpc, masterMessage);
                            }
                        }
                    }
                }*/
            }
        }
    }
}