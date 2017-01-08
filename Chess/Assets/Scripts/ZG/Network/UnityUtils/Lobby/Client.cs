using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using System;
using System.Collections.Generic;

namespace ZG.Network.Lobby
{
    public class Client : Network.Client, IHost
    {
        private struct Player
        {
            public int index;
            public int roomIndex;

            public Player(int index, int roomIndex)
            {
                this.index = index;
                this.roomIndex = roomIndex;
            }
        }

        public event Action<int, int> onReady;
        public event Action<int, int> onNotReady;

        private Dictionary<int, Player> __players;
        private Dictionary<int, Room> __rooms;

        public void Ready(short index)
        {
            Rpc(index, (short)HostMessageHandle.Ready, new ReadyMessage());
        }

        public void NotReady(short index)
        {
            Rpc(index, (short)HostMessageHandle.NotReady, new NotReadyMessage());
        }

        public override void Shutdown()
        {
            __Clear();

            base.Shutdown();
        }

        public new void Create()
        {
            onDisconnect += __OnDisconnect;
            onUnregistered += __OnUnregistered;
            onRegistered += __OnRegistered;

            base.Create();

            RegisterHandler((short)HostMessageType.RoomInfo, __OnRoomInfo);
            RegisterHandler((short)HostMessageType.PlayerInfo, __OnPlayerInfo);
        }

        private void __OnDisconnect(NetworkMessage message)
        {
            __Clear();
        }

        private void __OnUnregistered(Network.Node node)
        {
            if (node == null || __players == null)
                return;


            int index = node.index;
            Player player;
            if (!__players.TryGetValue(index, out player))
                return;

            __players.Remove(index);

            if (__rooms == null)
                return;

            Room room;
            if (!__rooms.TryGetValue(player.roomIndex, out room) || room == null)
                return;
            
            Node temp = node as Node;
            while(room.NotReady(player.index))
            {
                if(temp != null)
                {
                    -- temp._count;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }
            }
        }

        private void __OnRegistered(Network.Node node)
        {
            if (node == null)
                return;

            int index = node.index;
            Node temp = node as Node;
            node.RegisterHandler((short)HostMessageHandle.Ready, delegate (NetworkReader reader)
            {
                if(__players != null && __rooms != null)
                {
                    Player player;
                    if(__players.TryGetValue(index, out player))
                    {
                        Room room;
                        if(__rooms.TryGetValue(player.roomIndex, out room) && room != null)
                        {
                            int count = room.count + 1;
                            if (room.Ready(player.index))
                            {
                                if (count == room.count)
                                {
                                    if (onReady != null)
                                        onReady(player.roomIndex, count);
                                }
                            }
                            else
                            {
                                Debug.LogError("Ready Fail:" + index);

                                return;
                            }
                        }
                    }
                }

                if (temp != null)
                {
                    ++temp._count;

                    if (temp._onReady != null)
                        temp._onReady();
                }
            });

            node.RegisterHandler((short)HostMessageHandle.NotReady, delegate (NetworkReader reader)
            {
                if (__players != null && __rooms != null)
                {
                    Player player;
                    if (__players.TryGetValue(index, out player))
                    {
                        Room room;
                        if (__rooms.TryGetValue(player.roomIndex, out room) && room != null)
                        {
                            int count = room.count - 1;
                            if (room.NotReady(player.index))
                            {
                                if (count == room.count)
                                {
                                    if (onNotReady != null)
                                        onNotReady(player.roomIndex, count);
                                }
                            }
                            else
                            {
                                Debug.LogError("Not Ready Fail:" + index);

                                return;
                            }
                        }
                    }
                }

                if (temp != null)
                {
                    --temp._count;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }
            });
        }

        private void __OnRoomInfo(NetworkMessage message)
        {
            RoomInfoMessage roomInfoMessage = message == null ? null : message.ReadMessage<RoomInfoMessage>();
            if (roomInfoMessage == null)
                return;

            if (__rooms == null)
                __rooms = new Dictionary<int, Room>();

            Room room;
            if (__rooms.TryGetValue(roomInfoMessage.index, out room) && room != null)
            {
                room.length = roomInfoMessage.length;
                room.count = roomInfoMessage.count;
            }
            else
                __rooms.Add(roomInfoMessage.index, new Room(roomInfoMessage.length, roomInfoMessage.count));
        }

        private void __OnPlayerInfo(NetworkMessage message)
        {
            PlayerInfoMessage playerInfoMessage = message == null ? null : message.ReadMessage<PlayerInfoMessage>();
            if (playerInfoMessage == null)
                return;
            
            if (__players == null)
                __players = new Dictionary<int, Player>();

            __players[playerInfoMessage.index] = new Player(playerInfoMessage.playerIndex, playerInfoMessage.roomIndex);
        }

        private void __Clear()
        {
            onDisconnect -= __OnDisconnect;
            onUnregistered -= __OnUnregistered;
            onRegistered -= __OnRegistered;

            if (__players != null)
                __players.Clear();

            if (__rooms != null)
                __rooms.Clear();
        }
    }
}