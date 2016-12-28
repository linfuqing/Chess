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
        }

        public Room(int length)
        {
            __length = length;
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

                foreach (int temp in __players)
                {
                    if (temp < count)
                        return true;
                }

                __count = count;
            }

            return true;
        }

        public bool NotReady(int index)
        {
            if (__players == null)
                return false;

            int count;
            if (!__players.TryGetValue(index, out count))
                return false;

            __players[index] = --count;

            foreach (int temp in __players)
            {
                if (temp > count)
                    return true;
            }

            __count = count;
            if (__count < 1)
                __players.Clear();

            return true;
        }
    }

    public class RoomMessage : MessageBase
    {
        public short index;
        public short roomIndex;
        public short roomLength;

        public RoomMessage()
        {

        }

        public RoomMessage(short index, short roomIndex, short roomLength)
        {
            this.index = index;
            this.roomIndex = roomIndex;
            this.roomLength = roomLength;
        }
    }
}