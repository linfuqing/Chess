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

    public void Throw(byte index, Mahjong.Tile instance)
    {
        RpcThrow(index, instance);
    }

    private void RpcDraw(byte index, byte code)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(index);
        writer.Write(code);

        RpcEnd((short)MahjongNetworkRPCHandle.Draw);
    }

    private void RpcThrow(byte index, Mahjong.Tile instance)
    {
        NetworkWriter writer = RpcStart();
        writer.Write(index);
        writer.Write(instance);
        RpcEnd((short)MahjongNetworkRPCHandle.Throw);
    }
}
