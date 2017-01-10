﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using ZG.Network.Lobby;

public class MahjongClientPlayer : Node
{
    private struct Tile
    {
        public byte index;
        public byte code;
        public MahjongAsset asset;

        public Tile(byte index, byte code, MahjongAsset asset)
        {
            this.index = index;
            this.code = code;
            this.asset = asset;
        }
    }

    private struct Handle
    {
        public Tile tile;
        public float velocity;

        public Handle(Tile tile, float velocity)
        {
            this.tile = tile;
            this.velocity = velocity;
        }
    }
    
    private struct Cache
    {
        public Tile tile;
        public byte group;
        public Mahjong.Tile? instance;

        public Cache(Tile tile, byte group, Mahjong.Tile? instance)
        {
            this.tile = tile;
            this.group = group;
            this.instance = instance;
        }
    }

    private struct Selector
    {
        public Mahjong.RuleNode ruleNode;
        public Action handler;

        public Selector(Mahjong.RuleNode ruleNode, Action handler)
        {
            this.ruleNode = ruleNode;
            this.handler = handler;
        }
    }

    private struct Group
    {
        public int index;
        public int count;

        public Group(int index, int count)
        {
            this.index = index;
            this.count = count;
        }
    }
    
    private int __drawCount;
    private int __discardCount;
    private int __scoreCount;
    private int __groupCount;
    private float __coolDown;
    private float __holdTime;
    private MahjongAsset __asset;
    private LinkedListNode<Cache> __handle;
    private LinkedList<Cache> __caches;
    private LinkedList<Handle> __handles;
    private LinkedList<Selector> __selectors;
    private List<MahjongAsset> __selectedAssets;
    private Dictionary<byte, Group> __groups;

    public void Clear()
    {
        __drawCount = 0;
        __discardCount = 0;
        __scoreCount = 0;
        __groupCount = 0;
        __coolDown = 0;
        __holdTime = 0;
        __asset = null;
        __handle = null;

        if (__caches != null)
            __caches.Clear();

        if (__handles != null)
            __handles.Clear();

        if (__selectors != null)
            __selectors.Clear();

        if (__selectedAssets != null)
            __selectedAssets.Clear();

        if (__groups != null)
            __groups.Clear();
    }
    
    public void Unselect()
    {
        if (__selectors != null)
            __selectors.Clear();

        if(__selectedAssets != null)
        {
            foreach(MahjongAsset selectedAsset in __selectedAssets)
            {
                if (selectedAsset != null)
                    selectedAsset.onSelected = null;
            }

            __selectedAssets.Clear();
        }
    }

    public void Select(Mahjong.RuleNode node, Action handler)
    {
        if (__selectors == null)
            __selectors = new LinkedList<Selector>();

        __selectors.AddLast(new Selector(node, handler));
    }

    public void Try(byte index)
    {
        CmdTry(index);
    }

    private LinkedListNode<Handle> __Add(Tile tile)
    {
        if (__handles == null)
            __handles = new LinkedList<Handle>();

        Handle result = new Handle(tile, 0.0f), temp;
        for (LinkedListNode<Handle> node = __handles.First; node != null; node = node.Next)
        {
            temp = node.Value;
            if (temp.tile.code > tile.code)
                return __handles.AddBefore(node, result);
        }
        
        return __handles.AddLast(result);
    }

    private void __Throw(Tile tile, byte group, Mahjong.Tile instance)
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        if (tile.asset != null)
        {
            tile.asset.onSelected = null;
            tile.asset.onDiscard = null;

            if (room != null)
                room.As(tile.asset, instance);
        }

        if (group == 255)
        {
            switch (instance.type)
            {
                case Mahjong.TileType.Spring:
                case Mahjong.TileType.Summer:
                case Mahjong.TileType.Autumn:
                case Mahjong.TileType.Winter:

                case Mahjong.TileType.Plum:
                case Mahjong.TileType.Orchid:
                case Mahjong.TileType.Chrysanthemum:
                case Mahjong.TileType.Bamboo:
                    room.Score(tile.asset, __scoreCount++);
                    break;
                default:
                    room.Discard(tile.asset, __discardCount++);

                    __asset = tile.asset;
                    break;
            }
        }
        else
        {
            if (__groups == null)
                __groups = new Dictionary<byte, Group>();

            Group temp;
            if (!__groups.TryGetValue(group, out temp))
                temp = new Group(__groups.Count, 0);

            room.Group(tile.asset, temp.index, temp.count);

            ++temp.count;
            __groups[group] = temp;
        }
    }

    private void __Discard(Tile tile)
    {
        if (tile.asset == null)
            return;

        tile.asset.onDiscard = delegate ()
        {
            if (__coolDown > 0.0f)
                return;
            
            CmdDiscard(tile.index);
        };
    }

    private void __Reset()
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room != null)
        {
            if (room.animator != null)
                room.animator.SetTrigger("Reset");

            if (room.time != null)
                room.time.text = string.Empty;
        }
    }
    
    private void __OnCreate()
    {
        if(isLocalPlayer)
        {
            Camera camera = Camera.main;
            if (camera != null)
                camera.transform.SetParent(transform, false);

            MahjongClientRoom room = MahjongClientRoom.instance;
            if(room != null)
            {
                Transform transform = room.time == null ? null : room.time.transform;
                if (transform != null)
                {
                    Vector3 eulerAngles = transform.eulerAngles;
                    transform.eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y - (type + 1) * 90.0f, eulerAngles.z);
                }
            }
        }
    }
    
    private void __OnHold(NetworkReader reader)
    {
        if (reader == null)
            return;

        RpcHold(reader.ReadSingle());
    }

    private void __OnDraw(NetworkReader reader)
    {
        if (reader == null)
            return;

        RpcDraw(reader.ReadByte(), reader.ReadByte());
    }

    private void __OnThrow(NetworkReader reader)
    {
        if (reader == null)
            return;
        
        RpcThrow(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
    }

    private void __OnTry(NetworkReader reader)
    {
        if (reader == null)
            return;

        RpcTry((Mahjong.RuleType)reader.ReadByte());
    }

    private void __OnDo(NetworkReader reader)
    {
        if (reader == null)
            return;

        RpcDo(reader.ReadInt16(), (Mahjong.RuleType)reader.ReadByte(), reader.ReadByte());
    }

    private void __OnScore(NetworkReader reader)
    {
        if (reader == null)
            return;

        Client host = base.host as Client;
        ZG.Network.Node node = host == null ? null : host.localPlayer;
        short type = node == null ? (short)0 : node.type;
        int index = (base.type - type + 4) & 3, ruleType = reader.ReadByte(), playerIndex = (reader.ReadByte() + index) & 3;
        MahjongClientRoom room = MahjongClientRoom.instance;
        MahjongClientRoom.Player normalPlayer = new MahjongClientRoom.Player(), winPlayer = new MahjongClientRoom.Player();
        GameObject gameObject;
        if (room != null)
        {
            if (isLocalPlayer)
            {
                if (room.finish.win.root != null)
                    room.finish.win.root.SetActive(true);

                if (room.finish.normal.root != null)
                    room.finish.normal.root.SetActive(false);
            }
            else
            {
                if (room.finish.win.root == null || !room.finish.win.root.activeSelf)
                {
                    if (room.finish.normal.root != null)
                        room.finish.normal.root.SetActive(true);
                }
            }

            if (index < (room.finish.normal.players == null ? 0 : room.finish.normal.players.Length))
            {
                normalPlayer = room.finish.normal.players[index];
                if(ruleType >= 0 && ruleType < (normalPlayer.winners == null ? 0 : normalPlayer.winners.Length))
                {
                    gameObject = normalPlayer.winners[ruleType];
                    if (gameObject != null)
                        gameObject.SetActive(true);
                }
            }

            if (index < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
            {
                winPlayer = room.finish.win.players[index];
                if (ruleType >= 0 && ruleType < (winPlayer.winners == null ? 0 : winPlayer.winners.Length))
                {
                    gameObject = winPlayer.winners[ruleType];
                    if (gameObject != null)
                        gameObject.SetActive(true);
                }
            }


            if (playerIndex > 0)
            {
                if (playerIndex < (room.finish.normal.players == null ? 0 : room.finish.normal.players.Length))
                {
                    GameObject[] losers = room.finish.normal.players[index].losers;
                    if (ruleType >= 0 && ruleType < (losers == null ? 0 : losers.Length))
                    {
                        gameObject = losers[ruleType];
                        if (gameObject != null)
                            gameObject.SetActive(true);
                    }
                }

                if (playerIndex < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
                {
                    GameObject[] losers = room.finish.win.players[index].losers;
                    if (ruleType >= 0 && ruleType < (losers == null ? 0 : losers.Length))
                    {
                        gameObject = losers[ruleType];
                        if (gameObject != null)
                            gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                if (room.finish.normal.players != null)
                {
                    foreach(MahjongClientRoom.Player player in room.finish.normal.players)
                    {
                        if (ruleType >= 0 && ruleType < (player.losers == null ? 0 : player.losers.Length))
                        {
                            gameObject = player.losers[ruleType];
                            if (gameObject != null)
                                gameObject.SetActive(true);
                        }
                    }
                }

                if (room.finish.win.players != null)
                {
                    foreach (MahjongClientRoom.Player player in room.finish.win.players)
                    {
                        if (ruleType >= 0 && ruleType < (player.losers == null ? 0 : player.losers.Length))
                        {
                            gameObject = player.losers[ruleType];
                            if (gameObject != null)
                                gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
        
        int score = 0, normalScores = normalPlayer.scores == null ? 0 : normalPlayer.scores.Length, winScores = winPlayer.scores == null ? 0 : winPlayer.scores.Length;
        byte scoreType;
        while(true)
        {
            scoreType = reader.ReadByte();
            if (scoreType == 255)
                break;
            
            if(scoreType < normalScores)
            {
                gameObject = normalPlayer.scores[scoreType];
                if (gameObject != null)
                    gameObject.SetActive(true);
            }

            if (scoreType < winScores)
            {
                gameObject = winPlayer.scores[scoreType];
                if (gameObject != null)
                    gameObject.SetActive(true);
            }

            score += reader.ReadByte();
        }
        
        if (normalPlayer.score != null)
            normalPlayer.score.text = score.ToString();

        if (winPlayer.score != null)
            winPlayer.score.text = score.ToString();
        
        if (playerIndex > 0)
        {
            if (playerIndex < (room.finish.normal.players == null ? 0 : room.finish.normal.players.Length))
            {
                Text text = room.finish.normal.players[index].score;
                if (text != null)
                {
                    if (int.TryParse(text.text, out index))
                        text.text = (-score - index).ToString();
                    else
                        text.text = (-score).ToString();
                }
            }

            if (playerIndex < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
            {
                Text text = room.finish.win.players[index].score;
                if (text != null)
                {
                    if (int.TryParse(text.text, out index))
                        text.text = (-score - index).ToString();
                    else
                        text.text = (-score).ToString();
                }
            }
        }
        else
        {
            if (room.finish.normal.players != null)
            {
                foreach (MahjongClientRoom.Player player in room.finish.normal.players)
                {
                    if(player.score != null)
                    {
                        if (int.TryParse(player.score.text, out index))
                            player.score.text = (-score - index).ToString();
                        else
                            player.score.text = (-score).ToString();
                    }
                }
            }

            if (room.finish.win.players != null)
            {
                foreach (MahjongClientRoom.Player player in room.finish.win.players)
                {
                    if (player.score != null)
                    {
                        if (int.TryParse(player.score.text, out index))
                            player.score.text = (-score - index).ToString();
                        else
                            player.score.text = (-score).ToString();
                    }
                }
            }
        }
    }

    private void __OnInit(NetworkReader reader)
    {
        if (reader == null)
            return;

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        byte length = reader.ReadByte(), count, i, j;
        Mahjong.RuleType type;
        MahjongAsset asset;
        Transform transform;
        for (i = 0; i < length; ++i)
        {
            type = (Mahjong.RuleType)reader.ReadByte();

            count = 0;
            switch (type)
            {
                case Mahjong.RuleType.Chow:
                case Mahjong.RuleType.Kong:
                    count = 3;
                    break;
                case Mahjong.RuleType.Pong:
                case Mahjong.RuleType.HiddenKong:
                case Mahjong.RuleType.MeldedKong:
                    count = 4;
                    break;
            }

            for (j = 0; j < count; ++j)
            {
                asset = room.next;
                room.As(asset, Mahjong.Tile.Get(reader.ReadByte()));
                transform = asset == null ? null : asset.transform;
                if (transform != null)
                {
                    transform.SetParent(this.transform, false);
                    room.Group(asset, i, j);
                }
            }

            __groupCount += count;

            if (__groups == null)
                __groups = new Dictionary<byte, Group>();

            __groups.Add(i, new Group(i, count));
        }

        length = reader.ReadByte();
        Mahjong.Tile tile;
        for (i = 0; i < length; ++i)
        {
            tile = Mahjong.Tile.Get(reader.ReadByte());
            asset = room.next;

            transform = asset == null ? null : asset.transform;
            if (transform == null)
                continue;

            transform.SetParent(this.transform, false);

            room.As(asset, tile);
            switch (tile.type)
            {
                case Mahjong.TileType.Spring:
                case Mahjong.TileType.Summer:
                case Mahjong.TileType.Autumn:
                case Mahjong.TileType.Winter:

                case Mahjong.TileType.Plum:
                case Mahjong.TileType.Orchid:
                case Mahjong.TileType.Chrysanthemum:
                case Mahjong.TileType.Bamboo:
                    room.Score(asset, __scoreCount++);
                    break;
                default:
                    room.Discard(asset, __discardCount++);

                    __asset = asset;
                    break;
            }
        }
        
        length = reader.ReadByte();
        for (i = 0; i < length; ++i)
            RpcDraw(reader.ReadByte(), reader.ReadByte());
    }

    private void RpcHold(float time)
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room != null && room.animator != null)
            room.animator.SetTrigger(type.ToString());

        __holdTime = time + Time.time;

        Invoke("__Reset", time);
    }

    private void RpcDraw(byte index, byte code)
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        MahjongAsset asset = room == null ? null : room.next;
        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        byte count = (byte)((__handles == null ? 0 : __handles.Count) + (__caches == null ? 0 : __caches.Count));
        transform.SetParent(this.transform, false);
        transform.localEulerAngles = Vector3.zero;
        transform.localPosition = room.handPosition + new Vector3(count * room.width + room.offset, 0.0f, 0.0f);

        if (isLocalPlayer)
        {
            MahjongClient host = base.host as MahjongClient;
            if (host != null)
                room.As(asset, host.GetTile(code));
        }
        
        Cache result = new Cache(new Tile(index, code, asset), 255, null);
        if (__caches == null)
            __caches = new LinkedList<Cache>();

        if (count < (13 - __groupCount))
            __caches.AddLast(result);
        else
        {
            __handle = new LinkedListNode<Cache>(result);

            if(isLocalPlayer)
                __Discard(result.tile);
        }
        
        ++__drawCount;
    }

    private void RpcThrow(byte index, byte group, Mahjong.Tile instance)
    {
        __holdTime = 0.0f;

        if (__handle != null && __handle.Value.tile.index == index)
        {
            if (__caches == null)
                __caches = new LinkedList<Cache>();

            __handle.Value = new Cache(__handle.Value.tile, group, instance);
            __caches.AddLast(__handle);

            __handle = null;
        }
        else
        {
            LinkedListNode<Cache> cacheNode = null;
            for (cacheNode = __caches == null ? null : __caches.First; cacheNode != null; cacheNode = cacheNode.Next)
            {
                if (cacheNode.Value.tile.index == index)
                {
                    cacheNode.Value = new Cache(cacheNode.Value.tile, group, instance);

                    break;
                }
            }

            if (cacheNode == null)
            {
                LinkedListNode<Handle> node = null;
                for (node = __handles == null ? null : __handles.First; node != null; node = node.Next)
                {
                    if (node.Value.tile.index == index)
                    {
                        __handles.Remove(node);

                        break;
                    }
                }
                
                if (node == null)
                    return;

                Handle handle = node.Value;
                if (handle.tile.asset != null)
                {
                    handle.tile.asset.onSelected = null;
                    handle.tile.asset.onDiscard = null;
                }

                __Throw(node.Value.tile, group, instance);

                MahjongClientRoom room = MahjongClientRoom.instance;
                if(room != null)
                    __coolDown = Mathf.Max(__coolDown, room.throwTime);
            }
            
            if (__handle != null)
            {
                MahjongAsset asset = __handle.Value.tile.asset;
                if (asset != null)
                    asset.onDiscard = null;

                if (__caches == null)
                    __caches = new LinkedList<Cache>();

                __caches.AddLast(__handle);

                __handle = null;
            }
        }
    }

    private void RpcTry(Mahjong.RuleType type)
    {
        __holdTime = 0.0f;
    }

    private void RpcDo(short playerIndex, Mahjong.RuleType type, byte group)
    {
        if (__groups == null)
            return;

        Group temp;
        if (!__groups.TryGetValue(group, out temp))
            return;

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        if (playerIndex != index)
        {
            Client host = base.host as Client;
            MahjongClientPlayer player = host == null ? null : host.Get(playerIndex) as MahjongClientPlayer;
            MahjongAsset asset = player == null ? null : player.__asset;
            Transform transform = asset == null ? null : asset.transform;
            if (transform == null)
                return;

            transform.SetParent(this.transform, false);
            room.Group(asset, temp.index, temp.count);
        }

        ++temp.count;

        __groups[group] = temp;

        switch(type)
        {
            case Mahjong.RuleType.Chow:
            case Mahjong.RuleType.Kong:
                __groupCount += 3;
                break;
            case Mahjong.RuleType.Pong:
            case Mahjong.RuleType.HiddenKong:
            case Mahjong.RuleType.MeldedKong:
                __groupCount += 4;
                break;
        }
    }
    
    private void CmdDiscard(byte index)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.Write(index);

        Rpc((short)MahjongNetworkRPCHandle.Throw, writer.AsArray(), writer.Position);
    }

    private void CmdTry(byte index)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.Write(index);

        Rpc((short)MahjongNetworkRPCHandle.Try, writer.AsArray(), writer.Position);
    }

    void Awake()
    {
        RegisterHandler((short)MahjongNetworkRPCHandle.Init, __OnInit);
        RegisterHandler((short)MahjongNetworkRPCHandle.Hold, __OnHold);
        RegisterHandler((short)MahjongNetworkRPCHandle.Draw, __OnDraw);
        RegisterHandler((short)MahjongNetworkRPCHandle.Throw, __OnThrow);
        RegisterHandler((short)MahjongNetworkRPCHandle.Try, __OnTry);
        RegisterHandler((short)MahjongNetworkRPCHandle.Do, __OnDo);

        onCreate += __OnCreate;
    }

    void Update()
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;
        
        float time = Time.time;
        if (time < __holdTime)
        {
            if (room.time != null)
                room.time.text = Mathf.RoundToInt(__holdTime - time).ToString();
        }

        __coolDown -= Time.deltaTime;
        if (__coolDown > 0.0f)
        {
            Handle handle;
            Transform transform;
            Vector3 position;
            int index = 0;
            for (LinkedListNode<Handle> node = __handles == null ? null : __handles.First; node != null; node = node.Next)
            {
                handle = node.Value;
                transform = (handle.tile.asset == null || handle.tile.asset.isDraging) ? null : handle.tile.asset.transform;
                if (transform != null)
                {
                    position = transform.localPosition;
                    position.x = Mathf.SmoothDamp(position.x, room.handPosition.x + room.width * index, ref handle.velocity, room.smoothTime, room.maxSpeed);
                    transform.localPosition = position;


                    node.Value = handle;
                }

                ++index;
            }
        }
        else if (__caches != null)
        {

            LinkedListNode<Cache> cacheNode;
            Cache cache;
            while (__caches.Count > 0)
            {
                cacheNode = __caches.First;
                __caches.RemoveFirst();
                if (cacheNode != null)
                {
                    cache = cacheNode.Value;
                    if (cache.instance != null)
                    {
                        __Throw(cache.tile, cache.group, (Mahjong.Tile)cache.instance);

                        __coolDown = room.throwTime;
                    }
                    else if (cache.tile.asset != null)
                    {
                        if (cache.tile.asset != null && isLocalPlayer)
                            __Discard(cache.tile);

                        __Add(cache.tile);

                        if (cache.tile.asset != null)
                            cache.tile.asset.Move();

                        __coolDown = room.moveTime;
                    }

                    break;
                }
            }

            if (__caches.Count < 1 && __selectors != null && __selectors.Count > 0)
            {
                LinkedListNode<Selector> selectorNode;
                Selector selector;
                Handle handle;
                int index = 0;
                for (LinkedListNode<Handle> hanldeNode = __handles == null ? null : __handles.First; hanldeNode != null; hanldeNode = hanldeNode.Next)
                {
                    handle = hanldeNode.Value;
                    if (handle.tile.asset != null)
                    {
                        for (selectorNode = __selectors.First; selectorNode != null; selectorNode = selectorNode.Next)
                        {
                            selector = selectorNode.Value;
                            if (selector.ruleNode.index == index)
                            {
                                handle.tile.asset.onSelected = selector.handler;

                                if (__selectedAssets == null)
                                    __selectedAssets = new List<MahjongAsset>();

                                __selectedAssets.Add(handle.tile.asset);

                                __selectors.Remove(selectorNode);

                                break;
                            }
                        }
                    }

                    ++index;
                }
            }
        }
    }
}
