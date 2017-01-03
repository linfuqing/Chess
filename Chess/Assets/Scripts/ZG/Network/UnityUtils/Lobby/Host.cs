using UnityEngine.Networking;

namespace ZG.Network.Lobby
{
    public enum HostMessageType : short
    {
        RoomInfo = 200
    }

    public enum HostMessageHandle : short
    {
        Ready = 0,
        NotReady
    }

    public interface IHost : Network.IHost
    {
        void Ready(short index);

        void NotReady(short index);
    }
    
    public class Room
    {
        private int __length;
        private int __count;

        private Pool<int> __players;

        public int length
        {
            get
            {
                return __length;
            }

            set
            {
                __length = value;
            }
        }

        public int count
        {
            get
            {
                return __count;
            }

            set
            {
                __count = value;
            }
        }

        public Room(int length, int count)
        {
            __length = length;
            __count = count;
        }

        public bool Ready(int index)
        {
            int count = __players == null ? 0 : __players.count;
            if (count < __length)
            {
                if (__players == null)
                    __players = new Pool<int>();

                if (!__players.TryGetValue(index, out count))
                    count = 0;

                __players.Insert(index, ++count);
                if (__players.count >= __length)
                    __count = 1;
            }
            else
            {
                if (__players == null)
                    return false;

                if (!__players.TryGetValue(index, out count))
                    return false;

                __players[index] = ++count;

                if (__count == count - 1)
                {
                    foreach (int temp in __players)
                    {
                        if (temp < count)
                            return true;
                    }

                    __count = count;
                }
            }

            return true;
        }

        public bool NotReady(int index)
        {
            if (__players == null)
                return false;

            int count;
            if (!__players.TryGetValue(index, out count) || count < 1)
                return false;

            __players[index] = --count;

            if (__count == count + 1)
            {
                foreach (int temp in __players)
                {
                    if (temp > count)
                        return true;
                }

                __count = count;
                if (__count < 1)
                    __players.Clear();
            }

            return true;
        }
    }

    public class RoomMessage : MessageBase
    {
        public short index;
        public short playerIndex;
        public short roomIndex;
        public short roomLength;
        public short roomCount;

        public RoomMessage()
        {

        }

        public RoomMessage(short index, short playerIndex, short roomIndex, short roomLength, short roomCount)
        {
            this.index = index;
            this.playerIndex = playerIndex;
            this.roomIndex = roomIndex;
            this.roomLength = roomLength;
            this.roomCount = roomCount;
        }
    }
}