using System;
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
        public Action onSelected;
        public Action onDiscard;

        public Handle(Tile tile, float velocity, Action onSelected, Action onDiscard)
        {
            this.tile = tile;
            this.velocity = velocity;
            this.onSelected = onSelected;
            this.onDiscard = onDiscard;
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

    public MahjongAssets assets;
    private int __drawCount;
    private int __discardCount;
    private int __scoreCount;
    private int __groupCount;
    private float __time;
    private MahjongAsset __selected;
    private LinkedListNode<Cache> __handle;
    private LinkedList<Handle> __handles;
    private LinkedList<Cache> __caches;

    public void Clear()
    {
        if (__handles != null)
        {
            foreach(Handle handle in __handles)
            {
                if(handle.tile.asset != null)
                    Destroy(handle.tile.asset.gameObject);
            }

            __handles.Clear();
        }
    }

    private void __Throw(Tile tile, Mahjong.Tile instance)
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
                assets.Score(tile.asset, __scoreCount++);
                break;
            default:
                assets.Discard(tile.asset, __discardCount++);
                break;
        }
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
            __handle = new LinkedListNode<Cache>(result);
        
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
                    handle.tile.asset.onSelected -= handle.onSelected;
                    handle.tile.asset.onDiscard -= handle.onDiscard;
                }

                __Throw(node.Value.tile, instance);
            }
            
            if (__handle != null)
            {
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
                transform = handle.tile.asset == null ? null : handle.tile.asset.transform;
                if (transform != null)
                {
                    position = transform.localPosition;
                    position.x = Mathf.SmoothDamp(position.x, assets.handPosition.x + assets.width * index, ref handle.velocity, assets.smoothTime, assets.maxSpeed);
                    transform.localPosition = position;
                }

                node.Value = handle;

                ++index;
            }
        }
        else if(__caches != null)
        {

            LinkedListNode<Cache> cacheNode;
            Cache cache;
            Action onSelected, onDiscard;
            LinkedListNode<Handle> node;
            Handle result, temp;
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
                        if (__handles == null)
                            __handles = new LinkedList<Handle>();

                        if (cache.tile.asset != null && isLocalPlayer)
                        {
                            Tile tile = cache.tile;
                            onSelected = delegate ()
                            {
                                if (__time > 0.0f)
                                    return;

                                if (__selected != null)
                                    __selected.isSelected = false;

                                tile.asset.isSelected = true;

                                __selected = tile.asset;
                            };

                            onDiscard = delegate ()
                            {
                                if (__time > 0.0f)
                                    return;

                                if (__selected == tile.asset)
                                {
                                    __selected.isSelected = false;

                                    __selected = null;
                                }

                                CmdDiscard(tile.index);
                            };

                            tile.asset.onSelected += onSelected;
                            tile.asset.onDiscard += onDiscard;
                        }
                        else
                        {
                            onSelected = null;
                            onDiscard = null;
                        }

                        node = null;
                        result = new Handle(cache.tile, 0.0f, onSelected, onDiscard);
                        for (node = __handles.First; node != null; node = node.Next)
                        {
                            temp = node.Value;
                            if (temp.tile.code > cache.tile.code)
                            {
                                __handles.AddBefore(node, result);

                                break;
                            }
                        }

                        if (node == null)
                            __handles.AddLast(result);

                        if (cache.tile.asset != null)
                            cache.tile.asset.Move();

                        __time = assets.moveTime;
                    }

                    break;
                }

            }
        }
    }
}
