using UnityEngine;
using UnityEngine.Networking;
using System;

namespace ZG.Network
{
    public class Node : MonoBehaviour
    {
        internal bool _isLocalPlayer;
        internal short _type;
        internal short _index;
        internal IHost _host;

        internal Action _onCreate;
        internal Action _onDestroy;
        private System.Collections.Generic.Dictionary<short, Action<int, NetworkReader>> __handlers;

        public bool isLocalPlayer
        {
            get
            {
                return _isLocalPlayer;
            }
        }

        public short type
        {
            get
            {
                return _type;
            }
        }

        public short index
        {
            get
            {
                return _index;
            }
        }

        public IHost host
        {
            get
            {
                return _host;
            }
        }

        public event Action onCreate
        {
            add
            {
                _onCreate += value;
            }

            remove
            {
                _onCreate -= value;
            }
        }

        public event Action onDestroy
        {
            add
            {
                _onDestroy += value;
            }

            remove
            {
                _onDestroy -= value;
            }
        }

        public bool InvokeHandler(short handle, int connectionId, NetworkReader reader)
        {
            if (__handlers == null)
                return false;

            Action<int, NetworkReader> action;
            if (!__handlers.TryGetValue(handle, out action) || action == null)
                return false;

            action(connectionId, reader);

            return true;
        }

        public bool UnregisterHandler(short handle)
        {
            return __handlers != null && __handlers.Remove(handle);
        }

        public void RegisterHandler(short handle, Action<int, NetworkReader> action)
        {
            if (__handlers == null)
                __handlers = new System.Collections.Generic.Dictionary<short, Action<int, NetworkReader>>();

            __handlers.Add(handle, action);
        }

        public void RegisterHandler(short handle, Action<NetworkReader> action)
        {
            RegisterHandler(handle, action == null ? (Action<int, NetworkReader>)null : delegate(int connectionId, NetworkReader reader)
            {
                action(reader);
            });
        }

        public void Rpc(short handle, byte[] bytes, int count)
        {
            if (_host == null)
                return;

            _host.Rpc(_index, handle, bytes, count);
        }

        public void Rpc<T>(short handle, T message) where T : MessageBase
        {
            if (_host == null)
                return;

            _host.Rpc(_index, handle, message);
        }

        public virtual bool CopyFrom(Node node)
        {
            if (!(node is UnityEngine.Object) && node == null)
                return false;

            _isLocalPlayer = node._isLocalPlayer;
            _type = node._type;
            _index = node._index;
            _host = node._host;

            return true;
        }
    }
}