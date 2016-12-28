using UnityEngine.Networking.NetworkSystem;
using System;

namespace ZG.Network.Lobby
{
    public class Node : Network.Node
    {
        internal Action _onReady;
        internal Action _onNotReady;
        internal int _count;
        
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
        
        public int count
        {
            get
            {
                return _count;
            }
        }

        public void Ready()
        {
            IHost host = base.host as IHost;
            if (host == null)
                return;

            host.Ready(index);
        }

        public void NotReady()
        {
            IHost host = base.host as IHost;
            if (host == null)
                return;

            host.NotReady(index);
        }

        public override bool CopyFrom(Network.Node node)
        {
            if (!base.CopyFrom(node))
                return false;

            Node temp = node as Node;
            if (temp == null)
                return false;
            
            _count = temp._count;

            return true;
        }
    }
}