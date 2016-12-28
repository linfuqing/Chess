using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ZG.Network.Lobby
{
    public class Client : Network.Client
    {
        public event Action<int, int> onReady;
        public event Action<int, int> onNotReady;

        private Dictionary<int, int> __roomIndices;
        private Dictionary<int, Room> __rooms;

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
        }

        private void __OnDisconnect(NetworkMessage message)
        {
            __Clear();
        }

        private void __OnUnregistered(Network.Node player)
        {
            if (player == null || __roomIndices == null)
                return;

            int roomIndex;
            if (!__roomIndices.TryGetValue(player.index, out roomIndex))
                return;

            int index = player.index;
            __roomIndices.Remove(index);

            if (__rooms == null)
                return;

            Room room;
            if (!__rooms.TryGetValue(roomIndex, out room) || room == null)
                return;
            
            Node temp = player as Node;
            while(room.NotReady(index))
            {
                if(temp != null)
                {
                    -- temp._count;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }
            }
        }

        private void __OnRegistered(Network.Node player)
        {
            if (player == null)
                return;

            int index = player.index;
            Node temp = player as Node;
            player.RegisterHandler((short)HostMessageHandle.Ready, delegate (NetworkReader reader)
            {
                if(__roomIndices != null && __rooms != null)
                {
                    int roomIndex;
                    if(__roomIndices.TryGetValue(index, out roomIndex))
                    {
                        Room room;
                        if(__rooms.TryGetValue(roomIndex, out room) && room != null)
                        {
                            int count = room.count + 1;
                            if (room.Ready(index))
                            {
                                if (count == room.count)
                                {
                                    if (onReady != null)
                                        onReady(roomIndex, count);
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

            player.RegisterHandler((short)HostMessageHandle.NotReady, delegate (NetworkReader reader)
            {
                if (__roomIndices != null && __rooms != null)
                {
                    int roomIndex;
                    if (__roomIndices.TryGetValue(index, out roomIndex))
                    {
                        Room room;
                        if (__rooms.TryGetValue(roomIndex, out room) && room != null)
                        {
                            int count = room.count - 1;
                            if (room.NotReady(index))
                            {
                                if (count == room.count)
                                {
                                    if (onNotReady != null)
                                        onNotReady(roomIndex, count);
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
            RoomMessage roomMessage = message == null ? null : message.ReadMessage<RoomMessage>();
            if (roomMessage == null)
                return;

            if (__roomIndices == null)
                __roomIndices = new Dictionary<int, int>();

            __roomIndices[roomMessage.index] = roomMessage.roomIndex;

            if (__rooms == null)
                __rooms = new Dictionary<int, Room>();

            Room room;
            if (__rooms.TryGetValue(roomMessage.roomIndex, out room) && room != null)
                room.length = roomMessage.roomLength;
            else
                __rooms.Add(roomMessage.roomIndex, new Room(roomMessage.roomLength));
        }
        
        private void __Clear()
        {
            onDisconnect -= __OnDisconnect;
            onUnregistered -= __OnUnregistered;
            onRegistered -= __OnRegistered;

            if (__roomIndices != null)
                __roomIndices.Clear();

            if (__rooms != null)
                __rooms.Clear();
        }
    }
}