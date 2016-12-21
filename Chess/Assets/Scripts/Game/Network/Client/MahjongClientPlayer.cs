using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        public Mahjong.Tile? instance;

        public Cache(Tile tile, Mahjong.Tile? instance)
        {
            this.tile = tile;
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
    
    public MahjongAssets assets;
    private int __drawCount;
    private int __discardCount;
    private int __scoreCount;
    private int __groupCount;
    private float __time;
    private LinkedListNode<Cache> __handle;
    private LinkedList<Cache> __caches;
    private LinkedList<Handle> __handles;
    private LinkedList<Selector> __selectors;
    private List<MahjongAsset> __selectedAssets;

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

    private void __Throw(Tile tile, Mahjong.Tile instance)
    {
        if (assets == null)
            return;

        if (tile.asset != null)
        {
            tile.asset.onSelected = null;
            tile.asset.onDiscard = null;

            if (assets.textures != null && tile.asset.renderer != null)
            {
                byte code = instance;
                if (code < assets.textures.Length)
                    tile.asset.renderer.material.mainTexture = assets.textures[code];
            }
        }

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
                assets.Score(tile.asset, __scoreCount++);
                break;
            default:
                assets.Discard(tile.asset, __discardCount++);
                break;
        }
    }

    private void __Discard(Tile tile)
    {
        if (tile.asset == null)
            return;

        tile.asset.onDiscard = delegate ()
        {
            if (__time > 0.0f)
                return;

            CmdDiscard(tile.index);
        };
    }

    private void __OnCreate()
    {
        if(isLocalPlayer)
        {
            Camera camera = Camera.main;
            if (camera != null)
                camera.transform.SetParent(transform, false);
        }
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
        
        RpcThrow(reader.ReadByte(), reader.ReadByte());
    }
    
    private void RpcDraw(byte index, byte code)
    {
        if (assets == null)
            return;

        MahjongAsset asset = null;
        if (isLocalPlayer)
        {
            MahjongClient host = base.host as MahjongClient;
            if (host != null)
                asset = assets.Create(host.GetTile(code));
        }

        if (asset == null)
            asset = assets.Create();

        Transform transform = asset == null ? null : asset.transform;
        if (transform == null)
            return;

        int count = (__handles == null ? 0 : __handles.Count) + (__caches == null ? 0 : __caches.Count);
        transform.SetParent(this.transform, false);
        transform.localPosition = assets.handPosition + new Vector3(count * assets.width + assets.offset, 0.0f, 0.0f);

        Cache result = new Cache(new Tile(index, code, asset), null);
        if (__caches == null)
            __caches = new LinkedList<Cache>();

        if (count < (13 - __groupCount * 3))
            __caches.AddLast(result);
        else
        {
            __handle = new LinkedListNode<Cache>(result);

            __Discard(result.tile);
        }
        
        ++__drawCount;
    }

    private void RpcThrow(byte index, Mahjong.Tile instance)
    {
        if (__handle != null && __handle.Value.tile.index == index)
        {
            if (__caches == null)
                __caches = new LinkedList<Cache>();

            __handle.Value = new Cache(__handle.Value.tile, instance);
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
                    cacheNode.Value = new Cache(cacheNode.Value.tile, instance);

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

                __Throw(node.Value.tile, instance);
            }
            
            if (__handle != null)
            {
                MahjongAsset asset = __handle.Value.tile.asset;
                if (asset != null)
                    asset.onDiscard = null;

                if (__caches == null)
                    __caches = new LinkedList<Cache>();

                __caches.AddLast(__handle);
            }
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
        RegisterHandler((short)MahjongNetworkRPCHandle.Draw, __OnDraw);
        RegisterHandler((short)MahjongNetworkRPCHandle.Throw, __OnThrow);

        onCreate += __OnCreate;
    }

    void Update()
    {
        if (assets == null)
            return;

        __time -= Time.deltaTime;
        if (__time > 0.0f)
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
                    position.x = Mathf.SmoothDamp(position.x, assets.handPosition.x + assets.width * index, ref handle.velocity, assets.smoothTime, assets.maxSpeed);
                    transform.localPosition = position;


                    node.Value = handle;
                }
                
                ++index;
            }
        }
        else if(__caches != null)
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
                        __Throw(cache.tile, (Mahjong.Tile)cache.instance);

                        __time = assets.throwTime;
                    }
                    else if (cache.tile.asset != null)
                    {
                        if (cache.tile.asset != null && isLocalPlayer)
                            __Discard(cache.tile);

                        __Add(cache.tile);

                        if (cache.tile.asset != null)
                            cache.tile.asset.Move();

                        __time = assets.moveTime;
                    }

                    break;
                }
            }
            
            if(__caches.Count < 1 && __selectors != null && __selectors.Count > 0)
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
                        for(selectorNode = __selectors.First; selectorNode != null; selectorNode = selectorNode.Next)
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
