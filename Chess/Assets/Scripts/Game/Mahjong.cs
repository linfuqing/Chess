using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ZG;

public class Mahjong
{

    public enum TileType
    {
        //Simples
        Dots, 
        Bomboo = Dots + 9, 
        Characters = Bomboo + 9,

        //Winds
        Easet = Characters + 9,
        South,
        West,
        North,

        //Dragons
        Red,
        Green,
        White,
        
        //Seasons
        Spring,
        Summer,
        Autumn,
        Winter,

        //Flowers
        Plum,
        Orchid,
        Chrysanthemum,
        Bamboo,

        Unknown
    }

    public enum RuleType
    {
        Chow,
        Pong,
        Kong,

        HiddenKong,
        MeldedKong, 

        Win, 

        Unknown
    }
    
    public struct Tile
    {
        public TileType type;
        public int number;

        public static implicit operator Tile(byte code)
        {
            return code < (byte)TileType.Unknown ? (code < (byte)TileType.Easet ? new Tile((TileType)(code / 9 * 9), code % 9) : new Tile((TileType)code, 0)) : new Tile(TileType.Unknown, 0);
        }

        public static implicit operator byte(Tile tile)
        {
            return (byte)((int)tile.type + tile.number);
        }

        public static Tile Get(int index)
        {
            byte code = (byte)index, count = (byte)TileType.Plum << 2;
            if (code < count)
                code >>= 2;
            else
                code -= count;

            return code;
        }

        public Tile(TileType type, int number)
        {
            this.type = type;
            this.number = number;
        }

        public override int GetHashCode()
        {
            return (int)type + number;
        }
    }
    
    public struct RuleNode
    {
        public RuleType type;

        public int index;

        public int offset;

        public RuleNode(RuleType type, int index, int offset)
        {
            this.type = type;
            this.index = index;
            this.offset = offset;
        }
    }
    
    public struct RuleObject
    {
        public RuleNode instance;
        public int playerIndex;

        public RuleObject(RuleNode instance, int playerIndex)
        {
            this.instance = instance;
            this.playerIndex = playerIndex;
        }
    }

    public class Rule
    {
        public virtual int[] winFlag
        {
            get
            {
                return new int[]
                    {
                    33, //11、111、111、111、111
                    89, //11、111、111、111、123
                    145, //11、111、111、123、123
                    201, //11、111、123、123、123
                    257, //11、123、123、123、123
                    7 //11、11、11、11、11、11、11
                    };
            }
        }

        public void Check(IEnumerable<int> indices, int index, Action<RuleNode> set)
        {
            if (indices == null || set == null)
                return;
            
            Tile tile = Tile.Get(index), source = new Tile(), destination;
            int count = 0, offset = 0, result = -1;
            bool isType = false, isEyes = false;
            foreach (int temp in indices)
            {
                destination = Tile.Get(temp);
                if (destination.type != tile.type)
                {
                    if (isType)
                        return;
                    else
                        continue;
                }

                switch (count)
                {
                    case 0:
                        isType = true;

                        isEyes = true;

                        result = -1;
                        break;
                    case 1:
                        if (destination.number - source.number == 1)
                            isEyes = false;
                        else if (destination.number != source.number)
                        {
                            isEyes = true;

                            result = -1;

                            count = 0;
                        }
                        break;
                    case 2:
                        if (destination.number - source.number == 1)
                        {
                            if (isEyes)
                                isEyes = false;
                            else
                            {
                                if (result >= 0)
                                    set(new RuleNode(RuleType.Chow, offset - 2, result));
                            }

                            count = 1;
                        }
                        else if (destination.number == source.number)
                        {
                            if (isEyes)
                            {
                                if (result >= 0)
                                    set(new RuleNode(RuleType.Pong, offset - 2, result));
                            }
                            else
                                count = 1;
                        }
                        else
                        {
                            isEyes = true;

                            result = -1;

                            count = 0;
                        }

                        break;
                    case 3:
                        if (destination.number == source.number)
                        {
                            if (isEyes)
                            {
                                if (result >= 0)
                                {
                                    set(new RuleNode(RuleType.Pong, offset - 2, result));
                                    set(new RuleNode(RuleType.Kong, offset - 3, result));
                                }

                                count = 2;
                            }
                            else
                                count = 1;
                        }
                        else
                        {
                            isEyes = true;

                            result = -1;

                            count = 0;
                        }

                        break;
                    default:
                        isEyes = true;

                        result = -1;

                        count = 0;
                        break;
                }

                if (destination.number == tile.number)
                {
                    if (isEyes)
                    {
                        ++count;

                        switch (count)
                        {
                            case 2:
                                set(new RuleNode(RuleType.Pong, offset - 2, 2));
                                break;
                            case 3:
                                set(new RuleNode(RuleType.Pong, offset - 2, 2));
                                set(new RuleNode(RuleType.Kong, offset - 3, 3));

                                count = 2;
                                break;
                        }
                    }
                    else
                        count = 1;

                    result = count;
                }
                else if (Math.Abs(destination.number - tile.number) == 1)
                {
                    if (isEyes)
                    {
                        count = 1;

                        isEyes = false;
                    }
                    else if (count == 1)
                        set(new RuleNode(RuleType.Chow, offset - 2, 2));
                    else
                        ++count;

                    if (destination.number < tile.number)
                    {
                        destination = tile;

                        result = count;
                    }
                    else
                    {
                        count = 0;

                        result = 0;
                    }
                }

                ++offset;

                ++count;

                source = destination;
            }
        }

        public bool Win(IEnumerable<int> indices)
        {
            if (indices == null)
                return false;
            
            IEnumerable<int> result = this.winFlag;
            Tile source = new Tile(), destination;
            int count = 0;
            bool isEyes = false;
            foreach (int index in indices)
            {
                switch (count)
                {
                    case 0:
                        source = Tile.Get(index);
                        break;
                    case 1:
                        destination = Tile.Get(index);
                        if (destination.type == source.type)
                        {
                            if (destination.number == source.number)
                                isEyes = true;
                            else if (destination.number - source.number == 1)
                                isEyes = false;
                            else
                                return false;

                            source = destination;
                        }
                        else
                            return false;
                        break;
                    case 2:
                        destination = Tile.Get(index);
                        if (destination.type != source.type)
                            return false;

                        if (isEyes)
                        {
                            if (destination.number != source.number)
                            {
                                result =
                                    from winFlag in result
                                    where (winFlag & 0x7) > 0
                                    select winFlag - 1;

                                if (!result.Any())
                                    return false;

                                count = 0;
                            }

                            source = destination;
                        }
                        else if (destination.number - source.number == 1)
                        {
                            result =
                                    from winFlag in result
                                    where ((winFlag >> 6) & 0x7) > 0
                                    select winFlag - (1 << 6);

                            if (!result.Any())
                                return false;

                            count = 0;

                            continue;
                        }
                        else
                            return false;

                        break;
                    case 3:
                        destination = Tile.Get(index);
                        if (destination.type != source.type || !isEyes)
                            return false;

                        result =
                        from winFlag in result
                        where ((winFlag >> 3) & 0x7) > 0
                        select winFlag - (1 << 3);

                        if (!result.Any())
                            return false;

                        count = 0;

                        if (destination.number == source.number)
                            continue;

                        source = destination;
                        break;
                    default:
                        return false;
                }

                ++count;
            }

            return true;
        }
    }

    public class Player : IEnumerable<int>
    {
        public struct Group
        {
            internal RuleType _type;

            internal LinkedListNode<int> _node0;
            internal LinkedListNode<int> _node1;
            internal LinkedListNode<int> _node2;
            internal LinkedListNode<int> _node3;
            
            public Group(
                RuleType type, 
                LinkedListNode<int> node0, 
                LinkedListNode<int> node1, 
                LinkedListNode<int> node2, 
                LinkedListNode<int> node3)
            {
                _type = type;
                _node0 = node0;
                _node1 = node1;
                _node2 = node2;
                _node3 = node3;
            }
        }

        public struct Enumerator : IEnumerator<int>
        {
            private IEnumerator<LinkedListNode<int>> __instance;

            public int Current
            {
                get
                {
                    LinkedListNode<int> node = __instance == null ? null : __instance.Current;
                    return node == null ? -1 : node.Value;
                }
            }

            public Enumerator(IEnumerator<LinkedListNode<int>> instance)
            {
                __instance = instance;
            }

            public bool MoveNext()
            {
                return __instance != null && __instance.MoveNext();
            }

            public void Reset()
            {
                if (__instance != null)
                    __instance.Reset();
            }

            public void Dispose()
            {
                if (__instance != null)
                    __instance.Dispose();
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }

        private int[] temp = new int[4];

        private bool __isDraw;
        private int __index;
        private int __handleTileIndex;
        private int __kongCount;
        private Mahjong __mahjong;
        private Pool<LinkedListNode<int>> __handIndices;
        private LinkedList<int> __handTileIndices;
        private List<int> __poolTileIndices;
        private List<RuleObject> __ruleObjects;
        private Pool<Group> __groups; 

        public bool isDraw
        {
            get
            {
                return __isDraw;
            }
        }
        
        public int index
        {
            get
            {
                return __index;
            }

            set
            {
#if DEBUG
                if (__mahjong == null)
                    throw new InvalidOperationException();

                if (value < 0 || value > 3)
                    throw new IndexOutOfRangeException();
#endif

                if (__mahjong.__players == null || __mahjong.__players.Length < 4)
                    __mahjong.__players = new Player[4];

                if(__index >= 0 && __index < 4)
                    __mahjong.__players[__index] = null;

                __mahjong.__players[value] = this;

                __index = value;
            }
        }

        public int handleTileIndex
        {
            get
            {
                return __handleTileIndex;
            }
        }

        public Mahjong mahjong
        {
            get
            {
                return __mahjong;
            }
        }

        public IEnumerable<int> handTileIndices
        {
            get
            {
                return __handTileIndices;
            }
        }

        public IEnumerable<Group> groups
        {
            get
            {
                return __groups;
            }
        }

        public Player(Mahjong mahjong)
        {
            __mahjong = mahjong;
            __index = -1;
        }

        public Tile GetHandTile(int index)
        {
            if (__handIndices == null)
                return new Tile(TileType.Unknown, 0);

            LinkedListNode<int> node;
            if (!__handIndices.TryGetValue(index, out node) || node == null)
                return new Tile(TileType.Unknown, 0);

            return Tile.Get(node.Value);
        }

        public bool Draw(Action<int> add, Action<int> remove)
        {
            if (__mahjong == null || __isDraw)
                return false;

            if (__mahjong.__players == null || __mahjong.__players[__mahjong.__playerIndex] != this)
                return false;
            
            int handTileCount = __handTileIndices == null ? 0 : __handTileIndices.Count;
            if (handTileCount > (13 + __kongCount))
                return false;
            
            int tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
            if (__mahjong.__tileCount >= tileCount)
                return false;
            
            LinkedListNode<int> node;
            if (handTileCount < 9)
            {
                node = __handTileIndices == null ? null : __handTileIndices.Last;

                int index;
                for (int i = 0; i < 4; ++i)
                {
                    ++__mahjong.__tileCount;

                    if (__mahjong.__tileIndex >= tileCount)
                        __mahjong.__tileIndex = 0;

                    index = __Add(__Add(__mahjong.__tileIndices[__mahjong.__tileIndex++]));
                    temp[i] = index;

                    if (add != null)
                        add(index);
                }

                if (__handIndices != null)
                {
                    foreach (int i in temp)
                    {
                        if (__handIndices.TryGetValue(i, out node) && node != null)
                        {
                            while (true)
                            {
                                switch (Tile.Get(node.Value).type)
                                {
                                    case TileType.Spring:
                                    case TileType.Summer:
                                    case TileType.Autumn:
                                    case TileType.Winter:
                                        
                                    case TileType.Plum:
                                    case TileType.Orchid:
                                    case TileType.Chrysanthemum:
                                    case TileType.Bamboo:
                                        if (remove != null)
                                            remove(i);

                                        ++__mahjong.__tileCount;

                                        if (__mahjong.__tileIndex >= tileCount)
                                            __mahjong.__tileIndex = 0;

                                        node.Value = __mahjong.__tileIndices[__mahjong.__tileIndex++];

                                        if (add != null)
                                            add(i);
                                        continue;
                                }

                                break;
                            }
                        }
                    }
                }
                
                __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;

                return true;
            }

            if (handTileCount < (13 + __kongCount))
                __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;
            else
            {
                __Clear();

                __isDraw = true;
            }

            node = null;
            while (__mahjong.__tileCount < tileCount)
            {
                ++__mahjong.__tileCount;

                if (__mahjong.__tileIndex >= tileCount)
                    __mahjong.__tileIndex = 0;

                int index = __mahjong.__tileIndices[__mahjong.__tileIndex++];
                switch (Tile.Get(index).type)
                {
                    case TileType.Spring:
                    case TileType.Summer:
                    case TileType.Autumn:
                    case TileType.Winter:

                    case TileType.Plum:
                    case TileType.Orchid:
                    case TileType.Chrysanthemum:
                    case TileType.Bamboo:
                        if (node == null)
                            node = new LinkedListNode<int>(index);
                        else
                            node.Value = index;

                        index = __Add(node);

                        if (add != null)
                            add(index);

                        if (remove != null)
                            remove(index);

                        if (__handIndices != null)
                            __handIndices.RemoveAt(index);

                        break;
                    default:
                        __handleTileIndex = __Add(__Add(index));

                        if (add != null)
                            add(__handleTileIndex);

                        return true;
                }
            }

            return false;
        }

        public bool Discard(int index)
        {
            if (__mahjong == null || !__isDraw)
                return false;

            if (__handIndices == null || __handTileIndices == null || __handTileIndices.Count != 14)
                return false;

            __isDraw = false;
            __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;

            LinkedListNode<int> node;
            if (!__handIndices.TryGetValue(index, out node) || node == null)
                return false;


            if (!__handIndices.RemoveAt(index))
                return false;
            
            __handTileIndices.Remove(node);
                
            if (__poolTileIndices == null)
                __poolTileIndices = new List<int>();

            __poolTileIndices.Add(node.Value);

            __Clear();

            return true;
        }

        public ReadOnlyCollection<RuleObject> Start()
        {
            if (__mahjong == null || 
                __mahjong.rule == null || 
                __mahjong.__players == null || 
                __mahjong.__players.Length <= __mahjong.__playerIndex || 
                __mahjong.__tileCount < 47)
                return null;

            if (__mahjong.__players[__mahjong.__playerIndex] == this)
            {
                if (__isDraw)
                {
                    if (__handIndices != null)
                    {
                        LinkedListNode<int> node;
                        if (__handIndices.TryGetValue(__handleTileIndex, out node) && node != null)
                        {
                            LinkedListNode<int> temp = node.Previous;
                            if (__handTileIndices != null)
                                __handTileIndices.Remove(node);

                            __mahjong.rule.Check(__poolTileIndices, node.Value, delegate (RuleNode ruleNode)
                            {
                                if (ruleNode.type != RuleType.Kong)
                                    return;

                                __Add(ruleNode, __mahjong.__playerIndex);
                            });

                            if (__handTileIndices != null)
                            {
                                if (temp == null)
                                    __handTileIndices.AddFirst(node);
                                else
                                    __handTileIndices.AddAfter(temp, node);
                            }
                        }
                    }

                    if (__mahjong.rule.Win(__handTileIndices))
                        __Add(new RuleNode(RuleType.Win, 0, 0), __mahjong.__playerIndex);
                }
                else
                {
                    int playerIndex = (__mahjong.__playerIndex + 3) & 3;
                    Player player = __mahjong.__players[playerIndex];
                    int count = player == null || player.__poolTileIndices == null ? 0 : player.__poolTileIndices.Count;
                    if (count > 0)
                    {
                        int tileIndex = player.__poolTileIndices[count - 1];
                        __mahjong.rule.Check(this, tileIndex, delegate (RuleNode ruleNode)
                        {
                            __Add(ruleNode, playerIndex);

                            LinkedListNode<int> node = __Add(tileIndex);
                            if(__mahjong.rule.Win(__handTileIndices))
                            {
                                ruleNode.type = RuleType.Win;
                                __Add(ruleNode, playerIndex);
                            }

                            if (__handTileIndices != null)
                                __handTileIndices.Remove(node);
                        });
                    }
                    else
                        return null;
                }
            }
            else
            {
                Player player = __mahjong.__players[__mahjong.__playerIndex];
                if (player == null || player.__isDraw)
                    return null;

                int playerIndex = (__mahjong.__playerIndex + 3) & 3;
                player = __mahjong.__players[playerIndex];
                if (player == this)
                    return null;

                int count = player == null || player.__poolTileIndices == null ? 0 : player.__poolTileIndices.Count;
                if (count > 0)
                {
                    int tileIndex = player.__poolTileIndices[count - 1];
                    __mahjong.rule.Check(this, tileIndex, delegate (RuleNode ruleNode)
                    {
                        if (ruleNode.type == RuleType.Chow)
                            return;

                        __Add(ruleNode, playerIndex);
                        
                        LinkedListNode<int> node = __Add(tileIndex);
                        if (__mahjong.rule.Win(__handTileIndices))
                        {
                            ruleNode.type = RuleType.Win;
                            __Add(ruleNode, playerIndex);
                        }

                        if (__handTileIndices != null)
                            __handTileIndices.Remove(node);
                    });
                }
            }

            return __ruleObjects == null ? null : __ruleObjects.AsReadOnly();
        }
        
        public RuleObject Get(int index)
        {
            int count = __ruleObjects == null ? 0 : __ruleObjects.Count;
            return index < count ? __ruleObjects[index] : new RuleObject(new RuleNode(RuleType.Unknown, 0, 0), __index);
        }
        
        public bool Try(int index)
        {
            if (__mahjong == null)
                return false;

            if (((__mahjong.__rulePlayerIndex + 1) & 3) != __index)
                return false;

            RuleType ruleType = Get(index).instance.type;
            switch (ruleType)
            {
                case RuleType.Chow:
                    if (__mahjong.__ruleType == RuleType.Unknown)
                    {
                        __mahjong.__rulePlayerIndex = __index;
                        __mahjong.__ruleObjectIndex = index;
                        __mahjong.__ruleType = ruleType;

                        return true;
                    }
                    break;
                case RuleType.Pong:
                    switch (__mahjong.__ruleType)
                    {
                        case RuleType.Chow:
                        case RuleType.Unknown:

                            __mahjong.__rulePlayerIndex = __index;
                            __mahjong.__ruleObjectIndex = index;
                            __mahjong.__ruleType = ruleType;

                            return true;
                        default:
                            break;
                    }
                    break;
                case RuleType.Kong:
                case RuleType.HiddenKong:
                case RuleType.MeldedKong:
                    switch (__mahjong.__ruleType)
                    {
                        case RuleType.Chow:
                        case RuleType.Pong:
                        case RuleType.Unknown:

                            __mahjong.__rulePlayerIndex = __index;
                            __mahjong.__ruleObjectIndex = index;
                            __mahjong.__ruleType = ruleType;

                            return true;
                        default:
                            break;
                    }
                    break;
                case RuleType.Win:
                    __mahjong.__rulePlayerIndex = __index;
                    __mahjong.__ruleObjectIndex = index;
                    __mahjong.__ruleType = ruleType;

                    return true;
                default:
                    break;
            }

            return false;
        }
        
        public RuleType End(int index)
        {
            if (__ruleObjects == null || 
                __ruleObjects.Count <= index)
                return RuleType.Unknown;

            RuleObject target = __ruleObjects[index];
            
            index = target.instance.index;
            LinkedListNode<int> node = __handTileIndices == null ? null : __handTileIndices.First;
            while (node != null)
            {
                if (--index < 0)
                    break;

                node = node.Next;
            }

            if (node == null)
                return RuleType.Unknown;

            int count;
            Player player;
            LinkedListNode<int> node1, result;
            if (target.instance.type == RuleType.Kong)
            {
                LinkedListNode<int> node2;
                if (target.playerIndex == __index)
                {
                    if (__handIndices == null)
                        return RuleType.Unknown;

                    if (!__handIndices.TryGetValue(__handleTileIndex, out result) || result == null)
                        return RuleType.Unknown;
                    
                    if (__groups != null)
                    {
                        Group group;
                        foreach (KeyValuePair<int, Group> pair in (IEnumerable<KeyValuePair<int, Group>>)__groups)
                        {
                            group = pair.Value;
                            if (group._node0 == node)
                            {
                                group._type = RuleType.MeldedKong;
                                group._node3 = result;

                                __groups[pair.Key] = group;

                                ++__kongCount;
                                
                                __isDraw = false;

                                __Clear();

                                return RuleType.MeldedKong;
                            }
                        }
                    }

                    node1 = node.Next;
                    if (node1 == null)
                        return RuleType.Unknown;

                    node2 = node.Next;
                    if (node2 == null)
                        return RuleType.Unknown;

                    __handIndices.Remove(node);
                    __handIndices.Remove(node1);
                    __handIndices.Remove(node2);

                    if (__groups == null)
                        __groups = new Pool<Group>();

                    __groups.Add(new Group(RuleType.HiddenKong, node, node1, node2, result));

                    ++__kongCount;

                    __isDraw = false;

                    __Clear();

                    return RuleType.HiddenKong;
                }

                if (__mahjong.__players == null ||
                __mahjong.__players.Length <= target.playerIndex)
                    return RuleType.Unknown;

                player = __mahjong.__players[target.playerIndex];
                if (player == null)
                    return RuleType.Unknown;

                count = player.__poolTileIndices == null ? 0 : player.__poolTileIndices.Count;
                if (count < 1)
                    return RuleType.Unknown;

                --count;
                result = __Add(player.__poolTileIndices[count]);

                node1 = node.Next;
                if (node1 == null)
                    return RuleType.Unknown;

                node2 = node.Next;
                if (node2 == null)
                    return RuleType.Unknown;

                __handIndices.Remove(node);
                __handIndices.Remove(node1);
                __handIndices.Remove(node2);

                if (__groups == null)
                    __groups = new Pool<Group>();

                __groups.Add(new Group(RuleType.Kong, node, node1, node2, result));
                
                player.__poolTileIndices.RemoveAt(count);

                __mahjong.__playerIndex = __index;

                ++__kongCount;

                __Clear();

                return RuleType.Kong;
            }
            
            if (__mahjong.__players == null ||
            __mahjong.__players.Length <= target.playerIndex)
                return RuleType.Unknown;

            player = __mahjong.__players[target.playerIndex];
            if (player == null)
                return RuleType.Unknown;

            count = player.__poolTileIndices == null ? 0 : player.__poolTileIndices.Count;
            if (count < 1)
                return RuleType.Unknown;

            --count;
            result = __Add(player.__poolTileIndices[count]);

            node1 = node.Next;
            if (node1 == null)
                return RuleType.Unknown;
            
            __handIndices.Remove(node);
            __handIndices.Remove(node1);

            if (__groups == null)
                __groups = new Pool<Group>();

            __groups.Add(new Group(target.instance.type, node, node1, result, null));
            
            player.__poolTileIndices.RemoveAt(count);

            __mahjong.__playerIndex = __index;

            __isDraw = true;

            __Clear();

            return target.instance.type;
        }
        
        public bool Check(int index)
        {
            if (__mahjong == null || __mahjong.rule == null)
                return false;

            int count = __handTileIndices == null ? 0 : __handTileIndices.Count;
            if (count < 13)
                return false;

            LinkedListNode<int> node = __Add(index);
            bool result = __mahjong.rule.Win(__handTileIndices);
            __handTileIndices.Remove(node);

            return result;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(__handIndices == null ? null : __handIndices.GetEnumerator());
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        private LinkedListNode<int> __Add(int index)
        {
            if (__handTileIndices == null)
                __handTileIndices = new LinkedList<int>();

            byte code = Tile.Get(index), temp;
            for (LinkedListNode<int> node = __handTileIndices.First; node != null; node = node.Next)
            {
                temp = Tile.Get(node.Value);
                if (temp > code)
                    return __handTileIndices.AddBefore(node, index);
            }
            
            return __handTileIndices.AddLast(index);
        }

        private int __Add(LinkedListNode<int> node)
        {
            if (__handIndices == null)
                __handIndices = new Pool<LinkedListNode<int>>();

            return __handIndices.Add(node);
        }

        private void __Add(RuleNode ruleNode, int playerIndex)
        {
            if (__mahjong == null)
                return;

            switch (ruleNode.type)
            {
                case RuleType.Chow:
                    if (__mahjong.__ruleType != RuleType.Unknown)
                        return;
                    
                    if (__handIndices == null)
                        return;

                    int index = ruleNode.index;
                    LinkedListNode<int> node = __handTileIndices == null ? null : __handTileIndices.First;
                    while (node != null)
                    {
                        if (--index < 0)
                            break;

                        node = node.Next;
                    }

                    for (int i = 0; i < 2; ++i)
                    {
                        if (!__handIndices.ContainsValue(node))
                            return;

                        node = node.Next;
                    }
                    break;
                case RuleType.Pong:
                    switch (__mahjong.__ruleType)
                    {
                        case RuleType.Chow:
                        case RuleType.Unknown:
                            break;
                        default:
                            return;
                    }
                    break;
                case RuleType.Kong:
                case RuleType.HiddenKong:
                case RuleType.MeldedKong:
                    switch (__mahjong.__ruleType)
                    {
                        case RuleType.Chow:
                        case RuleType.Pong:
                        case RuleType.Unknown:
                            break;
                        default:
                            return;
                    }
                    break;
                case RuleType.Win:
                    break;
                default:
                    return;
            }
            
            if (__ruleObjects == null)
                __ruleObjects = new List<RuleObject>();

            __ruleObjects.Add(new RuleObject(ruleNode, playerIndex));
        }

        private void __Clear()
        {
            if (__mahjong == null)
                return;

            if(__mahjong.__players != null)
            {
                foreach(Player player in __mahjong.__players)
                {
                    if (player != null && player.__ruleObjects != null)
                        player.__ruleObjects.Clear();
                }
            }

            __mahjong.__rulePlayerIndex = __mahjong.__playerIndex;
            __mahjong.__ruleObjectIndex = -1;
            __mahjong.__ruleType = RuleType.Unknown;
        }
    }
    
    private Player[] __players;
    private int[] __tileIndices;
    private int __tileIndex;
    private int __tileCount;
    private int __dealerIndex;
    private int __playerIndex;
    private int __rulePlayerIndex;
    private int __ruleObjectIndex;
    private RuleType __ruleType;
    public Rule rule;
    
    public int tileIndex
    {
        get
        {
            return __tileIndex;
        }
    }

    public int tileCount
    {
        get
        {
            return __tileCount;
        }
    }

    public int dealerIndex
    {
        get
        {
            return __dealerIndex;
        }
    }

    public int playerIndex
    {
        get
        {
            return __playerIndex;
        }
    }

    public int rulePlayerIndex
    {
        get
        {
            return __rulePlayerIndex;
        }
    }

    public int ruleObjectIndex
    {
        get
        {
            return __ruleObjectIndex;
        }
    }

    public RuleType ruleType
    {
        get
        {
            return __ruleType;
        }
    }

    public IEnumerable<Player> players
    {
        get
        {
            return __players;
        }
    }

    public Mahjong()
    {
        
    }

    public Player Get(int index)
    {
        if (index < 0 || __players == null || __players.Length <= index)
            return null;

        return __players[index];
    }

    public void Shuffle(out int point0, out int point1, out int point2, out int point3)
    {
        int poolTileCount = 144, handTileCount = 18;

        Random random = new Random();
        if (__tileIndices == null)
        {
            __tileIndices = new int[poolTileCount];
            for (int i = 0; i < poolTileCount; ++i)
                __tileIndices[i] = i;
        }

        --poolTileCount;
        int index, temp;
        for (int i = 0; i < poolTileCount; ++i)
        {
            index = random.Next(i, poolTileCount);
            if(index != i)
            {
                temp = __tileIndices[index];
                __tileIndices[index] = __tileIndices[i];
                __tileIndices[i] = temp;
            }
        }
        
        point0 = random.Next(0, 5);
        point1 = random.Next(0, 5);
        point2 = random.Next(0, 5);
        point3 = random.Next(0, 5);

        int count = point0 + point1;
        
        __tileIndex = ((count + 1) & 3) * handTileCount + count + point2 + point3;
        __tileCount = 0;
        
        __dealerIndex = random.Next(0, 3);
        __playerIndex = __dealerIndex;
    }
}
