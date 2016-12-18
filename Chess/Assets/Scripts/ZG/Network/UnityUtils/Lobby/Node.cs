using UnityEngine.Networking.NetworkSystem;
using System;

namespace ZG.Network.Lobby
{
    public class Node : Network.Node
    {
        internal Action _onStart;
        internal Action _onReady;
        internal Action _onNotReady;
        internal Action _onLoad;
        internal Action _onUnload;
        internal bool _isReady;
        internal bool _isLoad;

        public event Action onStart
        {
            add
            {
                _onStart += value;
            }

            remove
            {
                _onStart -= value;
            }
        }

        public event Action onReady
        {
            add
            {
                _onReady += value;
            }

            remove
            {
                _onReady -= value;
            }
        }

        public event Action onNotReady
        {
            add
            {
                _onNotReady += value;
            }

            remove
            {
                _onNotReady -= value;
            }
        }

        public event Action onLoad
        {
            add
            {
                _onLoad += value;
            }

            remove
            {
                _onLoad -= value;
            }
        }

        public event Action onUnload
        {
            add
            {
                _onUnload += value;
            }

            remove
            {
                _onUnload -= value;
            }
        }

        public bool isReady
        {
            get
            {
                return _isReady;
            }
        }

        public void SendReadyMessage()
        {
            Rpc(_isReady ? (short)HostMessageHandle.NotReady : (short)HostMessageHandle.Ready, new ReadyMessage());
        }

        public override bool CopyFrom(Network.Node node)
        {
            if (!base.CopyFrom(node))
                return false;

            Node temp = node as Node;
            if (temp == null)
                return false;
            
            _isReady = temp._isReady;
            _isLoad = temp._isLoad;

            return true;
        }
    }
}