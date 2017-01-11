using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;
using ZG;
using ZG.Network.Lobby;

public class MahjongServer : Server
{
    private struct Player
    {
        public string roomName;
        public int index;

        public Player(string roomName, int index)
        {
            this.roomName = roomName;
            this.index = index;
        }
    }

    private class Room
    {
        public class Rule : Mahjong.Rule
        {
            private IEnumerable<WinFlag> __winFlags;

            public override IEnumerable<WinFlag> winFlags
            {
                get
                {
                    if(__winFlags == null)
                        __winFlags = base.winFlags;

                    return __winFlags;
                }
            }

            public IEnumerable<WinFlag> Check(IEnumerator enumerator, int count)
            {
                IEnumerable<WinFlag> winFlags = __winFlags;
                if (winFlags == null)
                    winFlags = base.winFlags;

                if (winFlags == null)
                    return null;

                List<WinFlag> result = null;
                foreach(WinFlag winFlag in winFlags)
                {
                    if(((winFlag.instance >> 0) & 0x7) >= count)
                    {
                        if (result == null)
                            result = new List<WinFlag>();

                        result.Add(new WinFlag(winFlag.index, winFlag.instance - count));
                    }

                    if (((winFlag.instance >> 3) & 0x7) >= count)
                    {
                        if (result == null)
                            result = new List<WinFlag>();

                        result.Add(new WinFlag(winFlag.index, winFlag.instance - (count << 3)));
                    }

                    if (((winFlag.instance >> 6) & 0x7) >= count)
                    {
                        if (result == null)
                            result = new List<WinFlag>();

                        result.Add(new WinFlag(winFlag.index, winFlag.instance - (count << 6)));
                    }
                }

                __winFlags = result;

                IEnumerable<WinFlag> temp = Check(enumerator);

                __winFlags = winFlags;

                return temp;
            }
        }

        public class Rule258 : Rule
        {
            public override IEnumerable<WinFlag> Check(IEnumerator enumerator)
            {
                IEnumerator temp = enumerator.Clone();
                IEnumerable <WinFlag> winFlags = base.Check(enumerator);
                if (winFlags == null || temp == null || !temp.MoveNext())
                    return winFlags;

                Mahjong.Tile source = Mahjong.Tile.Get(temp.Current), destination;
                int count = 1;
                while(temp.MoveNext())
                {
                    destination = Mahjong.Tile.Get(temp.Current);
                    if (source.type == destination.type && source.number == destination.number)
                        ++count;
                    else
                    {
                        if(count == 2)
                            return source.number == 2 || source.number == 5 || source.number == 8 ? winFlags : null;

                        count = 1;
                    }
                }

                if (count == 2)
                    return source.number == 2 || source.number == 5 || source.number == 8 ? winFlags : null;

                return null;
            }
        }

        public class Player : Mahjong.Player
        {
            private static List<KeyValuePair<int, Mahjong.Tile>> __tiles;

            private byte[] __tileCodes;
            private MahjongServerPlayer __instance;
            private Room __room;

            public MahjongServerPlayer instance
            {
                get
                {
                    return __instance;
                }

                set
                {
                    if (__instance != null)
                        __instance.onInit -= __OnInit;

                    if (value != null)
                        value.onInit += __OnInit;

                    MahjongServer host = value == null ? null : value.host as MahjongServer;
                    if (host != null)
                    {
                        int index = value.node.index;
                        Node node;
                        if (host.GetNode(index, out node))
                        {
                            NetworkWriter writer = new NetworkWriter();
                            short handle = __OnInit(writer);
                            host.Send((short)index, handle, writer.AsArray(), writer.Position);
                        }
                    }

                    __instance = value;

                    __Reset();
                }
            }

            public Player(Room room) : base(room.__mahjong)
            {
                __room = room;
            }

            public override int Score(Mahjong.RuleType type, int playerIndex, IEnumerable<Mahjong.Rule.WinFlag> winFlags)
            {
                MahjongServer host = instance == null ? null : instance.host as MahjongServer;
                if (host == null)
                    return 0;
                
                NetworkWriter  writer = __instance.RpcStart();
                if (writer != null)
                {
                    writer.Write((byte)((int)type - (int)Mahjong.RuleType.Win));
                    writer.Write((byte)(((int)type + 4 - index) & 3));
                }

                int score = 0;
                switch(type)
                {
                    case Mahjong.RuleType.Win:
                    case Mahjong.RuleType.SelfDraw:
                    case Mahjong.RuleType.BreakKong:
                    case Mahjong.RuleType.OverKong:
                        if(type == Mahjong.RuleType.SelfDraw)
                        {
                            if (drawType == DrawType.Flower)
                                score += __Write(MahjongScoreType.FlowerDraw, writer, host.scores);
                        }

                        if(type == Mahjong.RuleType.OverKong)
                        {
                            score += __Write(MahjongScoreType.KongDraw, writer, host.scores);

                            if (drawType == DrawType.Flower)
                                score += __Write(MahjongScoreType.FlowerDraw, writer, host.scores);
                        }
                        break;
                    default:
                        if (writer != null)
                            writer.Write((byte)255);
                        return 0;
                }

                IEnumerable<int> handTileIndices = base.handTileIndices;
                if (winFlags != null)
                {
                    foreach (Mahjong.Rule.WinFlag winFlag in winFlags)
                    {
                        switch(winFlag.index)
                        {
                            case 0:
                                score += __Write(MahjongScoreType.AllPongHand, writer, host.scores);

                                if(Mahjong.Rule.IsGreatHand258(handTileIndices))
                                    score += __Write(MahjongScoreType.GreatHand258, writer, host.scores);

                                break;
                            case 1:
                            case 2:
                            case 3:
                                score += __Write(MahjongScoreType.Normal, writer, host.scores);
                                break;
                            case 4:
                                break;
                            case 5:
                                score += __Write(MahjongScoreType.SevenPairsHand, writer, host.scores);
                                break;
                        }
                    }
                }

                if (Mahjong.Rule.IsCleanHand(handTileIndices))
                    score += __Write(MahjongScoreType.CleanHand, writer, host.scores);

                if (Mahjong.Rule.IsPureHand(handTileIndices))
                    score += __Write(MahjongScoreType.PureHand, writer, host.scores);

                if (Mahjong.Rule.IsSimpleHand(handTileIndices))
                    score += __Write(MahjongScoreType.SimpleHand, writer, host.scores);

                if(count < 2)
                    score += __Write(MahjongScoreType.Single, writer, host.scores);

                Mahjong mahjong = base.mahjong;
                if(mahjong != null)
                {
                    if(poolTileIndexCount < 1)
                        score += __Write(index == mahjong.dealerIndex ? MahjongScoreType.HeavenlyHand : MahjongScoreType.EarthlyHand, writer, host.scores);
                }

                if (writer != null)
                    writer.Write((byte)255);

                instance.RpcEnd((short)MahjongNetworkRPCHandle.Score);

                return score;
            }

            public void SendRuleMessage(ReadOnlyCollection<Mahjong.RuleNode> ruleNodes)
            {
                MahjongServer host = __instance == null ? null : __instance.host as MahjongServer;
                if (host != null)
                {
                    Node node;
                    if (host.GetNode(__instance.node.index, out node))
                        host.SendRuleMessage(node.connectionId, ruleNodes);
                }
            }

            public IEnumerator WaitToThrow(float timeout)
            {
                if (__instance == null)
                    return null;

                return __instance.Wait((byte)handleTileIndex, (short)MahjongNetworkRPCHandle.Throw, timeout, __OnThrow);
            }

            public IEnumerator WaitToTry(float timeout)
            {
                if (__instance == null)
                    return null;

                return __instance.Wait((byte)handleTileIndex, (short)MahjongNetworkRPCHandle.Try, timeout, __OnTry);
            }

            public bool End(int index)
            {
                Action<int> handler = delegate (int temp)
                {
                    if (__tiles == null)
                        __tiles = new List<KeyValuePair<int, Mahjong.Tile>>();

                    __tiles.Add(new KeyValuePair<int, Mahjong.Tile>(temp, Mahjong.Tile.Get(GetHandTileIndex(temp))));
                };

                if (__tiles != null)
                    __tiles.Clear();
                
                int playerIndex;
                Mahjong.RuleNode ruleNode = Get(index);
                int groupIndex = End(index, handler, out playerIndex);
                byte group = groupIndex >= 0 && groupIndex < groupCount ? (byte)groupIndex : (byte)255;
                if (__instance != null)
                {
                    if (__tiles != null)
                    {
                        foreach (KeyValuePair<int, Mahjong.Tile> pair in __tiles)
                            __instance.Throw((byte)pair.Key, group, pair.Value);
                    }

                    __instance.Do((short)playerIndex, ruleNode.type, group);
                }

                return true;
            }

            public bool Draw()
            {
                return Draw(__Add, __Remove);
            }

            public new bool Discard(int index)
            {
                Mahjong.Tile tile = Mahjong.Tile.Get(GetHandTileIndex(index));
                if (base.Discard(index))
                {
                    if (instance != null)
                        instance.Throw((byte)index, 255, tile);

                    return true;
                }

                return false;
            }

            public new bool Try(int index)
            {
                Mahjong.RuleType type = Get(index).type;
                if (base.Try(index))
                {
                    if (__instance != null)
                        __instance.Try(type);

                    return true;
                }

                return false;
            }
            
            public new void Reset()
            {
                if (__room != null && __room.isRunning)
                {
                    MahjongServer host = __instance == null ? null : __instance.host as MahjongServer;
                    if (host != null)
                    {
                        Node node;
                        if (host.GetNode(__instance.node.index, out node))
                            host.SendShuffleMessage(node.connectionId, (short)__room.dealerIndex, (byte)__room.point0, (byte)__room.point1, (byte)__room.point2, (byte)__room.point3);
                    }
                }

                int count = (int)Mahjong.TileType.Unknown, step = (256 - count) / count;
                if (__tileCodes == null)
                    __tileCodes = new byte[count];

                int index = 0;
                for (int i = 0; i < count; ++i)
                    __tileCodes[i] = (byte)UnityEngine.Random.Range(index, index += step);

                __Reset();
            }
            
            private void __Reset()
            {
                if (__tileCodes != null)
                {
                    MahjongServer host = __instance == null ? null : __instance.host as MahjongServer;
                    if (host != null)
                    {
                        Node node;
                        if (host.GetNode(__instance.node.index, out node))
                            host.SendTileCodeMessage(node.connectionId, (byte)__tileCodes.Length, __tileCodes);
                    }
                }
            }

            private int __Write(MahjongScoreType type, NetworkWriter writer, int[] scores)
            {
                if ((int)(type) >= (scores == null ? 0 : scores.Length))
                    return 0;
                
                int result = scores[(int)type];
                if (writer != null)
                {
                    writer.Write((byte)type);

                    writer.Write((byte)result);
                }

                return result;
            }

            private void __Add(int index)
            {
                if (__tileCodes == null || instance == null)
                    return;

                byte code = Mahjong.Tile.Get(GetHandTileIndex(index));
                if (code >= __tileCodes.Length)
                    return;

                instance.Draw((byte)index, __tileCodes[code]);
            }

            private void __Remove(int index)
            {
                if (instance != null)
                    instance.Throw((byte)index, 255, Mahjong.Tile.Get(GetHandTileIndex(index)));
            }

            private void __OnThrow(byte index)
            {
                Discard(index);
            }

            private void __OnTry(byte index)
            {
                Try(index);
            }

            private short __OnInit(NetworkWriter writer)
            {
                if(writer != null)
                {
                    writer.Write((byte)groupCount);
                    IEnumerable<Group> groups = base.groups;
                    if (groups != null)
                    {
                        foreach (Group group in groups)
                        {
                            writer.Write((byte)group.type);
                            switch (group.type)
                            {
                                case Mahjong.RuleType.Chow:
                                case Mahjong.RuleType.Pong:
                                    writer.Write((byte)group.x);
                                    writer.Write((byte)group.y);
                                    writer.Write((byte)group.z);
                                    break;
                                case Mahjong.RuleType.Kong:
                                case Mahjong.RuleType.HiddenKong:
                                case Mahjong.RuleType.MeldedKong:
                                    writer.Write((byte)group.x);
                                    writer.Write((byte)group.y);
                                    writer.Write((byte)group.z);
                                    writer.Write((byte)group.w);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    writer.Write((byte)poolTileIndexCount);
                    IEnumerable<int> poolTileIndices = base.poolTileIndices;
                    if(poolTileIndices != null)
                    {
                        foreach(int poolTileIndex in poolTileIndices)
                            writer.Write((byte)poolTileIndex);
                    }

                    int count = __tileCodes == null ? 0 : base.count;
                    writer.Write((byte)count);
                    if (count > 0)
                    {
                        int length = __tileCodes == null ? 0 : __tileCodes.Length;
                        byte code;
                        foreach (KeyValuePair<int, int> pair in (IEnumerable<KeyValuePair<int, int>>)this)
                        {
                            writer.Write((byte)pair.Key);

                            code = Mahjong.Tile.Get(pair.Value);
                            code = code < length ? __tileCodes[code] : (byte)255;
                            writer.Write(code);
                        }
                    }
                }

                return (short)MahjongNetworkRPCHandle.Init;
            }
        }

        private bool __isRunning;
        private int __index;
        private int __point0;
        private int __point1;
        private int __point2;
        private int __point3;
        private Mahjong __mahjong;

        public bool isRunning
        {
            get
            {
                return __isRunning;
            }
        }

        public int index
        {
            get
            {
                return __index;
            }
        }

        public int dealerIndex
        {
            get
            {
                return __mahjong == null ? -1 : __mahjong.dealerIndex;
            }
        }

        public int tileCount
        {
            get
            {
                return __mahjong == null ? 0 : __mahjong.tileCount;
            }
        }

        public int point0
        {
            get
            {
                return __point0;
            }
        }

        public int point1
        {
            get
            {
                return __point1;
            }
        }

        public int point2
        {
            get
            {
                return __point2;
            }
        }

        public int point3
        {
            get
            {
                return __point3;
            }
        }

        public Room(int index)
        {
            __index = index;

            __mahjong = new Mahjong();
        }

        public Player Get(int index)
        {
            if (__mahjong == null)
                return null;

            return __mahjong.Get(index) as Player;
        }

        public IEnumerator Run()
        {
            __isRunning = true;

            if (__mahjong == null)
            {
                __mahjong = new Mahjong();

                __mahjong.rule = new Mahjong.Rule();
            }
            else if(__mahjong.rule == null)
                __mahjong.rule = new Mahjong.Rule();

            Player player;

            IEnumerable<Mahjong.Player> players = __mahjong.players;
            if(players != null)
            {
                foreach(Mahjong.Player instance in players)
                {
                    player = instance as Player;
                    if (player != null)
                        player.Reset();
                }
            }

            __mahjong.Shuffle(out __point0, out __point1, out __point2, out __point3);
            int i, index, ruleNodeIndex;
            Mahjong.RuleType ruleType;
            ReadOnlyCollection<Mahjong.RuleNode> ruleNodes;
            Player temp;
            while (__mahjong.tileCount < 144)
            {
                index = __mahjong.playerIndex;
                player = __mahjong.Get(index) as Player;
                if(player != null)
                {
                    while (true)
                    {
                        if (player.drawType == Mahjong.Player.DrawType.None)
                        {
                            if (!player.Draw())
                                break;
                        }
                        else
                        {
                            ruleNodes = player.Start();
                            if (ruleNodes != null && ruleNodes.Count > 0)
                            {
                                player.SendRuleMessage(ruleNodes);

                                yield return player.WaitToTry(5.0f);
                                
                                for (i = 1; i < 4; ++i)
                                {
                                    temp = __mahjong.Get((i + index) & 3) as Player;
                                    if (temp != null)
                                    {
                                        ruleNodes = temp.Start();

                                        if (ruleNodes != null && ruleNodes.Count > 0)
                                        {
                                            temp.SendRuleMessage(ruleNodes);

                                            yield return temp.WaitToTry(5.0f);

                                            ruleType = __mahjong.ruleType;
                                            if (ruleType == Mahjong.RuleType.BreakKong)
                                            {
                                                if (__mahjong.rulePlayerIndex == temp.index)
                                                {
                                                    if (!temp.End(__mahjong.ruleNodeIndex))
                                                        continue;
                                                }
                                                
                                                __isRunning = false;

                                                break;
                                            }
                                        }
                                    }
                                }
                                
                                if (!__isRunning)
                                    break;

                                ruleType = __mahjong.ruleType;
                                if (ruleType != Mahjong.RuleType.Unknown && index == __mahjong.rulePlayerIndex)
                                {
                                    ruleNodeIndex = __mahjong.ruleNodeIndex;
                                    if (!player.End(ruleNodeIndex))
                                        continue;

                                    if (ruleType == Mahjong.RuleType.SelfDraw || ruleType == Mahjong.RuleType.OverKong)
                                    {
                                        __isRunning = false;

                                        break;
                                    }
                                }
                            }

                            yield return player.WaitToThrow(6.0f);

                            for (i = 1; i < 4; ++i)
                            {
                                temp = __mahjong.Get((i + index) & 3) as Player;
                                if (temp != null)
                                {
                                    ruleNodes = temp.Start();

                                    if (ruleNodes != null && ruleNodes.Count > 0)
                                    {
                                        temp.SendRuleMessage(ruleNodes);

                                        yield return temp.WaitToTry(5.0f);

                                        ruleType = __mahjong.ruleType;
                                        if (ruleType == Mahjong.RuleType.Win)
                                        {
                                            __isRunning = false;

                                            break;
                                        }
                                    }
                                }
                            }

                            ruleType = __mahjong.ruleType;
                            if (ruleType != Mahjong.RuleType.Unknown)
                            {
                                player = __mahjong.Get(__mahjong.rulePlayerIndex) as Player;
                                if (player != null)
                                {
                                    ruleNodeIndex = __mahjong.ruleNodeIndex;
                                    player.End(ruleNodeIndex);
                                }
                            }
                            
                            break;
                        }
                    }
                }

                if(!__isRunning)
                    break;
                
                yield return new WaitForSeconds(2.0f);
            }
            
            __Break();
        }

        public void Break()
        {
            __isRunning = false;
        }

        private void __Break()
        {
            int i, j, count;
            Node node;
            Player player;
            MahjongServerPlayer instance;
            ZG.Network.Server host;
            ZG.Network.Lobby.Node temp;
            for(i = 0; i < 4; ++i)
            {
                player = __mahjong.Get(i) as Player;
                instance = player == null ? null : player.instance;
                temp = instance == null ? null : instance.node as ZG.Network.Lobby.Node;
                if (temp != null)
                {
                    host = instance.host;
                    if (host != null && host.GetNode(temp.index, out node) && node.connectionId < 0)
                        host.Unregister(temp.index, null);
                    else
                    {
                        count = temp.count;
                        for (j = 0; j < count; ++j)
                            temp.NotReady();
                    }
                }
            }
        }
    }

    public int[] scores;
    private Pool<string> __playerNames;
    private Pool<string> __roomNames;
    private Dictionary<string, Player> __playerMap;
    private Dictionary<string, Room> __roomMap;
    
    public int CreateRoom(string name)
    {
        if (__roomMap == null)
            __roomMap = new Dictionary<string, Room>();
        else if (__roomMap.ContainsKey(name))
            return -1;

        int roomIndex = nextRoomIndex;
        if (__roomNames == null)
            __roomNames = new Pool<string>();

        __roomNames.Insert(roomIndex, name);

        __roomMap[name] = new Room(roomIndex);

        return roomIndex;
    }

    public void SendShuffleMessage(int connectionId, short dealerIndex, byte point0, byte point1, byte point2, byte point3)
    {
        Send(connectionId, (short)MahjongNetworkMessageType.Shuffle, new MahjongShuffleMessage(point0, point1, point2, point2, dealerIndex));
    }

    public void SendTileCodeMessage(int connectionId, byte count, IEnumerable<byte> codes)
    {
        Send(connectionId, (short)MahjongNetworkMessageType.TileCodes, new MahjongTileCodeMessage(count, codes));
    }

    public void SendRuleMessage(int connectionId, ReadOnlyCollection<Mahjong.RuleNode> ruleNodes)
    {
        Send(connectionId, (short)MahjongNetworkMessageType.RuleNodes, new MahjongRuleMessage(ruleNodes));
    }
    
    public new void Create()
    {
        onRegistered += __OnRegistered;
        onReady += __OnReady;

        base.Create();

        RegisterHandler((short)MahjongNetworkMessageType.Player, __OnPlayer);
        RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
    }
    
    protected override bool _GetRoomInfo(NetworkReader reader, int connectionId, int roomIndex, out int length)
    {
        length = 4;

        if (__roomNames == null || __roomMap == null)
            return false;

        string roomName;
        if (!__roomNames.TryGetValue(roomIndex, out roomName))
            return false;
        
        return __roomMap.ContainsKey(roomName);
    }

    protected override bool _GetPlayerInfo(NetworkReader reader, int connectionId, out short type, out int roomIndex, out int playerIndex)
    {
        type = 0;
        roomIndex = -1;
        playerIndex = -1;
        if (__roomMap == null)
            return false;

        RegisterMessage message = reader == null ? null : reader.ReadMessage<RegisterMessage>();
        if (message == null)
            return false;

        Room room;
        if (!__roomMap.TryGetValue(message.roomName, out room) || room == null)
            return false;

        if (__playerMap == null)
            __playerMap = new Dictionary<string, Player>();

        Player player;
        if (!__playerMap.TryGetValue(message.name, out player))
        {
            if (!room.isRunning && GetCount(room.index) > 3)
                return false;

            player.index = GetRoomNextIndex(room.index);
            player.roomName = message.roomName;
            __playerMap.Add(message.name, player);
            
            if (__playerNames == null)
                __playerNames = new Pool<string>();

            __playerNames.Insert(nextNodeIndex, message.name);
        }

        type = (short)(player.index >= 0 && player.index < 4 ? player.index : 4);

        roomIndex = room.index;

        playerIndex = player.index;

        return true;
    }

    protected override bool _Unregister(NetworkReader reader, int connectionId, short index)
    {
        if (!base._Unregister(reader, connectionId, index))
            return false;

        if (__playerNames != null)
        {
            string name;
            if (__playerNames.TryGetValue(index, out name))
            {
                if (__playerMap != null)
                {
                    Player player;
                    if (__playerMap.TryGetValue(name, out player))
                    {
                        if (__roomMap != null)
                        {
                            Room room;
                            if (__roomMap.TryGetValue(player.roomName, out room) && room != null)
                            {
                                if (room.isRunning)
                                    return false;

                                Room.Player temp = room.Get(player.index);
                                if (temp != null)
                                    temp.index = -1;

                                if (GetCount(room.index) < 1)
                                {
                                    room.Break();

                                    if (__roomNames != null)
                                        __roomNames.RemoveAt(room.index);

                                    __roomMap.Remove(player.roomName);
                                }
                            }
                        }

                        __playerMap.Remove(name);
                    }
                }

                __playerNames.RemoveAt(index);
            }
        }

        return true;
    }
    
    private void __OnRegistered(ZG.Network.Node node)
    {
        if (node == null || __playerNames == null || __playerMap == null)
            return;

        string playerName;
        if (!__playerNames.TryGetValue(node.index, out playerName))
            return;

        Player player;
        if (!__playerMap.TryGetValue(playerName, out player))
            return;

        if (player.index >= 0 && player.index < 4)
        {
            if (__roomMap == null)
                return;

            Room room;
            if (!__roomMap.TryGetValue(player.roomName, out room) || room == null)
                return;

            Room.Player temp = room.Get(player.index);
            if (temp == null)
            {
                temp = new Room.Player(room);
                temp.index = player.index;
            }

            temp.instance = node == null ? null : node.GetComponent<MahjongServerPlayer>();
        }
    }

    private void __OnReady(int roomIndex, int count)
    {
        if (__roomNames == null || __roomMap == null)
            return;

        string roomName;
        if (!__roomNames.TryGetValue(roomIndex, out roomName))
            return;

        Room room;
        if (!__roomMap.TryGetValue(roomName, out room) || room == null)
            return;

        if(count == 1)
            StartCoroutine(room.Run());
    }

    private void __OnPlayer(NetworkMessage message)
    {
        NetworkConnection connection = message == null ? null : message.conn;
        if (connection == null)
            return;

        NameMessage nameMessage = message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        int connectionId = connection.connectionId;
        string roomName;
        Player player;
        if (__playerMap != null && __playerMap.TryGetValue(nameMessage.name, out player))
        {
            roomName = player.roomName;
            
            if (__roomMap != null)
            {
                Room room;
                if (__roomMap.TryGetValue(roomName, out room) && room != null && room.isRunning)
                    SendShuffleMessage(connectionId, (short)room.dealerIndex, (byte)room.point0, (byte)room.point1, (byte)room.point2, (byte)room.point3);
            }
        }
        else
            roomName = string.Empty;

        Send(connectionId, (short)MahjongNetworkMessageType.Player, new NameMessage(roomName));
    }

    private void __OnRoom(NetworkMessage message)
    {
        NetworkConnection connection = message == null ? null : message.conn;
        if (connection == null)
            return;

        NameMessage nameMessage = message.ReadMessage<NameMessage>();
        string name;
        if (nameMessage == null || string.IsNullOrEmpty(nameMessage.name))
        {
            name = nextRoomIndex.ToString("D6");
            if (CreateRoom(name) == -1)
                name = string.Empty;
        }
        else
        {
            if (__roomMap == null || !__roomMap.ContainsKey(nameMessage.name))
                name = string.Empty;
            else
                name = nameMessage.name;
        }

        Send(connection.connectionId, (short)MahjongNetworkMessageType.Room, new NameMessage(name));
    }

}
