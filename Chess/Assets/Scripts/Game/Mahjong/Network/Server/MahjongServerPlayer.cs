﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZG.Network;

public class MahjongServerPlayer : ServerObject
{
    public new void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator Wait(byte index, short handle, float timeout, Action<byte> onComplete)
    {
        if (onComplete == null)
            yield break;

        byte result = 255;
        RegisterHandler(handle, delegate (NetworkReader reader)
        {
            if (reader == null)
                return;

            result = reader.ReadByte();
        });

        RpcHold(timeout);

        float time = Time.time + timeout;
        do
        {
            yield return null;
            if(result != 255)
            {
                index = result;

                break;
            }
        } while (Time.time < time);

        Node node = base.node;
        if (node != null)
            node.UnregisterHandler(handle);

        onComplete(index);
    }
    
    public void Show(IEnumerable<KeyValuePair<int, int>> handTileIndices)
    {
        RpcShow(handTileIndices);
    }

    public void Draw(byte index, byte code)
    {
        RpcDraw(index, code);
    }

    public void Throw(byte index, byte group, Mahjong.Tile instance)
    {
        RpcThrow(index, group, instance);
    }

    public void Ready(bool isShow)
    {
        RpcReady(isShow);
    }

    public void Try(Mahjong.RuleType type)
    {
        RpcTry(type);
    }

    public void Do(short playerIndex, Mahjong.RuleType type, byte group)
    {
        RpcDo(playerIndex, type, group);
    }
    
    private void RpcHold(float time)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(time);

        RpcEnd((short)MahjongNetworkRPCHandle.Hold);
    }
    
    private void RpcDraw(byte index, byte code)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(index);
        writer.Write(code);

        RpcEnd((short)MahjongNetworkRPCHandle.Draw);
    }

    private void RpcThrow(byte index, byte group, Mahjong.Tile instance)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(index);
        writer.Write(group);
        writer.Write(instance);
        RpcEnd((short)MahjongNetworkRPCHandle.Throw);
    }

    private void RpcReady(bool isShow)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(isShow);
        RpcEnd((short)MahjongNetworkRPCHandle.Ready);
    }

    private void RpcTry(Mahjong.RuleType type)
    {
        NetworkWriter writer = RpcStart();
        writer.Write((byte)node.type);
        RpcEnd((short)MahjongNetworkRPCHandle.Try);
    }

    private void RpcDo(short playerIndex, Mahjong.RuleType type, byte group)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(playerIndex);
        writer.Write((byte)type);
        writer.Write(group);
        RpcEnd((short)MahjongNetworkRPCHandle.Do);
    }
    
    private void RpcShow(IEnumerable<KeyValuePair<int, int>> handTileIndices)
    {
        NetworkWriter writer = RpcStart();
        if (handTileIndices != null)
        {
            foreach (KeyValuePair<int, int> pair in handTileIndices)
            {
                writer.Write((byte)pair.Key);
                writer.Write(Mahjong.Tile.Get(pair.Value));
            }
        }

        writer.Write((byte)255);
        RpcEnd((short)MahjongNetworkRPCHandle.Show);
    }
}
