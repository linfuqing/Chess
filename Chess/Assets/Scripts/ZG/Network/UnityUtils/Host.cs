using UnityEngine;
using UnityEngine.Networking;

namespace ZG.Network
{
    public enum HostMessageType : short
    {
        Register = 150,
        Unregister,
        Rpc
    }

    public class HostMessage : MessageBase
    {
        public short index;
        public int count;
        public byte[] bytes;

        public HostMessage()
        {

        }

        public HostMessage(short index, int count, byte[] bytes)
        {
            this.index = index;
            this.count = count;
            this.bytes = bytes;
        }

        public override void Deserialize(NetworkReader reader)
        {
            if (reader == null)
                return;

            index = reader.ReadInt16();
            bytes = reader.ReadBytesAndSize();
            count = bytes == null ? 0 : bytes.Length;
        }

        public override void Serialize(NetworkWriter writer)
        {
            if (writer == null)
                return;

            writer.Write(index);
            writer.WriteBytesAndSize(bytes, count);
        }
    }

    public interface IHost
    {
        void Rpc(short index, short handle, byte[] bytes, int count);

        void Rpc(short index, short handle, MessageBase message);
    }

    public class Host : MonoBehaviour, IHost
    {
        public Node server;
        public Node client;

        private Node __server;
        private Node __client;

        public void Create()
        {
            if (server != null)
            {
                if (__server != null)
                    Destroy(__server.gameObject);

                __server = Instantiate(server);
                if (__server != null)
                {
                    __server._isLocalPlayer = true;
                    __server._index = 0;
                    __server._host = this;

                    if (__server._onCreate != null)
                        __server._onCreate();
                }
            }

            if (client != null)
            {
                if (__client != null)
                    Destroy(__client.gameObject);

                __client = Instantiate(client);
                if (__client != null)
                {
                    __client._isLocalPlayer = true;
                    __client._index = 1;
                    __client._host = this;

                    if (__client._onCreate != null)
                        __client._onCreate();
                }
            }
        }

        public void Rpc(short index, short handle, byte[] bytes, int count)
        {
            Node player = index == 0 ? __client : __server;
            if (player == null)
                return;

            NetworkWriter writer = new NetworkWriter();
            writer.Write(bytes, count);

            player.InvokeHandler(handle, -1, new NetworkReader(writer.AsArray()));
        }

        public void Rpc(short index, short handle, MessageBase message)
        {
            Node player = index == 0 ? __client : __server;
            if (player == null)
                return;

            NetworkWriter writer = new NetworkWriter();
            writer.Write(handle);

            if (message != null)
                writer.Write(message);
            
            player.InvokeHandler(handle, -1, new NetworkReader(writer.AsArray()));
        }
    }
}