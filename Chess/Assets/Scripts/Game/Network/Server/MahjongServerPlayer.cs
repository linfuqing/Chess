using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using ZG.Network;

public class MahjongServerPlayer : ServerObject
{
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

    public void Draw(byte index, byte code)
    {
        RpcDraw(index, code);
    }

    public void Throw(byte index, byte group, Mahjong.Tile instance)
    {
        RpcThrow(index, group, instance);
    }

    public void Try(Mahjong.RuleType type)
    {
        RpcTry(type);
    }

    public void Do(Mahjong.RuleType type, byte group)
    {
        RpcDo(type, group);
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

    private void RpcTry(Mahjong.RuleType type)
    {
        NetworkWriter writer = RpcStart();
        writer.Write((byte)node.type);
        RpcEnd((short)MahjongNetworkRPCHandle.Try);
    }

    private void RpcDo(Mahjong.RuleType type, byte group)
    {
        NetworkWriter writer = RpcStart();
        writer.Write((byte)type);
        writer.Write(group);
        RpcEnd((short)MahjongNetworkRPCHandle.Do);
    }
}
