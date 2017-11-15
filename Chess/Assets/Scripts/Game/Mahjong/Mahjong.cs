using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ZG;

public class Mahjong
{
    [Flags]
    public enum ShuffleType
    {
        Winds = 0x01, 
        Flowers = 0x02, 
        All = Winds | Flowers
    }

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
        SelfDraw,
        BreakKong, 
        OverKong, 

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
            byte code = (byte)index, count = (byte)TileType.Spring << 2;
            if (code < count)
                code >>= 2;
            else
                code = (byte)(code + (int)TileType.Spring - count);

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

    public class Rule
    {
        public interface IEnumerator : IEnumerator<int>
        {
            IEnumerator Clone();
        }

        public struct WinFlag
        {
            public int index;
            public int instance;

            public WinFlag(int index, int instance)
            {
                this.index = index;
                this.instance = instance;
            }
        }


        public static bool IsSimpleHand(IEnumerable<int> indices)
        {
            if (indices == null)
                return false;

            Tile tile;
            foreach(int index in indices)
            {
                tile = Tile.Get(index);
                switch(tile.type)
                {
                    case TileType.Dots:
                    case TileType.Bomboo:
                    case TileType.Characters:
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        //混一色
        public static bool IsCleanHand(IEnumerable<int> indices)
        {
            IEnumerator<int> enumerator = indices == null ? null : indices.GetEnumerator();
            if (enumerator == null || !enumerator.MoveNext())
                return false;

            TileType type = Tile.Get(enumerator.Current).type;
            while (enumerator.MoveNext())
            {
                if (type != Tile.Get(enumerator.Current).type)
                    return false;
            }

            return true;
        }

        //清一色
        public static bool IsPureHand(IEnumerable<int> indices)
        {
            if (indices == null)
                return false;

            TileType source = TileType.Unknown, destination;
            foreach (int index in indices)
            {
                destination = Tile.Get(index).type;
                switch (destination)
                {
                    case TileType.Easet:
                    case TileType.South:
                    case TileType.West:
                    case TileType.North:

                    case TileType.Red:
                    case TileType.Green:
                    case TileType.White:
                        continue;
                }

                if (source == TileType.Unknown)
                    source = destination;
                else if (source != destination)
                    return false;
            }

            return true;
        }

        //将一色
        public static bool IsGreatHand258(IEnumerable<int> indices)
        {
            IEnumerator<int> enumerator = indices == null ? null : indices.GetEnumerator();
            if (enumerator == null || !enumerator.MoveNext())
                return false;

            int count = 0;
            Tile source = Tile.Get(enumerator.Current), destination;
            while (enumerator.MoveNext())
            {
                destination = Tile.Get(enumerator.Current);
                if (source.type != destination.type)
                {
                    switch (count)
                    {
                        case 2:
                            break;
                        case 3:
                            switch (source.number)
                            {
                                case 1:
                                case 4:
                                case 7:
                                    break;
                                default:
                                    return false;
                            }
                            break;
                        default:
                            return false;
                    }

                    count = 0;
                }
                else
                    ++count;
            }

            return true;
        }

        public virtual IEnumerable<WinFlag> winFlags
        {
            get
            {
                return new WinFlag[]
                    {
                    new WinFlag(0, 33), //11、111、111、111、111
                    new WinFlag(1, 89), //11、111、111、111、123
                    new WinFlag(2, 145), //11、111、111、123、123
                    new WinFlag(3, 201), //11、111、123、123、123
                    new WinFlag(4, 257), //11、123、123、123、123
                    new WinFlag(5, 7) //11、11、11、11、11、11、11
                    };
            }
        }
        
        public virtual int GetMask(RuleType ruleType, out int shift)
        {
            switch (ruleType)
            {
                case RuleType.Chow:
                    shift = 6;
                    return 0x7;
                case RuleType.Pong:
                case RuleType.Kong:
                case RuleType.HiddenKong:
                case RuleType.MeldedKong:
                    shift = 3;
                    return 0x7;
            }

            shift = 0;
            return 0;
        }

        public virtual bool CheckEye(int index, int count, ref IEnumerable<WinFlag> winFlags)
        {
            switch (count)
            {
                case 2:
                    winFlags =
                       from winFlag in winFlags
                       where (winFlag.instance & 0x7) > 0
                       select new WinFlag(winFlag.index, winFlag.instance - 1);

                    if (!winFlags.Any())
                        return false;

                    break;
                case 3:
                    winFlags =
                       from winFlag in winFlags
                       where ((winFlag.instance >> 3) & 0x7) > 0
                       select new WinFlag(winFlag.index, winFlag.instance - (1 << 3));

                    if (!winFlags.Any())
                        return false;

                    break;
                case 4:
                    winFlags =
                       (from winFlag in winFlags
                        where ((winFlag.instance >> 3) & 0x7) > 0
                        select new WinFlag(winFlag.index, winFlag.instance - (1 << 3))).Union(from winFlag in winFlags
                                                                                              where (winFlag.instance & 0x7) > 1
                                                                                              select new WinFlag(winFlag.index, winFlag.instance - 2));

                    if (!winFlags.Any())
                        return false;
                    break;
                default:
                    return false;
            }

            return true;
        }

        public virtual bool Check(int index, int currentCount, int nextCount, int count, ref IEnumerable<WinFlag> winFlags)
        {
            IEnumerable<WinFlag> result = null;
            if (count > 0)
            {
                result = winFlags;

                if(CheckEye(index, count, ref result))
                {
                    if (nextCount > 0)
                    {
                        if (CheckEye(index -= nextCount, count, ref result))
                        {
                            if (currentCount > 0 && !CheckEye(index -= currentCount, count, ref result))
                                result = null;
                        }
                        else
                            result = null;
                    }
                    else if(currentCount > 0 && !CheckEye(index -= currentCount, count, ref result))
                        result = null;
                }
                else
                    result = null;
            }

            for (int i = 0; i < count; ++i)
            {
                winFlags =
                        from winFlag in winFlags
                        where ((winFlag.instance >> 6) & 0x7) > 0
                        select new WinFlag(winFlag.index, winFlag.instance - (1 << 6));

                if (!winFlags.Any())
                    break;
            }

            if(result != null)
            {
                if (winFlags == null)
                    winFlags = result;
                else
                    winFlags = winFlags.Union(result);
            }

            return winFlags.Any();
        }

        public virtual bool Check(IEnumerator enumerator, ref IEnumerable<WinFlag> winFlags)
        {
            if (winFlags == null)
                return false;

            if (enumerator == null || !enumerator.MoveNext())
                return false;

            return __Check(0, 0, 0, 0, 1, Tile.Get(enumerator.Current), enumerator, ref winFlags);
        }

        public virtual bool Check(IEnumerator enumerator, IEnumerable<WinFlag> winFlags, Func<bool, int, int, IEnumerable<WinFlag>, bool> handler)
        {
            if (winFlags == null)
                return false;

            if (enumerator == null || !enumerator.MoveNext())
                return false;

            if (__Check(0, 0, 0, 0, 1, Tile.Get(enumerator.Current), handler, enumerator, ref winFlags))
                return true;

            return false;
        }
        
        public void Check(IEnumerable<int> indices, int index, Action<RuleNode> set)
        {
            if (set == null)
                return;

            IEnumerator<int> enumerator = indices == null ? null : indices.GetEnumerator();
            if (enumerator == null || !enumerator.MoveNext())
                return;

            int current = 0;
            Tile tile = Tile.Get(index), source = Tile.Get(enumerator.Current);
            while (tile.type != source.type && enumerator.MoveNext())
            {
                source = Tile.Get(enumerator.Current);

                ++current;
            }

            if(tile.type != source.type)
                return;

            Tile destination;
            int offset = current + 1, eyeCount = 0, count = 0, currentStep = 1, nextStep = 1;
            while(true)
            {
                if (source.number == tile.number)
                {
                    ++eyeCount;
                    switch (eyeCount)
                    {
                        case 2:
                            set(new RuleNode(RuleType.Pong, offset - 2, 2));
                            break;
                        case 3:
                            set(new RuleNode(RuleType.Kong, offset - 3, 3));
                            break;
                    }
                }

                if (!enumerator.MoveNext())
                    break;

                destination = Tile.Get(enumerator.Current);
                if (destination.type != tile.type)
                    break;

                if (destination.number == source.number)
                {
                    switch (count)
                    {
                        case 0:
                            ++currentStep;
                            break;
                        case 1:
                            ++nextStep;
                            break;
                    }
                }
                else
                {
                    if (count > 0 && tile.number - source.number == 1)
                        set(new RuleNode(RuleType.Chow, current, 2));

                    if (destination.number - source.number == 1)
                    {
                        if (source.number - tile.number == 1)
                            set(new RuleNode(RuleType.Chow, current + (count > 0 ? currentStep : 0), 0));

                        ++count;

                        if (count > 1)
                        {
                            if (tile.number == source.number)
                                set(new RuleNode(RuleType.Chow, current, 1));

                            count = 1;
                            current += currentStep;
                            currentStep = nextStep;
                            nextStep = 1;
                        }
                    }
                    else
                    {
                        if (tile.number - source.number == 1 && destination.number - tile.number == 1)
                            set(new RuleNode(RuleType.Chow, current + (count > 0 ? currentStep : 0), 1));

                        count = 0;

                        current = offset;

                        currentStep = 1;

                        nextStep = 1;
                    }
                }
                
                source = destination;

                ++offset;
            }

            if (count > 0 && tile.number - source.number == 1)
                set(new RuleNode(RuleType.Chow, current, 2));
        }
        
        /*public void Check(IEnumerable<int> indices, int index, Action<RuleNode> set)
        {
            if (indices == null || set == null)
                return;
            
            Tile tile = Tile.Get(index), source = new Tile(), destination;
            int eyeCount = 0, count = 0, offset = 0, current = 0, currentStep = 0, nextStep = 1, result = -1;
            bool isType = false;
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

                if (destination.number == tile.number)
                {
                    ++eyeCount;
                    switch(eyeCount)
                    {
                        case 2:
                            set(new RuleNode(RuleType.Pong, offset - 1, 2));
                            break;
                        case 3:
                            set(new RuleNode(RuleType.Kong, offset - 2, 3));
                            break;
                    }

                    source = destination;
                }
                else
                {
                    if (result == -1)
                    {
                        if (destination.number - tile.number == 1)
                        {
                            result = 0;

                            count = 2;

                            current = offset;

                            currentStep = 0;

                            source = destination;
                        }
                        else
                        {
                            if (isType)
                            {
                                if (destination.number - source.number == 1)
                                {
                                    ++count;

                                    if(count > 1)
                                    {
                                        count = 1;
                                        current += currentStep;
                                        currentStep = nextStep;
                                        nextStep = 1;
                                    }
                                }
                                else if (destination.number == source.number)
                                {
                                    switch (count)
                                    {
                                        case 0:
                                            ++currentStep;
                                            break;
                                        case 1:
                                            ++nextStep;
                                            break;
                                    }
                                }
                                else
                                {
                                    count = 0;

                                    current = offset;

                                    currentStep = 1;

                                    nextStep = 1;
                                }
                            }

                            if (tile.number - destination.number == 1)
                            {
                                result = ++count;

                                ++count;

                                if (count > 2)
                                {
                                    result -= count - 3;
                                    count = 3;
                                    set(new RuleNode(RuleType.Chow, current, result));

                                    if (result <= 0)
                                        return;
                                    
                                    current += currentStep;
                                    currentStep = nextStep;
                                    nextStep = 1;
                                }

                                source = tile;
                            }
                            else
                                source = destination;
                        }
                    }
                    else if (destination.number - source.number == 1)
                    {
                        ++count;
                        if (count > 2)
                        {
                            result -= count - 3;
                            count = 3;
                            set(new RuleNode(RuleType.Chow, current, result));

                            if (result <= 0)
                                return;

                            current += currentStep;
                            currentStep = nextStep;
                            nextStep = 1;
                        }

                        source = destination;
                    }
                    else if (source.number - destination.number == 1 || source.number == destination.number)
                        ++currentStep;
                    else
                        return;
                }
                
                isType = true;

                ++offset;
            }
        }*/

        /*public bool Win(IEnumerable<int> indices)
        {
            if (indices == null)
                return false;
            
            IEnumerable<int> result = this.winFlag;
            Tile source = new Tile(), destination;
            int length = 0, count = 0, eyeCount = 0, i;
            bool isEyes = false;
            foreach (int index in indices)
            {
                ++length;

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

            return length > 13;
        }*/

        /*public IEnumerable<int> Win(IEnumerable<int> indices)
        {
            IEnumerator<int> enumerator = indices == null ? null : indices.GetEnumerator();
            if (enumerator == null || !enumerator.MoveNext())
                return null;

            IEnumerable<int> result = this.winFlag;
            Tile source = Tile.Get(enumerator.Current), destination;
            int count = 1, previousCount = 0, currentCount = 0, nextCount = 1, temp;
            while(enumerator.MoveNext())
            {
                ++count;

                destination = Tile.Get(enumerator.Current);
                if (destination.type != source.type || destination.number - source.number > 1)
                {
                    temp = Math.Min(Math.Min(previousCount, currentCount), nextCount);
                    if (!__Check(temp, ref result))
                        return null;

                    previousCount -= temp;
                    if (previousCount > 0 && !__CheckEye(previousCount, ref result))
                        return null;

                    currentCount -= temp;
                    if (currentCount > 0 && !__CheckEye(currentCount, ref result))
                        return null;

                    nextCount -= temp;
                    if (nextCount > 0 && !__CheckEye(nextCount, ref result))
                        return null;

                    previousCount = 0;
                    currentCount = 0;
                    nextCount = 1;
                }
                else if (destination.number == source.number)
                    ++nextCount;
                else if (destination.number - source.number == 1)
                {
                    temp = Math.Min(Math.Min(previousCount, currentCount), nextCount);
                    if (!__Check(temp, ref result))
                        return null;

                    previousCount -= temp;
                    if (previousCount > 0 && !__CheckEye(previousCount, ref result))
                        return null;

                    previousCount = currentCount - temp;
                    currentCount = nextCount - temp;
                    nextCount = 1;
                }
                else
                    return null;
                
                source = destination;
            }

            temp = Math.Min(Math.Min(previousCount, currentCount), nextCount);
            if (!__Check(temp, ref result))
                return null;

            previousCount -= temp;
            if (previousCount > 0 && !__CheckEye(previousCount, ref result))
                return null;

            currentCount -= temp;
            if (currentCount > 0 && !__CheckEye(currentCount, ref result))
                return null;

            nextCount -= temp;
            if (nextCount > 0 && !__CheckEye(nextCount, ref result))
                return null;

            return count > 13 ? result : null;
        }*/

        private bool __Check(int index, int offset, int previousCount, int currentCount, int nextCount, Tile source, IEnumerator enumerator, ref IEnumerable<WinFlag> winFlags)
        {
            if (enumerator == null || !enumerator.MoveNext())
            {
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp = index;
                if (count > 0 && !Check(index, currentCount + offset, nextCount, count, ref winFlags))
                    return false;
                
                if (nextCount > count && !CheckEye(temp, nextCount - count, ref winFlags))
                    return false;

                if (currentCount > count && !CheckEye(temp -= nextCount, currentCount - count, ref winFlags))
                    return false;

                if (previousCount > count && !CheckEye(temp -= currentCount + offset, previousCount - count, ref winFlags))
                    return false;

                return true;
            }

            Tile destination = Tile.Get(enumerator.Current);
            if (destination.type != source.type || destination.number - source.number > 1)
            {
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp = index;
                if (count > 0 && !Check(index, currentCount + offset, nextCount, count, ref winFlags))
                    return false;

                if (nextCount > count && !CheckEye(temp, nextCount - count, ref winFlags))
                    return false;

                if (currentCount > count && !CheckEye(temp -= nextCount, currentCount - count, ref winFlags))
                    return false;
                
                if (previousCount > count && !CheckEye(temp -= currentCount + offset, previousCount - count, ref winFlags))
                    return false;

                offset = 0;
                previousCount = 0;
                currentCount = 0;
                nextCount = 1;
            }
            else if (destination.number == source.number)
                ++nextCount;
            else if (destination.number - source.number == 1)
            {
                IEnumerable<WinFlag> result = null, instance;
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp = index - nextCount - currentCount - offset;
                for (int i = 0; i <= count; ++i)
                {
                    instance = winFlags;
                     
                    if (i > 0 && !Check(index, currentCount + offset, nextCount, i, ref instance))
                        continue;

                    if (previousCount > i && !CheckEye(temp, previousCount - i, ref instance))
                        continue;

                    if (__Check(index + 1, i, currentCount - i, nextCount - i, 1, destination, enumerator.Clone(), ref instance))
                    {
                        if (result == null)
                            result = instance;
                        else
                            result = result.Union(instance);
                    }
                }

                if (result != null && result.Any())
                {
                    winFlags = result;

                    return true;
                }
                else
                    return false;
            }
            else
                return false;

            return __Check(index + 1, offset, previousCount, currentCount, nextCount, destination, enumerator, ref winFlags);
        }

        private bool __Check(
            int index, 
            int offset, 
            int previousCount, 
            int currentCount, 
            int nextCount, 
            Tile source, 
            Func<bool, int, int, IEnumerable<WinFlag>, bool> handler, 
            IEnumerator enumerator, 
            ref IEnumerable<WinFlag> winFlags)
        {
            if (enumerator == null || !enumerator.MoveNext())
            {
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp;
                IEnumerable<WinFlag> result;
                if ((source.number < 8 && Math.Min(Math.Min(previousCount, currentCount), nextCount + 1) > count) ||
                    (source.number > 0 && Math.Min(Math.Min(previousCount, currentCount + 1), nextCount) > count) ||
                    ((source.number > 1 || source.number < 8) && Math.Min(Math.Min(previousCount + 1, currentCount), nextCount) > count))
                {
                    result = winFlags;

                    ++count;
                    if ((Check(index, currentCount + offset, nextCount, count, ref result)) &&
                        (nextCount <= count || CheckEye(index, nextCount - count, ref result)) &&
                        (currentCount <= count || CheckEye(index - nextCount, currentCount - count, ref result)) &&
                        (previousCount <= count || CheckEye(index - nextCount - currentCount - offset, previousCount - count, ref result)))
                    {
                        temp = index;
                        if (nextCount < count && source.number < 8 && (handler == null || handler(false, 2, temp, result)))
                        {
                            winFlags = result;

                            return true;
                        }

                        temp -= nextCount;
                        if (currentCount < count && source.number > 0 && (handler == null || handler(false, 1, temp, result)))
                        {
                            winFlags = result;

                            return true;
                        }

                        temp -= currentCount + offset;
                        if (previousCount < count)
                        {
                            if (source.number > 1 && (handler == null || handler(false, 0, temp, result)))
                            {
                                winFlags = result;

                                return true;
                            }

                            if (source.number < 8 && (handler == null || handler(false, 2, temp, result)))
                            {
                                winFlags = result;

                                return true;
                            }
                        }
                    }

                    --count;
                }

                if (count > 0 && !Check(index, currentCount + offset, nextCount, count, ref winFlags))
                    return false;

                result = winFlags;
                temp = index;
                if (nextCount > count && !CheckEye(temp, nextCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, nextCount - count + 1, ref winFlags) &&
                        (currentCount <= count || CheckEye(temp - nextCount, currentCount - count, ref winFlags)) &&
                        ((previousCount <= count || CheckEye(temp - nextCount - currentCount - offset, previousCount - count, ref winFlags))) &&
                        (handler == null || handler(true, nextCount - count, temp, winFlags));
                }

                result = winFlags;
                temp -= nextCount;
                if (currentCount > count && !CheckEye(temp, currentCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, currentCount - count + 1, ref winFlags) &&
                        ((previousCount <= count || CheckEye(temp - currentCount - offset, previousCount - count, ref winFlags))) &&
                        (handler == null || handler(true, currentCount - count, temp, winFlags));
                }

                result = winFlags;
                temp -= currentCount + offset;
                if (previousCount > count && !CheckEye(temp, previousCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, previousCount - count + 1, ref winFlags) &&
                        (handler == null || handler(true, previousCount - count, temp, winFlags));
                }
                
                return false;
            }

            Tile destination = Tile.Get(enumerator.Current);
            if (destination.type != source.type || destination.number - source.number > 1)
            {
                IEnumerable<WinFlag> result;
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp = index - nextCount - currentCount - offset;
                if (destination.type == source.type && (destination.number - source.number) == 2)
                {
                    result = null;

                    IEnumerable<WinFlag> instance, target;
                    int length, i, j;
                    for (i = 0; i <= count; ++i)
                    {
                        instance = winFlags;

                        if (i > 0 && !Check(index, currentCount + offset, nextCount, i, ref instance))
                            continue;

                        if (previousCount > i && !CheckEye(temp, previousCount - i, ref instance))
                            continue;

                        length = Math.Min(Math.Min(currentCount - i, nextCount - i), 1);
                        for (j = 0; j <= length; ++j)
                        {
                            target = instance;
                            if (j > 0 && !Check(index, nextCount, 0, j, ref target))
                                continue;

                            if (currentCount > j && !CheckEye(index - 1 - nextCount, currentCount - i - j, ref target))
                                continue;

                            if (__Check(index + 1, j, nextCount - i - j, 1 - j, 1, destination, enumerator.Clone(), ref target))
                            {
                                if (result == null)
                                    result = target;
                                else
                                    result = result.Union(target);
                            }
                        }
                    }

                    if (result != null && result.Any() && (handler == null || handler(false, 1, index, result)))
                    {
                        winFlags = result;

                        return true;
                    }
                }

                if ((source.number < 8 && Math.Min(Math.Min(previousCount, currentCount), nextCount + 1) > count) ||
                    (source.number > 0 && Math.Min(Math.Min(previousCount, currentCount + 1), nextCount) > count) ||
                    ((source.number > 1 || source.number < 8) && Math.Min(Math.Min(previousCount + 1, currentCount), nextCount) > count))
                {
                    result = winFlags;

                    ++count;
                    if ((Check(index, currentCount + offset, nextCount, count, ref result)) &&
                        (nextCount <= count || CheckEye(index, nextCount - count, ref result)) &&
                        (currentCount <= count || CheckEye(index - nextCount, currentCount - count, ref result)) &&
                        (previousCount <= count || CheckEye(index - nextCount - currentCount - offset, previousCount - count, ref result)) &&
                        __Check(index + 1, 0, 0, 0, 1, destination, enumerator.Clone(), ref winFlags))
                    {
                        temp = index;
                        if (nextCount < count && source.number < 8 && (handler == null || handler(false, 2, temp, result)))
                        {
                            winFlags = result;

                            return true;
                        }

                        temp -= nextCount;
                        if (currentCount < count && source.number > 0 && (handler == null || handler(false, 1, temp, result)))
                        {
                            winFlags = result;

                            return true;
                        }

                        temp -= currentCount + offset;
                        if (previousCount < count)
                        {
                            if (source.number > 1 && (handler == null || handler(false, 0, temp, result)))
                            {
                                winFlags = result;

                                return true;
                            }

                            if (source.number < 8 && (handler == null || handler(false, 2, temp, result)))
                            {
                                winFlags = result;

                                return true;
                            }
                        }
                    }

                    --count;
                }
                
                if (count > 0 && !Check(index, currentCount + offset, nextCount, count, ref winFlags))
                    return false;

                result = winFlags;
                temp = index;
                if (nextCount > count && !CheckEye(temp, nextCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, nextCount - count + 1, ref winFlags) &&
                        (currentCount <= count || CheckEye(temp - nextCount, currentCount - count, ref winFlags)) &&
                        ((previousCount <= count || CheckEye(temp - nextCount - currentCount - offset, previousCount - count, ref winFlags))) &&
                        __Check(index + 1, 0, 0, 0, 1, destination, enumerator, ref winFlags) &&
                        (handler == null || handler(true, nextCount - count, temp, winFlags));
                }

                result = winFlags;
                temp -= nextCount;
                if (currentCount > count && !CheckEye(temp, currentCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, currentCount - count + 1, ref winFlags) &&
                        ((previousCount <= count || CheckEye(temp - currentCount - offset, previousCount - count, ref winFlags))) &&
                        __Check(index + 1, 0, 0, 0, 1, destination, enumerator, ref winFlags) &&
                        (handler == null || handler(true, currentCount - count, temp, winFlags));
                }

                result = winFlags;
                temp -= currentCount + offset;
                if (previousCount > count && !CheckEye(temp, previousCount - count, ref winFlags))
                {
                    winFlags = result;
                    return CheckEye(temp, previousCount - count + 1, ref winFlags) &&
                        __Check(index + 1, 0, 0, 0, 1, destination, enumerator, ref winFlags) &&
                        (handler == null || handler(true, previousCount - count, temp, winFlags));
                }

                offset = 0;
                previousCount = 0;
                currentCount = 0;
                nextCount = 1;
            }
            else if (destination.number == source.number)
                ++nextCount;
            else if (destination.number - source.number == 1)
            {
                IEnumerable<WinFlag> instance, target;
                int count = Math.Min(Math.Min(previousCount, currentCount), nextCount), temp = index - nextCount - currentCount - offset;
                for (int i = 0; i <= count; ++i)
                {
                    instance = winFlags;

                    if (i > 0 && !Check(index, currentCount + offset, nextCount, i, ref instance))
                        continue;

                    target = instance;
                    if (previousCount > i && !CheckEye(temp, previousCount - i, ref instance))
                    {
                        instance = target;
                        if (CheckEye(temp, previousCount - i + 1, ref instance) &&
                            __Check(index + 1, i, currentCount - i, nextCount - i, 1, destination, enumerator.Clone(), ref instance) &&
                            (handler == null || handler(true, previousCount - i, temp, instance)))
                        {
                            winFlags = instance;

                            return true;
                        }

                        continue;
                    }

                    if (__Check(index + 1, i, currentCount - i, nextCount - i, 1, destination, handler, enumerator.Clone(), ref instance))
                    {
                        winFlags = instance;

                        return true;
                    }
                }

                return false;
            }
            else
                return false;

            return __Check(index + 1, offset, previousCount, currentCount, nextCount, destination, handler, enumerator, ref winFlags);
        }
    }

    public class Player : IEnumerable<int>, IEnumerable<KeyValuePair<int, int>>
    {
        public enum DrawType
        {
            None, 
            Normal, 
            Flower
        }

        public struct Group
        {
            internal RuleType _type;

            internal int _playerIndex;

            internal LinkedListNode<int> _node0;
            internal LinkedListNode<int> _node1;
            internal LinkedListNode<int> _node2;
            internal LinkedListNode<int> _node3;
            
            public RuleType type
            {
                get
                {
                    return _type;
                }
            }

            public int playerIndex
            {
                get
                {
                    return _playerIndex;
                }
            }

            public int x
            {
                get
                {
                    return _node0 == null ? -1 : _node0.Value;
                }
            }

            public int y
            {
                get
                {
                    return _node1 == null ? -1 : _node1.Value;
                }
            }

            public int z
            {
                get
                {
                    return _node2 == null ? -1 : _node2.Value;
                }
            }

            public int w
            {
                get
                {
                    return _node3 == null ? -1 : _node3.Value;
                }
            }

            public Group(
                RuleType type, 
                int playerIndex, 
                LinkedListNode<int> node0, 
                LinkedListNode<int> node1, 
                LinkedListNode<int> node2, 
                LinkedListNode<int> node3)
            {
                _type = type;
                _playerIndex = playerIndex;
                _node0 = node0;
                _node1 = node1;
                _node2 = node2;
                _node3 = node3;
            }
        }

        public struct Enumerator : Rule.IEnumerator
        {
            private LinkedList<int> __list;
            private LinkedListNode<int> __instance;

            public int Current
            {
                get
                {
                    return __instance == null ? -1 : __instance.Value;
                }
            }

            public Enumerator(LinkedList<int> list)
            {
                __list = list;

                __instance = null;
            }

            public bool MoveNext()
            {
                if (__list == null)
                    return false;

                if (__instance == null)
                {
                    __instance = __list.First;

                    return true;
                }

                __instance = __instance.Next;

                return __instance != null;
            }

            public void Reset()
            {
                __instance = null;
            }

            public void Dispose()
            {
            }

            public Enumerator Clone()
            {
                Enumerator enumerator = new Enumerator(__list);
                enumerator.__instance = __instance;
                return enumerator;
            }

            Rule.IEnumerator Rule.IEnumerator.Clone()
            {
                return Clone();
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }
        }

        public struct Iterator : IEnumerator<KeyValuePair<int, int>>
        {
            private IEnumerator<KeyValuePair<int, LinkedListNode<int>>> __instance;

            public KeyValuePair<int, int> Current
            {
                get
                {
                    if (__instance == null)
                        return default(KeyValuePair<int, int>);

                    KeyValuePair<int, LinkedListNode<int>> pair = __instance.Current;
                    LinkedListNode<int> node = pair.Value;
                    return new KeyValuePair<int, int>(pair.Key, node == null ? -1 : node.Value);
                }
            }

            public Iterator(IEnumerator<KeyValuePair<int, LinkedListNode<int>>> instance)
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

        private bool __isReady;
        private DrawType __drawType;
        private int __index;
        private int __score;
        private int __poolTileIndex;
        private int __handleTileIndex;
        private int __kongPlayerIndex;
        private int __kongCount;
        private int __flowerCount;
        private Mahjong __mahjong;
        private Pool<int> __poolTileIndices;
        private Pool<LinkedListNode<int>> __handIndices;
        private LinkedList<int> __handTileIndices;
        private List<RuleNode> __ruleNodes;
        private List<IEnumerable<Rule.WinFlag>> __winFlags;
        private List<Group> __groups; 

        public bool isReady
        {
            get
            {
                return __isReady;
            }
        }

        public DrawType drawType
        {
            get
            {
                return __drawType;
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

#endif
                if(__mahjong.__players != null && __index >= 0 && __index < 4)
                    __mahjong.__players[__index] = null;

                if (value >= 0 && value < 4)
                {
                    if (__mahjong.__players == null || __mahjong.__players.Length < 4)
                        __mahjong.__players = new Player[4];

                    __mahjong.__players[value] = this;
                }

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

        public int count
        {
            get
            {
                return __handTileIndices == null ? 0 : __handTileIndices.Count;
            }
        }

        public int handTileIndexCount
        {
            get
            {
                return count + groupCount * 3 + __kongCount;
            }
        }

        public int poolTileIndexCount
        {
            get
            {
                return __poolTileIndices == null ? 0 : __poolTileIndices.Count;
            }
        }

        public int groupCount
        {
            get
            {
                return __groups == null ? 0 : __groups.Count;
            }
        }

        public int kongCount
        {
            get
            {
                return __kongCount;
            }
        }

        public int flowerCount
        {
            get
            {
                return __flowerCount;
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
                LinkedList<int> temp = __handTileIndices;

                __handTileIndices = new LinkedList<int>(__handTileIndices);

                if (__groups != null)
                {
                    LinkedListNode<int> node;
                    foreach (Group group in __groups)
                    {
                        node = group._node0;
                        if(node != null)
                            __Add(new LinkedListNode<int>(node.Value));

                        node = group._node1;
                        if (node != null)
                            __Add(new LinkedListNode<int>(node.Value));

                        node = group._node2;
                        if (node != null)
                            __Add(new LinkedListNode<int>(node.Value));

                        node = group._node3;
                        if (node != null)
                            __Add(new LinkedListNode<int>(node.Value));
                    }
                }

                LinkedList<int> result = __handTileIndices;

                __handTileIndices = temp;

                return result;
            }
        }
        
        public IEnumerable<int> poolTileIndices
        {
            get
            {
                return __poolTileIndices;
            }
        }

        public IEnumerable<RuleNode> ruleNodes
        {
            get
            {
                return __ruleNodes;
            }
        }

        public IEnumerable<Group> groups
        {
            get
            {
                return __groups;
            }
        }

        public IEnumerable<Rule.WinFlag> winFlags
        {
            get
            {
                if (__mahjong == null || __mahjong.rule == null)
                    return null;

                IEnumerable<Rule.WinFlag> winFlags = __mahjong.rule.winFlags;
                if (winFlags == null)
                    return null;

                if (__groups != null)
                {
                    int mask, shift;
                    foreach (Group group in __groups)
                    {
                        mask = __mahjong.rule.GetMask(group._type, out shift);
                        if (mask > 0)
                        {
                            winFlags = from winFlag in winFlags
                                       where ((winFlag.instance >> shift) & mask) > 0
                                       select new Rule.WinFlag(winFlag.index, winFlag.instance - (1 << shift));

                            if (!winFlags.Any())
                                return null;
                        }
                        else
                            return null;
                    }
                }

                return winFlags;
            }
        }

        public Player(Mahjong mahjong)
        {
            __mahjong = mahjong;
            __index = -1;
            __poolTileIndex = -1;
            __handleTileIndex = -1;
        }

        public virtual int Score(RuleType type, int playerIndex, int tileIndex, IEnumerable<Rule.WinFlag> winFlags)
        {
            return 0;
        }

        public int GetHandTileIndex(int index)
        {
            LinkedListNode<int> node = __Get(index);

            return node == null ? -1 : node.Value;
        }

        public bool Contains(int index)
        {
            return __handIndices != null && __handIndices.ContainsKey(index);
        }

        public bool Check(Func<bool, int, int, IEnumerable<Rule.WinFlag>, bool> handler)
        {
            if (handTileIndexCount != 13 + __kongCount)
                return false;
            
            return __mahjong.rule.Check(GetEnumerator(), winFlags, handler);
        }
        
        public IEnumerable<Rule.WinFlag> Check()
        {
            if (handTileIndexCount != 14 + __kongCount)
                return null;
            
            IEnumerable<Rule.WinFlag> winFlags = this.winFlags;
            if (__mahjong.rule.Check(new Enumerator(__handTileIndices), ref winFlags))
                return winFlags;

            return null;
        }

        public IEnumerable<Rule.WinFlag> Check(int index)
        {
            if (__mahjong == null || __mahjong.rule == null)
                return null;
            
            LinkedListNode<int> node = new LinkedListNode<int>(index);
            if (!__Add(node))
                return null;

            IEnumerable<Rule.WinFlag> result = Check();
            if (__handTileIndices != null)
                __handTileIndices.Remove(node);

            return result;
        }

        public void Reset()
        {
            __isReady = false;

            __drawType = DrawType.None;

            __poolTileIndex = -1;
            __handleTileIndex = -1;
            __kongPlayerIndex = -1;
            __kongCount = 0;
            __flowerCount = 0;

            if (__poolTileIndices != null)
                __poolTileIndices.Clear();

            if (__handIndices != null)
                __handIndices.Clear();

            if (__handTileIndices != null)
                __handTileIndices.Clear();
            
            if (__ruleNodes != null)
                __ruleNodes.Clear();

            if (__winFlags != null)
                __winFlags.Clear();

            if (__groups != null)
                __groups.Clear();
        }

        public bool Ready()
        {
            if (__isReady)
                return true;

            if(Check(null))
            {
                __isReady = true;

                return true;
            }

            return false;
        }

        public bool Draw(Action<int> add, Action<int> remove)
        {
            if (__mahjong == null || __drawType != DrawType.None)
                return false;

            if (__mahjong.__players == null || __mahjong.__players[__mahjong.__playerIndex] != this)
                return false;
            
            int handTileIndexCount = this.handTileIndexCount;
            if (handTileIndexCount > (13 + __kongCount))
                return false;
            
            int tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
            if (__mahjong.__tileCount >= tileCount)
                return false;
            
            LinkedListNode<int> node;
            int index;
            if (handTileIndexCount < 9)
            {
                for (int i = 0; i < 4; ++i)
                {
                    ++__mahjong.__tileCount;

                    if (__mahjong.__tileIndex >= tileCount)
                        __mahjong.__tileIndex = 0;

                    if (__handIndices == null)
                        __handIndices = new Pool<LinkedListNode<int>>();

                    index = __handIndices.Add(new LinkedListNode<int>(__mahjong.__tileIndices[__mahjong.__tileIndex++]));
                    this.temp[i] = index;

                    if (add != null)
                        add(index);
                }

                if (__handIndices != null)
                {
                    foreach (int i in this.temp)
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

                                        ++__flowerCount;

                                        if (__poolTileIndices == null)
                                            __poolTileIndices = new Pool<int>();

                                        __poolTileIndices.Add(node.Value);

                                        ++__mahjong.__tileCount;

                                        if (__mahjong.__tileIndex >= tileCount)
                                            __mahjong.__tileIndex = 0;

                                        node.Value = __mahjong.__tileIndices[__mahjong.__tileIndex++];

                                        if (add != null)
                                            add(i);

                                        continue;
                                }

                                if (!__Add(node))
                                    return false;

                                break;
                            }
                        }
                    }
                }
                
                __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;

                return true;
            }

            bool isFlower = false;
            int target = -1;
            node = null;
            while (__mahjong.__tileCount < tileCount)
            {
                ++__mahjong.__tileCount;

                if (__mahjong.__tileIndex >= tileCount)
                    __mahjong.__tileIndex = 0;

                index = __mahjong.__tileIndices[__mahjong.__tileIndex++];
                if (node == null)
                {
                    node = new LinkedListNode<int>(index);

                    if (__handIndices == null)
                        __handIndices = new Pool<LinkedListNode<int>>();

                    target = __handIndices.Add(node);
                }
                else
                    node.Value = index;

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
                        if (add != null)
                            add(target);

                        if (remove != null)
                            remove(target);

                        ++__flowerCount;

                        if (__poolTileIndices == null)
                            __poolTileIndices = new Pool<int>();

                        __poolTileIndices.Add(index);

                        isFlower = true;

                        break;
                    default:
                        if (__Add(node))
                        {
                            if (handTileIndexCount < (13 + __kongCount))
                                __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;
                            else
                            {
                                __Clear();

                                __drawType = isFlower ? DrawType.Flower : DrawType.Normal;
                            }

                            __handleTileIndex = target;

                            if (add != null)
                                add(__handleTileIndex);

                            return true;
                        }

                        break;
                }
            }

            return false;
        }

        public bool Discard(int index)
        {
            if (__mahjong == null || __drawType == DrawType.None)
                return false;

            if (handTileIndexCount != 14 + __kongCount)
                return false;

            LinkedListNode<int> node = __Remove(index);
            if (node == null)
                return false;

            __drawType = DrawType.None;
            __mahjong.__playerIndex = (__mahjong.__playerIndex + 1) & 3;

            if (__poolTileIndices == null)
                __poolTileIndices = new Pool<int>();

            __poolTileIndex = __poolTileIndices.Add(node.Value);

            __handleTileIndex = -1;

            __kongPlayerIndex = -1;

            __Clear();

            return true;
        }

        public ReadOnlyCollection<RuleNode> Start()
        {
            if (__mahjong == null || 
                __mahjong.rule == null || 
                __mahjong.__players == null || 
                __mahjong.__players.Length <= __mahjong.__playerIndex || 
                __mahjong.__tileCount < 47)
                return null;

            Player player = __mahjong.__players[__mahjong.__playerIndex];
            if (player == this)
            {
                if (__drawType == DrawType.None)
                {
                    player = __mahjong.__players[(__mahjong.__playerIndex + 3) & 3];
                    if (player != null && player.__poolTileIndices != null)
                    {
                        int tileIndex;
                        if (player.__poolTileIndices.TryGetValue(player.__poolTileIndex, out tileIndex))
                        {
                            if (!__isReady)
                            {
                                __mahjong.rule.Check(this, tileIndex, delegate (RuleNode ruleNode)
                                {
                                    if (ruleNode.type == RuleType.Kong)
                                        ruleNode.type = RuleType.MeldedKong;

                                    __Add(ruleNode);
                                });
                            }

                            IEnumerable<Rule.WinFlag> winFlags = Check(tileIndex);
                            if (winFlags != null)
                                __Add(new RuleNode(RuleType.Win, __Add(winFlags), 0));
                        }
                    }
                    else
                        return null;
                }
                else
                {
                    if (!__isReady && __handIndices != null)
                    {
                        LinkedListNode<int> node;
                        if (__handIndices.TryGetValue(__handleTileIndex, out node) && node != null)
                        {
                            int tileIndex = node.Value;

                            LinkedListNode<int> temp = node.Previous;
                            if (__handTileIndices != null)
                                __handTileIndices.Remove(node);

                            __mahjong.rule.Check(this, tileIndex, delegate (RuleNode ruleNode)
                            {
                                if (ruleNode.type != RuleType.Kong)
                                    return;

                                ruleNode.type = RuleType.HiddenKong;

                                __Add(ruleNode);
                            });

                            if (__handTileIndices != null)
                            {
                                if (temp == null)
                                    __handTileIndices.AddFirst(node);
                                else
                                    __handTileIndices.AddAfter(temp, node);
                            }

                            if (__groups != null)
                            {
                                int index = 0;
                                foreach (Group group in __groups)
                                {
                                    if (group._type == RuleType.Pong &&
                                        group._node0 != null &&
                                        group._node0.Value == tileIndex &&
                                        group._node1 != null &&
                                        group._node1.Value == tileIndex &&
                                        group._node2 != null &&
                                        group._node2.Value == tileIndex &&
                                        group._node3 == null)
                                    {
                                        __Add(new RuleNode(RuleType.Kong, index, 3));

                                        break;
                                    }

                                    ++index;
                                }
                            }
                        }
                    }

                    IEnumerable<Rule.WinFlag> winFlags = Check();
                    if (winFlags != null)
                        __Add(new RuleNode(__kongPlayerIndex >= 0 && __kongPlayerIndex < 4 ? RuleType.OverKong : RuleType.SelfDraw, __Add(winFlags), 0));
                }
            }
            else
            {
                if (player == null)
                    return null;

                if (player.__drawType == DrawType.None)
                {
                    player = __mahjong.__players[(__mahjong.__playerIndex + 3) & 3];
                    if (player == this)
                        return null;

                    if (player != null && player.__poolTileIndices != null)
                    {
                        int tileIndex;
                        if (player.__poolTileIndices.TryGetValue(player.__poolTileIndex, out tileIndex))
                        {
                            if (!__isReady)
                            {
                                __mahjong.rule.Check(this, tileIndex, delegate (RuleNode ruleNode)
                                {
                                    if (ruleNode.type == RuleType.Chow)
                                        return;

                                    if (ruleNode.type == RuleType.Kong)
                                        ruleNode.type = RuleType.MeldedKong;

                                    __Add(ruleNode);
                                });
                            }

                            IEnumerable<Rule.WinFlag> winFlags = Check(tileIndex);
                            if (winFlags != null)
                                __Add(new RuleNode(RuleType.Win, __Add(winFlags), 0));
                        }
                    }
                }
                else
                {
                    if (__mahjong.__rulePlayerIndex == __mahjong.__playerIndex && __mahjong.__ruleType == RuleType.Kong)
                    {
                        IEnumerable<Rule.WinFlag> winFlags = Check(player.GetHandTileIndex(player.__handleTileIndex));
                        if(winFlags != null)
                            __Add(new RuleNode(RuleType.BreakKong, __Add(winFlags), 0));
                    }
                }
            }

            return __ruleNodes == null ? null : __ruleNodes.AsReadOnly();
        }
        
        public RuleNode Get(int index)
        {
            int count = __ruleNodes == null ? 0 : __ruleNodes.Count;
            return index < count ? __ruleNodes[index] : new RuleNode(RuleType.Unknown, 0, 0);
        }
        
        public bool Try(int index)
        {
            if (__mahjong == null)
                return false;
            
            RuleType ruleType = Get(index).type;
            switch (ruleType)
            {
                case RuleType.Chow:
                    if (__mahjong.__ruleType == RuleType.Unknown)
                    {
                        __mahjong.__rulePlayerIndex = __index;
                        __mahjong.__ruleNodeIndex = index;
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
                            __mahjong.__ruleNodeIndex = index;
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
                            __mahjong.__ruleNodeIndex = index;
                            __mahjong.__ruleType = ruleType;

                            return true;
                        default:
                            break;
                    }
                    break;
                case RuleType.Win:
                case RuleType.SelfDraw:
                case RuleType.BreakKong:
                case RuleType.OverKong:
                    __mahjong.__rulePlayerIndex = __index;
                    __mahjong.__ruleNodeIndex = index;
                    __mahjong.__ruleType = ruleType;

                    return true;
                default:
                    break;
            }

            return false;
        }
        
        public int End(int index, Action<int> handler, out int playerIndex)
        {
            playerIndex = -1;
            if (__mahjong == null)
                return -1;
            
            if (index < 0 || __ruleNodes == null ||
                __ruleNodes.Count <= index)
                return -1;

            RuleNode ruleNode = __ruleNodes[index];
            if (ruleNode.index < 0)
                return -1;
            
            int temp, count, i;
            Player player;
            LinkedListNode<int> node, node1, node2, instance;
            switch (ruleNode.type)
            {
                case RuleType.Chow:
                case RuleType.Pong:
                    if (__handIndices == null || __mahjong.__players == null)
                        return -1;

                    playerIndex = (__mahjong.playerIndex + 3) & 3;
                    if (playerIndex < 0 || __mahjong.__players.Length <= playerIndex)
                        return -1;

                    player = __mahjong.__players[playerIndex];
                    if (player == null || player.__poolTileIndices == null || !player.__poolTileIndices.TryGetValue(player.__poolTileIndex, out temp))
                        return -1;
                    
                    if (!__handIndices.TryGetValue(ruleNode.index, out node) || node == null)
                        return -1;

                    if (ruleNode.type == RuleType.Chow)
                    {
                        byte code = Tile.Get(temp), target = Tile.Get(node.Value);
                        while (target == code)
                        {
                            node = node.Next;
                            if (node == null)
                                return -1;

                            target = Tile.Get(node.Value);
                        }
                        
                        node1 = node;
                        do
                        {
                            node1 = node1.Next;
                            if (node1 == null)
                                return -1;
                        } while (Tile.Get(node1.Value) == target);

                        node1 = node1.Previous;

                        do
                        {
                            node1 = node1.Next;
                            if (node1 == null)
                                return -1;
                        } while (Tile.Get(node1.Value) == code);
                    }
                    else
                    {
                        node1 = node.Next;
                        if (node1 == null)
                            return -1;
                    }
                    
                    if (handler != null)
                        handler(ruleNode.index);

                    if (__Remove(ruleNode.index) != node)
                        return -1;

                    i = __handIndices.IndexOf(node1);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node1)
                        return -1;
                    
                    if (__groups == null)
                        __groups = new List<Group>();

                    __groups.Add(new Group(ruleNode.type, playerIndex, node, node1, new LinkedListNode<int>(temp), null));

                    player.__poolTileIndices.RemoveAt(player.__poolTileIndex);

                    player.__poolTileIndex = -1;

                    __mahjong.__playerIndex = __index;

                    __drawType = DrawType.Normal;

                    __Clear();
                    
                    return __groups.Count - 1;
                case RuleType.Kong:
                    if (__groups == null || __groups.Count <= ruleNode.index)
                        return -1;

                    if (handler != null)
                        handler(__handleTileIndex);

                    node = __Remove(__handleTileIndex);
                    if (node != null)
                        return -1;

                    Group group = __groups[ruleNode.index];
                    group._type = RuleType.MeldedKong;
                    group._node3 = node;

                    __groups[ruleNode.index] = group;

                    ++__kongCount;

                    __handleTileIndex = -1;

                    __kongPlayerIndex = -1;

                    __drawType = DrawType.None;

                    __Clear();
                    
                    playerIndex = __index;

                    return ruleNode.index;

                case RuleType.HiddenKong:
                    if (__handIndices == null)
                        return -1;

                    i = ruleNode.index;
                    node = __handTileIndices == null ? null : __handTileIndices.First;
                    while (node != null)
                    {
                        if (--i < 0)
                            break;

                        node = node.Next;
                    }
                    
                    if (node == null)
                        return -1;
                    
                    node1 = node.Next;
                    if (node1 == null)
                        return -1;

                    node2 = node1.Next;
                    if (node2 == null)
                        return -1;

                    i = __handIndices.IndexOf(node);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node)
                        return -1;

                    i = __handIndices.IndexOf(node1);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node1)
                        return -1;

                    i = __handIndices.IndexOf(node2);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node2)
                        return -1;
                    
                    if (handler != null)
                        handler(__handleTileIndex);

                    instance = __Remove(__handleTileIndex);
                    if (instance == null)
                        return -1;

                    if (__groups == null)
                        __groups = new List<Group>();

                    __groups.Add(new Group(RuleType.HiddenKong, __index, node, node1, node2, instance));

                    ++__kongCount;

                    __handleTileIndex = -1;

                    __kongPlayerIndex = -1;

                    __drawType = DrawType.None;

                    __Clear();
                    
                    playerIndex = __index;

                    return __groups.Count - 1;
                    
                case RuleType.MeldedKong:
                    if (__handIndices == null || __mahjong.__players == null)
                        return -1;

                    playerIndex = (__mahjong.playerIndex + 3) & 3;
                    if (playerIndex < 0 || __mahjong.__players.Length <= playerIndex)
                        return -1;
                    
                    player = __mahjong.__players[playerIndex];
                    if (player == null || player.__poolTileIndices == null || !player.__poolTileIndices.TryGetValue(player.__poolTileIndex, out temp))
                        return -1;

                    i = ruleNode.index;
                    node = __handTileIndices == null ? null : __handTileIndices.First;
                    while (node != null)
                    {
                        if (--i < 0)
                            break;

                        node = node.Next;
                    }
                    
                    if (node == null)
                        return -1;

                    node1 = node.Next;
                    if (node1 == null)
                        return -1;

                    node2 = node1.Next;
                    if (node2 == null)
                        return -1;

                    i = __handIndices.IndexOf(node);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node)
                        return -1;

                    i = __handIndices.IndexOf(node1);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node1)
                        return -1;

                    i = __handIndices.IndexOf(node2);

                    if (handler != null)
                        handler(i);

                    if (__Remove(i) != node2)
                        return -1;
                    
                    if (__groups == null)
                        __groups = new List<Group>();

                    __groups.Add(new Group(RuleType.Kong, playerIndex, node, node1, node2, new LinkedListNode<int>(temp)));

                    player.__poolTileIndices.RemoveAt(player.__poolTileIndex);

                    player.__poolTileIndex = -1;

                    __mahjong.__playerIndex = __index;

                    ++__kongCount;

                    __kongPlayerIndex = playerIndex;

                    __Clear();
                    
                    return __groups.Count - 1;
                case RuleType.Win:
                    if (ruleNode.index < 0 || __winFlags == null || __winFlags.Count < ruleNode.index)
                        return -1;

                    if (__mahjong.__players == null)
                        return -1;

                    playerIndex = (__mahjong.playerIndex + 3) & 3;
                    if (playerIndex < 0 || __mahjong.__players.Length <= playerIndex)
                        return -1;

                    player = __mahjong.__players[playerIndex];
                    if (player == null)
                        return -1;

                    int tileIndex;
                    if (player.__poolTileIndices == null || !player.__poolTileIndices.TryGetValue(player.__poolTileIndex, out tileIndex))
                        return -1;

                    instance = new LinkedListNode<int>(tileIndex);
                    __Add(instance);
                    temp = Score(RuleType.Win, playerIndex, tileIndex, __winFlags[ruleNode.index]);
                    if (__handTileIndices != null)
                        __handTileIndices.Remove(instance);

                    __drawType = DrawType.Normal;
                    __score += temp;

                    int score = temp;

                    count = playerIndex + 4;
                    IEnumerable<Rule.WinFlag> winFlags;
                    for (i = __index; i < count; ++i)
                    {
                        player = __mahjong.__players[i & 3];
                        winFlags = player == null ? null : player.Check(tileIndex);
                        if (winFlags != null)
                        {
                            player.__Add(instance);
                            temp = player.Score(RuleType.Win, playerIndex, tileIndex, winFlags);
                            if (player.__handTileIndices != null)
                                player.__handTileIndices.Remove(instance);

                            player.__drawType = DrawType.Normal;
                            player.__score += temp;

                            score += temp;
                        }
                    }

                    player.__score -= score;
                    player.__poolTileIndex = -1;

                    __Clear();

                    __mahjong.__tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
                    __mahjong.__dealerIndex = __index;
                    __mahjong.__playerIndex = -1;

                    return -1;
                case RuleType.SelfDraw:
                    if (ruleNode.index < 0 || __winFlags == null || __winFlags.Count < ruleNode.index)
                        return -1;

                    temp = Score(RuleType.SelfDraw, __index, GetHandTileIndex(__handleTileIndex), __winFlags[ruleNode.index]);
                    __score += temp;

                    count = Math.Min(4, __mahjong.__players == null ? 0 : __mahjong.__players.Length);
                    for(i = 1; i < count; ++i)
                    {
                        player = __mahjong.__players[(__index + i) & 3];
                        if (player != null)
                            player.__score -= temp;
                    }

                    __handleTileIndex = -1;

                    __Clear();

                    __mahjong.__tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
                    __mahjong.__dealerIndex = __index;
                    __mahjong.__playerIndex = -1;

                    playerIndex = __index;
                    
                    return -1;
                case RuleType.BreakKong:
                    if (ruleNode.index < 0 || __winFlags == null || __winFlags.Count < ruleNode.index)
                        return -1;

                    playerIndex = __mahjong.playerIndex;
                    if (playerIndex < 0 || __mahjong.__players == null || __mahjong.__players.Length <= playerIndex)
                        return -1;

                    player = __mahjong.__players[playerIndex];
                    if (player == null)
                        return -1;

                    instance = new LinkedListNode<int>(player.GetHandTileIndex(player.__handleTileIndex));
                    __Add(instance);
                    temp = Score(RuleType.BreakKong, playerIndex, instance.Value, __winFlags[ruleNode.index]);
                    if (__handTileIndices != null)
                        __handTileIndices.Remove(instance);

                    __score += temp;
                    
                    player.__score -= temp;
                    player.__handleTileIndex = -1;

                    __drawType = DrawType.Normal;

                    __Clear();

                    __mahjong.__tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
                    __mahjong.__dealerIndex = __index;
                    __mahjong.__playerIndex = -1;

                    return -1;
                case RuleType.OverKong:
                    if (ruleNode.index < 0 || __winFlags == null || __winFlags.Count < ruleNode.index)
                        return -1;
                    
                    temp = Score(RuleType.OverKong, __kongPlayerIndex, GetHandTileIndex(__handleTileIndex), __winFlags[ruleNode.index]);
                    __score += temp;

                    if (__mahjong.__players != null &&
                    __mahjong.__players.Length > __kongPlayerIndex)
                    {
                        player = __mahjong.__players[__kongPlayerIndex];
                        if (player != null)
                            player.__score -= temp;
                    }
                    
                    playerIndex = __kongPlayerIndex;

                    __kongPlayerIndex = -1;

                    __Clear();

                    __mahjong.__tileCount = __mahjong.__tileIndices == null ? 0 : __mahjong.__tileIndices.Length;
                    __mahjong.__dealerIndex = __index;
                    __mahjong.__playerIndex = -1;

                    return -1;
            }
            
            return -1;
        }

        public IEnumerator<int> GetChecker(Func<bool, int, int, IEnumerable<Rule.WinFlag>, bool> handler)
        {
            if (__drawType == DrawType.None || handTileIndexCount != 14 + __kongCount)
                yield break;

            if (__handIndices == null || __handTileIndices == null)
                yield break;

            bool result;
            LinkedListNode<int> node, temp;
            foreach (KeyValuePair<int, LinkedListNode<int>> pair in (IEnumerable<KeyValuePair<int, LinkedListNode<int>>>)__handIndices)
            {
                node = pair.Value;
                if (node == null)
                    continue;

                temp = node.Previous;
                __handTileIndices.Remove(node);
                result = Check(handler);
                if (temp == null)
                    __handTileIndices.AddFirst(node);
                else
                    __handTileIndices.AddAfter(temp, node);

                if(result)
                    yield return pair.Key;
            }
        }

        public Iterator GetIterator()
        {
            return new Iterator(__handIndices == null ? null : ((IEnumerable<KeyValuePair<int, LinkedListNode<int>>>)__handIndices).GetEnumerator());
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(__handTileIndices);
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<int, int>> IEnumerable<KeyValuePair<int, int>>.GetEnumerator()
        {
            return GetIterator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private LinkedListNode<int> __Get(int index)
        {
            if (__handIndices == null)
                return null;

            LinkedListNode<int> node;
            if (__handIndices.TryGetValue(index, out node))
                return node;

            return null;
        }
        
        private LinkedListNode<int> __Remove(int index)
        {
            if (__handIndices == null)
                return null;

            LinkedListNode<int> node;
            if (!__handIndices.TryGetValue(index, out node) || node == null || !__handIndices.RemoveAt(index))
                return null;

            if (__handTileIndices != null)
                __handTileIndices.Remove(node);

            return node;
        }
        
        private bool __Add(LinkedListNode<int> node)
        {
            if (node == null)
                return false;

            if (__handTileIndices == null)
                __handTileIndices = new LinkedList<int>();

            int index = node.Value;
            for (LinkedListNode<int> temp = __handTileIndices.First; temp != null; temp = temp.Next)
            {
                if (temp.Value > index)
                {
                    __handTileIndices.AddBefore(temp, node);

                    return true;
                }
            }

            __handTileIndices.AddLast(node);

            return true;
        }
        
        private void __Add(RuleNode ruleNode)
        {
            if (__mahjong == null)
                return;

            int index;
            LinkedListNode<int> node;
            switch (ruleNode.type)
            {
                case RuleType.Chow:
                    if (__mahjong.__ruleType != RuleType.Unknown)
                        return;
                    
                    if (__handIndices == null)
                        return;

                    index = ruleNode.index;
                    node = __handTileIndices == null ? null : __handTileIndices.First;
                    while (node != null)
                    {
                        if (--index < 0)
                            break;

                        node = node.Next;
                    }

                    ruleNode.index = __handIndices.IndexOf(node);
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

                    if (__handIndices == null)
                        return;

                    index = ruleNode.index;
                    node = __handTileIndices == null ? null : __handTileIndices.First;
                    while (node != null)
                    {
                        if (--index < 0)
                            break;

                        node = node.Next;
                    }

                    ruleNode.index = __handIndices.IndexOf(node);
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
                case RuleType.SelfDraw:
                case RuleType.BreakKong:
                case RuleType.OverKong:
                    break;
                default:
                    return;
            }
            
            if (__ruleNodes == null)
                __ruleNodes = new List<RuleNode>();

            __ruleNodes.Add(ruleNode);
        }

        private int __Add(IEnumerable<Rule.WinFlag> winFlags)
        {
            if (__winFlags == null)
                __winFlags = new List<IEnumerable<Rule.WinFlag>>();

            __winFlags.Add(winFlags);

            return __winFlags.Count - 1;
        }

        private void __Clear()
        {
            if (__mahjong == null)
                return;

            if(__mahjong.__players != null)
            {
                foreach(Player player in __mahjong.__players)
                {
                    if (player != null)
                    {
                        if (player.__ruleNodes != null)
                            player.__ruleNodes.Clear();

                        if (player.__winFlags != null)
                            player.__winFlags.Clear();
                    }
                }
            }

            __mahjong.__rulePlayerIndex = __mahjong.__playerIndex;
            __mahjong.__ruleNodeIndex = -1;
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
    private int __ruleNodeIndex;
    private ShuffleType __shuffleType;
    private RuleType __ruleType;
    public Rule rule;

    public int count
    {
        get
        {
            return __tileIndices == null ? 0 : __tileIndices.Length;
        }
    }
    
    public int tileCount
    {
        get
        {
            return __tileCount;
        }
    }
    
    public int tileIndex
    {
        get
        {
            return __tileIndex;
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

    public int ruleNodeIndex
    {
        get
        {
            return __ruleNodeIndex;
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

    public void Shuffle(ShuffleType type, out int point0, out int point1, out int point2, out int point3)
    {
        /*__tileIndices = new int[]
        {
            //round 0
            //0
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 1) << 2,

            //1
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //2
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //3
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //round 1
            //0
            new Tile(TileType.Dots, 2) << 2,
            new Tile(TileType.Dots, 3) << 2,
            new Tile(TileType.Dots, 4) << 2,
            new Tile(TileType.Dots, 5) << 2,

            //1
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //2
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //3
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //round 2
            //0
            new Tile(TileType.Dots, 6) << 2,
            new Tile(TileType.Dots, 7) << 2,
            new Tile(TileType.Dots, 8) << 2,
            new Tile(TileType.Bomboo, 0) << 2,

            //1
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //2
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //3
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,
            new Tile(TileType.Dots, 0) << 2,

            //0
            new Tile(TileType.Bomboo, 1) << 2,
            //1
            new Tile(TileType.Dots, 2) << 2,
            //2
            new Tile(TileType.Dots, 2) << 2,
            //3
            new Tile(TileType.Dots, 2) << 2,

            //0
            new Tile(TileType.Bomboo, 2) << 2,
            //1
            new Tile(TileType.Dots, 2) << 2,
            //2
            new Tile(TileType.Dots, 2) << 2,
            //3
            new Tile(TileType.Dots, 2) << 2,

            //0
            new Tile(TileType.Bomboo, 3) << 2,
            //1
            new Tile(TileType.Dots, 2) << 2,
            //2
            new Tile(TileType.Dots, 2) << 2,
            //3
            new Tile(TileType.Dots, 2) << 2,
        };

        point0 = 0;
        point1 = 0;
        point2 = 0;
        point3 = 0;

        __tileIndex = 0;
        __tileCount = 0;

        __dealerIndex = 0;// random.Next(0, 3);
        __playerIndex = __dealerIndex;

        return;*/

        int poolTileCount = (int)TileType.Easet << 2, handTileCount = (poolTileCount + 4) >> 3, count;
        if ((type & ShuffleType.Winds) != 0)
        {
            count = TileType.Spring - TileType.Easet;
            poolTileCount += count << 2;
            handTileCount += count >> 1;
        }

        if ((type & ShuffleType.Flowers) != 0)
        {
            count = TileType.Unknown - TileType.Spring;
            poolTileCount += count;
            handTileCount += count >> 3;
        }
        
        int index;
        if (__tileIndices == null || __shuffleType != type)
        {
            if((__tileIndices == null ? 0 : __tileIndices.Length) != poolTileCount)
                __tileIndices = new int[poolTileCount];

            int i;
            index = 0;
            count = (int)TileType.Easet << 2;
            for (i = 0; i < count; ++i)
                __tileIndices[i] = index++;

            count = (TileType.Spring - TileType.Easet) << 2;
            if ((type & ShuffleType.Winds) == 0)
                index += count;
            else
            {
                for(count += i; i < count; ++i)
                    __tileIndices[i] = index++;
            }

            count = TileType.Unknown - TileType.Spring;
            if ((type & ShuffleType.Flowers) == 0)
                index += count;
            else
            {
                for (count += i; i < count; ++i)
                    __tileIndices[i] = index++;
            }

            __shuffleType = type;
        }

        Random random = new Random();
        count = poolTileCount - 1;
        int temp;
        for (int i = 0; i < count; ++i)
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

        count = point0 + point1;
        
        __tileIndex = (((count + 1) & 3) * handTileCount + count + point2 + point3) << 1;
        __tileCount = 0;

        //__dealerIndex = 0;// random.Next(0, 3);
        __playerIndex = __dealerIndex;

        if(__players != null)
        {
            foreach(Player player in __players)
            {
                if (player != null)
                    player.Reset();
            }
        }
    }
}
