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
        public event Action onReady;
        public event Action onNotReady;
        public event Action onLoad;
        public event Action onUnload;

        public int minPlayers;
        private int __readyCount;
        private int __loadCount;
        private bool __isOnline;

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
        }

        private void __RegisterHandlers(Network.Node player)
        {
            if (player == null)
                return;

            Node temp = player as Node;
            player.RegisterHandler((short)HostMessageHandle.Ready, delegate (NetworkReader reader)
            {
                if (temp != null)
                {
                    if (temp._isReady)
                        return;

                    temp._isReady = true;

                    if (temp._onReady != null)
                        temp._onReady();
                }

                ++__readyCount;
                if (__readyCount >= minPlayers)
                {
                    if (!__isOnline)
                    {
                        __isOnline = true;

                        _Load();
                    }

                    if (onReady != null)
                        onReady();
                }
            });

            player.RegisterHandler((short)HostMessageHandle.NotReady, delegate (NetworkReader reader)
            {
#if DEBUG
                if (__readyCount <= 0)
                    throw new InvalidOperationException();
#endif
                
                if (temp != null)
                {
                    if (!temp._isReady)
                        return;

                    temp._isReady = false;

                    if (temp._onNotReady != null)
                        temp._onNotReady();
                }

                --__readyCount;

                if (__readyCount < 1)
                {
                    if (__isOnline)
                    {
                        __isOnline = false;

                        _Unload();
                    }

                    if (onNotReady != null)
                        onNotReady();
                }
            });

            player.RegisterHandler((short)HostMessageHandle.Load, delegate (NetworkReader reader)
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

                ++__loadCount;
                if (__loadCount >= __readyCount)
                {
                    if (onLoad != null)
                        onLoad();
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

                --__loadCount;
                if (__loadCount < 1)
                {
                    if (onUnload != null)
                        onUnload();
                }
            });
        }

        protected void _Online()
        {
            Network.Node localPlayer = base.localPlayer;
            if (localPlayer is UnityEngine.Object)
                localPlayer.Rpc((short)HostMessageHandle.Load, new EmptyMessage());
        }

        protected void _Offline()
        {
            Network.Node localPlayer = base.localPlayer;
            if (localPlayer is UnityEngine.Object)
                localPlayer.Rpc((short)HostMessageHandle.Unload, new EmptyMessage());
        }

        protected virtual void _Load()
        {
            _Online();
        }

        protected virtual void _Unload()
        {
            _Offline();
        }

        private void __OnDisconnect(NetworkMessage message)
        {
            __Clear();
        }

        private void __OnUnregistered(Network.Node player)
        {
            if (!(player is Node))
                return;

            Node temp = player as Node;
            if (temp._isReady)
            {
                temp._isReady = false;
                if (temp._onNotReady != null)
                    temp._onNotReady();

                --__readyCount;
                if (__readyCount < 1)
                {
                    if (onNotReady != null)
                        onNotReady();

                    if (__isOnline)
                    {
                        __isOnline = false;

                        _Unload();
                    }
                }
            }

            if (temp._isLoad)
            {
                temp._isLoad = false;
                if (temp._onUnload != null)
                    temp._onUnload();

                --__loadCount;
                if (__loadCount < 1)
                {
                    if (onUnload != null)
                        onUnload();
                }
            }
        }

        private void __OnRegistered(Network.Node player)
        {
            __RegisterHandlers(player);

            if (player is Node)
            {
                Action onStart = ((Node)player)._onStart;
                if (onStart != null)
                    onStart();
            }
        }
        
        private void __Clear()
        {
            onDisconnect -= __OnDisconnect;
            onUnregistered -= __OnUnregistered;
            onRegistered -= __OnRegistered;

            __readyCount = 0;
            __loadCount = 0;

            __isOnline = false;
        }
    }
}