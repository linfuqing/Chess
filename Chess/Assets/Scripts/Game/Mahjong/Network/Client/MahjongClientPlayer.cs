using System;
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
        public int index;
        public Action handler;

        public Selector(int index, Action handler)
        {
            this.index = index;
            this.handler = handler;
        }
    }

    private struct Group
    {
        public int index;
        public int count;
        public Mahjong.Tile[] tiles;
        public Mahjong.Tile? tile;

        public Group(int index, int count)
        {
            this.index = index;
            this.count = count;
            tiles = new Mahjong.Tile[3];
            for (int i = 0; i < 3; ++i)
                tiles[i] = 255;

            tile = null;
        }
    }

    private bool __isShow;
    private int __drawCount;
    private int __discardCount;
    private int __scoreCount;
    private int __groupCount;
    private float __coolDown;
    private float __holdTime;
    private Mahjong.Tile __tile;
    private MahjongAsset __asset;
    private LinkedListNode<Cache> __handle;
    private LinkedList<Cache> __caches;
    private LinkedList<Handle> __handles;
    private LinkedList<Selector> __selectors;
    private List<MahjongAsset> __selectedAssets;
    private Dictionary<byte, Group> __groups;

    public void Clear()
    {
        __isShow = false;
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

    public void Select(int index, Action handler)
    {
        if (__handle != null)
        {
            Tile tile = __handle.Value.tile;
            if (tile.index == index)
            {
                tile.asset.onSelected = handler;

                if (__selectedAssets == null)
                    __selectedAssets = new List<MahjongAsset>();

                __selectedAssets.Add(tile.asset);

                return;
            }
        }
        
        if (__selectors == null)
            __selectors = new LinkedList<Selector>();

        __selectors.AddLast(new Selector(index, handler));
    }

    public void Ready(byte code)
    {
        CmdReady(code);
    }

    public void Try(byte index)
    {
        CmdTry(index);
    }

    private int __Add(Tile tile)
    {
        if (__handles == null)
            __handles = new LinkedList<Handle>();

        int index = 0;
        Handle result = new Handle(tile, 0.0f), temp;
        for (LinkedListNode<Handle> node = __handles.First; node != null; node = node.Next)
        {
            temp = node.Value;
            if (temp.tile.code > tile.code)
            {
                __handles.AddBefore(node, result);

                return index;
            }

            ++index;
        }
        
        __handles.AddLast(result);

        return index;
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
                    room.Throw(tile.asset, __scoreCount++);
                    break;
                default:
                    room.Discard(tile.asset, __discardCount++);
                    if (room.arrow != null)
                    {
                        Transform transform = tile.asset == null ? null : tile.asset.transform;
                        if (transform != null)
                        {
                            GameObject gameObject = room.arrow.gameObject;
                            if (gameObject != null)
                                gameObject.SetActive(true);

                            room.arrow.localPosition = transform.localPosition;

                            room.arrow.SetParent(this.transform, false);
                        }
                    }

                    __tile = instance;
                    __asset = tile.asset;
                    break;
            }
        }
        else
        {
            if (__groups == null)
                __groups = new Dictionary<byte, Group>();

            Group temp;
            if (__groups.TryGetValue(group, out temp))
            {
                if (temp.count < (temp.tiles == null ? 0 : temp.tiles.Length))
                    temp.tiles[temp.count] = instance;
                else
                    temp.tile = instance;
            }
            else
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
            if (room.wind != null)
                room.wind.SetTrigger("Reset");

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

    private void __OnReady(NetworkReader reader)
    {
        if (reader == null)
            return;
        
        byte index;
        int count = 0;
        Cache cache;
        Handle handle;
        LinkedListNode<Handle> handleNode;
        LinkedListNode<Cache> cacheNode;
        while(true)
        {
            index = reader.ReadByte();
            if (index == 255)
                break;

            ++count;

            if (__handle != null)
            {
                cache = __handle.Value;
                if (cache.tile.index == index)
                {
                    if (cache.tile.asset != null)
                        cache.tile.asset.Visible();

                    __handle.Value = new Cache(new Tile(index, reader.ReadByte(), cache.tile.asset), cache.group, cache.instance);

                    continue;
                }
            }

            for(handleNode = __handles == null ? null : __handles.First; handleNode != null; handleNode = handleNode.Next)
            {
                handle = handleNode.Value;
                if (handle.tile.index == index)
                {
                    if (handle.tile.asset != null)
                        handle.tile.asset.Visible();

                    handleNode.Value = new Handle(new Tile(index, reader.ReadByte(), handle.tile.asset), handle.velocity);

                    break;
                }
            }

            for (cacheNode = __caches == null ? null : __caches.First; cacheNode != null; cacheNode = cacheNode.Next)
            {
                cache = cacheNode.Value;
                if (cache.tile.index == index)
                {
                    if (cache.tile.asset != null)
                        cache.tile.asset.Visible();

                    cacheNode.Value = new Cache(new Tile(index, reader.ReadByte(), cache.tile.asset), cache.group, cache.instance);

                    continue;
                }
            }

            if (handleNode != null)
                continue;
        }

        __isShow = count > 0;
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
        if (node == null)
            return;

        byte ruleType = reader.ReadByte(), offset = reader.ReadByte(), code = reader.ReadByte();
        short index = (byte)((type + 4 - node.type) & 3), playerIndex = (short)((index + offset) & 3);
        int temp;
        MahjongClientRoom room = MahjongClientRoom.instance;
        MahjongFinishPlayerStyle normalPlayer = null, winPlayer = null, player;
        GameObject gameObject;
        if (room != null)
        {
            if (isLocalPlayer)
            {
                if (room.finish.win.root == null)
                {
                    if (room.finish.normal.root != null)
                        room.finish.normal.root.SetActive(true);
                }
                else
                {
                    room.finish.win.root.SetActive(true);

                    if (room.finish.normal.root != null)
                        room.finish.normal.root.SetActive(false);
                }
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
                if (normalPlayer != null)
                {
                    if (ruleType >= 0 && ruleType < (normalPlayer.winners == null ? 0 : normalPlayer.winners.Length))
                    {
                        gameObject = normalPlayer.winners[ruleType];
                        if (gameObject != null)
                            gameObject.SetActive(true);
                    }

                    MahjongFinshTileStyle style;
                    int styleCount = normalPlayer.handTiles.tiles.styles == null ? 0 : normalPlayer.handTiles.tiles.styles.Length,
                        textureCount = room.textures == null ? 0 : room.textures.Length;
                    temp = 0;
                    if(temp < styleCount)
                    {
                        style = normalPlayer.handTiles.tiles.styles[temp];
                        if(style != null)
                        {
                            gameObject = style.gameObject;
                            if (gameObject != null)
                                gameObject.SetActive(true);

                            if (style.image != null)
                                style.image.texture = code < textureCount ? room.textures[code] : null;

                            if (style.final != null)
                                style.final.SetActive(true);
                        }

                        ++temp;
                    }

                    if(__handles != null)
                    {
                        foreach(Handle handle in __handles)
                        {
                            if (temp < styleCount)
                            {
                                style = normalPlayer.handTiles.tiles.styles[temp];
                                if (style != null)
                                {
                                    gameObject = style.gameObject;
                                    if (gameObject != null)
                                        gameObject.SetActive(true);

                                    if (style.image != null)
                                        style.image.texture = handle.tile.code < textureCount ? room.textures[handle.tile.code] : null;

                                    if (style.final != null)
                                        style.final.SetActive(true);
                                }

                                ++temp;
                            }
                            else
                                break;
                        }
                    }

                    if(__caches != null)
                    {
                        foreach (Cache cache in __caches)
                        {
                            if (temp < styleCount)
                            {
                                style = normalPlayer.handTiles.tiles.styles[temp];
                                if (style != null)
                                {
                                    gameObject = style.gameObject;
                                    if (gameObject != null)
                                        gameObject.SetActive(true);

                                    if (style.image != null)
                                        style.image.texture = cache.tile.code < textureCount ? room.textures[cache.tile.code] : null;

                                    if (style.final != null)
                                        style.final.SetActive(true);
                                }

                                ++temp;
                            }
                            else
                                break;
                        }
                    }

                    Dictionary<byte, Group>.ValueCollection groups = __groups == null ? null : __groups.Values;
                    MahjongFinshTileStyle[] styles;
                    Dictionary<byte, Group>.ValueCollection.Enumerator enumerator = groups.GetEnumerator();
                    Group group;
                    int groupCount = normalPlayer.handTiles.groups == null ? 0 : normalPlayer.handTiles.groups.Length, i, j;
                    for (i = 0; i < groupCount; ++i)
                    {
                        if (!enumerator.MoveNext())
                            break;

                        group = enumerator.Current;
                        styles = normalPlayer.handTiles.groups[i].styles;
                        styleCount = Mathf.Min(group.tiles == null ? 0 : group.tiles.Length, styles == null ? 0 : styles.Length);
                        for(j = 0; j < styleCount; ++j)
                        {
                            style = styles[j];
                            if (style != null)
                            {
                                gameObject = style.gameObject;
                                if (gameObject != null)
                                    gameObject.SetActive(true);

                                if (style.image != null)
                                {
                                    temp = group.tiles[j];
                                    style.image.texture = temp < textureCount ? room.textures[temp] : null;
                                }

                                if (style.final != null)
                                    style.final.SetActive(true);
                            }
                        }

                        if(group.tile != null && ++j < styleCount)
                        {
                            style = styles[j];
                            if (style != null)
                            {
                                gameObject = style.gameObject;
                                if (gameObject != null)
                                    gameObject.SetActive(true);

                                if (style.image != null)
                                {
                                    temp = (Mahjong.Tile)group.tile;
                                    style.image.texture = temp < textureCount ? room.textures[temp] : null;
                                }

                                if (style.final != null)
                                    style.final.SetActive(true);
                            }
                        }
                    }
                }
            }

            if (index < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
            {
                winPlayer = room.finish.win.players[index];
                if (winPlayer != null)
                {
                    if (ruleType >= 0 && ruleType < (winPlayer.winners == null ? 0 : winPlayer.winners.Length))
                    {
                        gameObject = winPlayer.winners[ruleType];
                        if (gameObject != null)
                            gameObject.SetActive(true);
                    }


                    MahjongFinshTileStyle style;
                    int styleCount = winPlayer.handTiles.tiles.styles == null ? 0 : winPlayer.handTiles.tiles.styles.Length,
                        textureCount = room.textures == null ? 0 : room.textures.Length;
                    temp = 0;
                    if (temp < styleCount)
                    {
                        style = winPlayer.handTiles.tiles.styles[temp];
                        if (style != null)
                        {
                            gameObject = style.gameObject;
                            if (gameObject != null)
                                gameObject.SetActive(true);

                            if (style.image != null)
                                style.image.texture = code < textureCount ? room.textures[code] : null;

                            if (style.final != null)
                                style.final.SetActive(true);
                        }

                        ++temp;
                    }

                    if (__handles != null)
                    {
                        foreach (Handle handle in __handles)
                        {
                            if (temp < styleCount)
                            {
                                style = winPlayer.handTiles.tiles.styles[temp];
                                if (style != null)
                                {
                                    gameObject = style.gameObject;
                                    if (gameObject != null)
                                        gameObject.SetActive(true);

                                    if (style.image != null)
                                        style.image.texture = handle.tile.code < textureCount ? room.textures[handle.tile.code] : null;

                                    if (style.final != null)
                                        style.final.SetActive(true);
                                }

                                ++temp;
                            }
                            else
                                break;
                        }
                    }

                    if (__caches != null)
                    {
                        foreach (Cache cache in __caches)
                        {
                            if (temp < styleCount)
                            {
                                style = winPlayer.handTiles.tiles.styles[temp];
                                if (style != null)
                                {
                                    gameObject = style.gameObject;
                                    if (gameObject != null)
                                        gameObject.SetActive(true);

                                    if (style.image != null)
                                        style.image.texture = cache.tile.code < textureCount ? room.textures[cache.tile.code] : null;

                                    if (style.final != null)
                                        style.final.SetActive(true);
                                }

                                ++temp;
                            }
                            else
                                break;
                        }
                    }

                    Dictionary<byte, Group>.ValueCollection groups = __groups == null ? null : __groups.Values;
                    MahjongFinshTileStyle[] styles;
                    Dictionary<byte, Group>.ValueCollection.Enumerator enumerator = groups.GetEnumerator();
                    Group group;
                    int groupCount = winPlayer.handTiles.groups == null ? 0 : winPlayer.handTiles.groups.Length, i, j;
                    for (i = 0; i < groupCount; ++i)
                    {
                        if (!enumerator.MoveNext())
                            break;

                        group = enumerator.Current;
                        styles = winPlayer.handTiles.groups[i].styles;
                        styleCount = Mathf.Min(group.tiles == null ? 0 : group.tiles.Length, styles == null ? 0 : styles.Length);
                        for (j = 0; j < styleCount; ++j)
                        {
                            style = styles[j];
                            if (style != null)
                            {
                                gameObject = style.gameObject;
                                if (gameObject != null)
                                    gameObject.SetActive(true);

                                if (style.image != null)
                                {
                                    temp = group.tiles[j];
                                    style.image.texture = temp < textureCount ? room.textures[temp] : null;
                                }

                                if (style.final != null)
                                    style.final.SetActive(true);
                            }
                        }

                        if (group.tile != null && ++j < styleCount)
                        {
                            style = styles[j];
                            if (style != null)
                            {
                                gameObject = style.gameObject;
                                if (gameObject != null)
                                    gameObject.SetActive(true);

                                if (style.image != null)
                                {
                                    temp = (Mahjong.Tile)group.tile;
                                    style.image.texture = temp < textureCount ? room.textures[temp] : null;
                                }

                                if (style.final != null)
                                    style.final.SetActive(true);
                            }
                        }
                    }
                }
            }
            
            if (offset > 0)
            {
                if (playerIndex < (room.finish.normal.players == null ? 0 : room.finish.normal.players.Length))
                {
                    player = room.finish.normal.players[playerIndex];
                    GameObject[] losers = player == null ? null : player.losers;
                    if (ruleType >= 0 && ruleType < (losers == null ? 0 : losers.Length))
                    {
                        gameObject = losers[ruleType];
                        if (gameObject != null)
                            gameObject.SetActive(true);
                    }
                }

                if (playerIndex < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
                {
                    player = room.finish.win.players[playerIndex];
                    GameObject[] losers = player == null ? null : player.losers;
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
                    int length = room.finish.normal.players == null ? 0 : room.finish.normal.players.Length;
                    for(int i = 1; i < length; ++i)
                    {
                        player = room.finish.normal.players[i];
                        if (player != null)
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

                if (room.finish.win.players != null)
                {
                    int length = room.finish.win.players == null ? 0 : room.finish.win.players.Length;
                    for (int i = 1; i < length; ++i)
                    {
                        player = room.finish.win.players[i];
                        if (player != null)
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
        }
        
        int result = 0, 
            normalScores = (normalPlayer == null || normalPlayer.scores == null) ? 0 : normalPlayer.scores.Length, 
            winScores = (winPlayer == null || winPlayer.scores == null) ? 0 : winPlayer.scores.Length;
        byte scoreType;
        MahjongFinishPlayerStyle.Score score;
        while(true)
        {
            scoreType = reader.ReadByte();
            if (scoreType == 255)
                break;

            temp = reader.ReadByte();
            result += temp;
            if (normalPlayer != null)
            {
                if (scoreType < normalScores)
                {
                    score = normalPlayer.scores[scoreType];
                    if (score.root != null)
                        score.root.SetActive(true);

                    if (score.text != null)
                        score.text.text = temp.ToString();
                }
            }

            if (winPlayer != null)
            {
                if (scoreType < winScores)
                {
                    score = winPlayer.scores[scoreType];
                    if (score.root != null)
                        score.root.SetActive(true);

                    if (score.text != null)
                        score.text.text = temp.ToString();
                }
            }
        }
        
        if (normalPlayer != null && normalPlayer.score != null)
            normalPlayer.score.text = result.ToString();

        if (winPlayer != null && winPlayer.score != null)
            winPlayer.score.text = result.ToString();
        
        if (offset > 0)
        {
            if (playerIndex < (room.finish.normal.players == null ? 0 : room.finish.normal.players.Length))
            {
                player = room.finish.normal.players[playerIndex];
                Text text = player == null ? null : player.score;
                if (text != null)
                {
                    if (int.TryParse(text.text, out temp))
                        text.text = (-result - temp).ToString();
                    else
                        text.text = (-result).ToString();
                }
            }

            if (playerIndex < (room.finish.win.players == null ? 0 : room.finish.win.players.Length))
            {
                player = room.finish.win.players[playerIndex];
                Text text = player == null ? null : player.score;
                if (text != null)
                {
                    if (int.TryParse(text.text, out temp))
                        text.text = (-result - temp).ToString();
                    else
                        text.text = (-result).ToString();
                }
            }
        }
        else
        {
            if (room.finish.normal.players != null)
            {
                int length = room.finish.normal.players == null ? 0 : room.finish.normal.players.Length;
                for (int i = 1; i < length; ++i)
                {
                    player = room.finish.normal.players[i];
                    if (player != null)
                    {
                        if (player.score != null)
                        {
                            if (int.TryParse(player.score.text, out temp))
                                player.score.text = (-result - temp).ToString();
                            else
                                player.score.text = (-result).ToString();
                        }
                    }
                }
            }

            if (room.finish.win.players != null)
            {
                int length = room.finish.win.players == null ? 0 : room.finish.win.players.Length;
                for (int i = 1; i < length; ++i)
                {
                    player = room.finish.win.players[i];
                    if (player != null)
                    {
                        if (player.score != null)
                        {
                            if (int.TryParse(player.score.text, out temp))
                                player.score.text = (-result - temp).ToString();
                            else
                                player.score.text = (-result).ToString();
                        }
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

        byte flag = reader.ReadByte();
        __isShow = (flag & (1 << (int)MahjongPlayerStatus.Show)) != 0;

        byte length = reader.ReadByte(), count, i, j;
        Mahjong.RuleType type;
        Mahjong.Tile tile;
        Group group;
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

            group = new Group(i, count);
            for (j = 0; j < count; ++j)
            {
                tile = reader.ReadByte();
                if (j < 3)
                    group.tiles[j] = tile;
                else
                    group.tile = tile;

                asset = room.next;
                if (__isShow)
                    asset.Visible();
                else
                    asset.Hide();

                room.As(asset, tile);
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

            __groups.Add(i, group);
        }

        length = reader.ReadByte();
        for (i = 0; i < length; ++i)
        {
            tile = reader.ReadByte();
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
                    room.Throw(asset, __scoreCount++);
                    break;
                default:
                    room.Discard(asset, __discardCount++);
                    if((flag & (1 << (int)MahjongPlayerStatus.Turn)) != 0)
                    {
                        if (room.arrow != null)
                        {
                            GameObject gameObject = room.arrow.gameObject;
                            if (gameObject != null)
                                gameObject.SetActive(true);

                            room.arrow.localPosition = transform.localPosition;

                            room.arrow.SetParent(this.transform, false);
                        }
                    }

                    __asset = asset;
                    break;
            }
        }

        MahjongClient host = isLocalPlayer ? base.host as MahjongClient : null;
        Tile instance;
        length = reader.ReadByte();
        for (i = 0; i < length; ++i)
        {
            instance = new Tile(reader.ReadByte(), reader.ReadByte(), room == null ? null : room.next);
            transform = instance.asset == null ? null : instance.asset.transform;
            if (transform == null)
                continue;

            transform.SetParent(this.transform, false);
            transform.localPosition = room.handPosition + new Vector3(__Add(instance) * room.width, 0.0f, 0.0f);

            if (__isShow)
                instance.asset.Visible();
            else
                instance.asset.Hide();

            if (host != null)
            {
                room.As(instance.asset, host.GetTile(instance.code));

                __Discard(instance);
            }
            
            ++__drawCount;
        }
    }

    private void RpcHold(float time)
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room != null && room.wind != null)
            room.wind.SetTrigger(type.ToString());

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
        transform.localPosition = room.handPosition + new Vector3(count * room.width + room.offset, 0.0f, 0.0f);
        if (__isShow)
            asset.Visible();
        else
            asset.Hide();

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

        GameObject gameObject = room.arrow == null ? null : room.arrow.gameObject;
        if (gameObject != null)
            gameObject.SetActive(false);

        Mahjong.Tile tile;
        if (playerIndex == index)
            tile = __tile;
        else
        {
            Client host = base.host as Client;
            MahjongClientPlayer player = host == null ? null : host.Get(playerIndex) as MahjongClientPlayer;
            MahjongAsset asset = player == null ? null : player.__asset;
            Transform transform = asset == null ? null : asset.transform;
            if (transform == null)
                return;

            --player.__discardCount;

            transform.SetParent(this.transform, false);
            room.Group(asset, temp.index, temp.count);

            tile = player.__tile;
        }

        if (temp.count < (temp.tiles == null ? 0 : temp.tiles.Length))
            temp.tiles[temp.count] = tile;
        else
            temp.tile = tile;

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

    private void CmdReady(byte code)
    {
        NetworkWriter writer = new NetworkWriter();
        writer.Write(code);

        Rpc((short)MahjongNetworkRPCHandle.Ready, writer.AsArray(), writer.Position);
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
        RegisterHandler((short)MahjongNetworkRPCHandle.Score, __OnScore);

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
                for (LinkedListNode<Handle> hanldeNode = __handles == null ? null : __handles.First; hanldeNode != null; hanldeNode = hanldeNode.Next)
                {
                    handle = hanldeNode.Value;
                    if (handle.tile.asset != null)
                    {
                        for (selectorNode = __selectors.First; selectorNode != null; selectorNode = selectorNode.Next)
                        {
                            selector = selectorNode.Value;
                            if (selector.index == handle.tile.index)
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
                }
            }
        }
    }
}
