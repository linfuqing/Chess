﻿using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System;
using System.Collections.Generic;

namespace ZG.Network.Lobby
{
    public class Server : Network.Server, IHost
    {
        public event Action<int, int> onReady;
        public event Action<int, int> onNotReady;

        private Pool<Room> __rooms;

        public int GetRoomCount(int index)
        {
            if (__rooms == null)
                return 0;

            Room room;
            if (!__rooms.TryGetValue(index, out room) || room == null)
                return 0;

            return room.count;
        }

        public void Ready(short index)
        {
            Network.Node player = Get(index);

            Node node;
            if (GetNode(index, out node))
            {
                Room room;
                if (__rooms.TryGetValue(node.roomIndex, out room) && room != null)
                {
                    int count = room.count + 1;
                    if (room.Ready(node.playerIndex))
                    {
                        if (player != null)
                            player.Rpc((short)HostMessageHandle.Ready, new ReadyMessage());

                        if (count == room.count)
                        {
                            if (onReady != null)
                                onReady(node.roomIndex, count);
                        }
                    }
                    else
                    {
                        Debug.LogError("Ready Fail:" + index);

                        return;
                    }
                }
            }

            Lobby.Node temp = player as Lobby.Node;
            if (temp != null)
            {
                ++temp._count;

                if (temp._onReady != null)
                    temp._onReady();
            }
        }

        public void NotReady(short index)
        {
            Network.Node player = Get(index);

            Node node;
            if (GetNode(index, out node))
            {
                Room room;
                if (__rooms.TryGetValue(node.roomIndex, out room) && room != null)
                {
                    int count = room.count - 1;
                    if (room.NotReady(node.playerIndex))
                    {
                        if (player != null)
                            player.Rpc((short)HostMessageHandle.NotReady, new NotReadyMessage());

                        if (count == room.count)
                        {
                            if (onReady != null)
                                onReady(node.roomIndex, count);
                        }
                    }
                    else
                    {
                        Debug.LogError("Not Ready Fail:" + index);

                        return;
                    }
                }
            }

            Lobby.Node temp = player as Lobby.Node;
            if (temp != null)
            {
                --temp._count;

                if (temp._onNotReady != null)
                    temp._onNotReady();
            }
        }

        public void Awake()
        {
            onRegistered += __OnRegistered;
        }

        protected virtual bool _GetRoomInfo(NetworkReader reader, int connectionId, int roomIndex, out int length)
        {
            length = 0;

            return true;
        }

        protected virtual bool _GetPlayerInfo(NetworkReader reader, int connectionId, out short type, out int roomIndex, out int playerIndex)
        {
            type = 0;
            roomIndex = 0;
            playerIndex = 0;
            if (__rooms != null)
            {
                foreach (KeyValuePair<int, Room> pair in ((IEnumerable<KeyValuePair<int, Room>>)__rooms))
                {
                    roomIndex = pair.Key;
                    if (GetRoomCount(roomIndex) < 1)
                    {
                        playerIndex = GetRoomNextIndex(roomIndex);

                        return true;
                    }
                }

                roomIndex = __rooms.nextIndex;
            }

            return true;
        }

        protected override bool _Register(NetworkReader reader, int connectionId, out short type, out int roomIndex, out int playerIndex)
        {
            if (!_GetPlayerInfo(reader, connectionId, out type, out roomIndex, out playerIndex))
                return false;

            if (__rooms == null)
                __rooms = new Pool<Room>();

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room))
            {
                int length;
                if (_GetRoomInfo(reader, connectionId, roomIndex, out length))
                {
                    room = new Room(length, 0);

                    __rooms.Insert(playerIndex, room);
                }
                else
                    return false;
            }

            if (connectionId >= 0)
                Send(connectionId, (short)HostMessageType.RoomInfo, new RoomMessage((short)nextNodeIndex, (short)playerIndex, (short)roomIndex, (short)room.length, (short)room.count));
            
            return true;
        }

        protected override bool _Unregister(int index)
        {
            Node node;
            if (!GetNode(index, out node))
                return false;

            if (__rooms == null)
                return false;

            Room room;
            if (!__rooms.TryGetValue(node.roomIndex, out room) || room == null)
                return false;

            Lobby.Node temp = Get(index) as Lobby.Node;
            while (room.NotReady(index))
            {
                if (temp != null)
                {
                    --temp._count;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }
            }

            return true;
        }

        private void __OnRegistered(Network.Node player)
        {
            if (player == null)
                return;

            short index = player.index;
            player.RegisterHandler((short)HostMessageHandle.Ready, delegate (NetworkReader reader)
            {
                Ready(index);
            });

            player.RegisterHandler((short)HostMessageHandle.NotReady, delegate(NetworkReader reader)
            {
                NotReady(index);
            });

            Node node;
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
                    int i;
                    foreach (KeyValuePair<int, Network.Node> pair in room)
                    {
                        instance = pair.Value as Lobby.Node;
                        if (instance != null)
                        {
                            hostMessage.index = instance.index;
                            for (i = 0; i < instance._count; ++i)
                                NetworkServer.SendToClient(node.connectionId, (short)Network.HostMessageType.Rpc, hostMessage);
                        }
                    }

                    IEnumerable<int> neighborRoomIndices = GetNeighborRoomIndices(node.roomIndex);
                    if(neighborRoomIndices != null)
                    {
                        foreach(int neighborRoomIndex in neighborRoomIndices)
                        {
                            room = GetRoom(neighborRoomIndex);
                            if (room == null)
                                continue;

                            foreach (KeyValuePair<int, Network.Node> pair in room)
                            {
                                instance = pair.Value as Lobby.Node;
                                if (instance != null)
                                {
                                    hostMessage.index = instance.index;
                                    for (i = 0; i < instance._count; ++i)
                                        NetworkServer.SendToClient(node.connectionId, (short)Network.HostMessageType.Rpc, hostMessage);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}