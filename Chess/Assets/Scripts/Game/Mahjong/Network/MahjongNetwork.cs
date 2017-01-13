﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;

public enum MahjongNetworkMessageType
{
    Player = 300,
    Room,
    Init, 
    Shuffle, 
    TileCodes,
    ReadyHand, 
    RuleNodes
}

public enum MahjongNetworkRPCHandle
{
    Init = 10, 
    Hold, 
    Draw,
    Throw, 
    Ready, 
    Try, 
    Do, 
    Score
}

public enum MahjongScoreType : byte
{
    Normal,
    SevenPairsHand, 
    AllPongHand, 
    SimpleHand,
    CleanHand,
    PureHand,
    GreatHand258, 
    KongDraw,
    FlowerDraw, 
    Single,
    HeavenlyHand,
    EarthlyHand
}

public class NameMessage : MessageBase
{
    public string name;

    public NameMessage()
    {

    }

    public NameMessage(string name)
    {
        this.name = name;
    }
}

public class RegisterMessage : NameMessage
{
    public string roomName;

    public RegisterMessage()
    {

    }

    public RegisterMessage(string username, string roomName) : base(username)
    {
        this.roomName = roomName;
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;

        writer.Write(name);
        writer.Write(roomName);
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;

        name = reader.ReadString();
        try
        {
            roomName = reader.ReadString();
        }
        catch
        {
            roomName = null;
        }
    }
}

public class MahjongShuffleMessage : MessageBase
{
    public byte point0;
    public byte point1;
    public byte point2;
    public byte point3;
    public short dealerIndex;

    public MahjongShuffleMessage(byte point0, byte point1, byte point2, byte point3, short dealerIndex)
    {
        this.point0 = point0;
        this.point1 = point1;
        this.point2 = point2;
        this.point3 = point3;
        this.dealerIndex = dealerIndex;
    }

    public MahjongShuffleMessage()
    {

    }

    /*public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;
        
        writer.Write(point0);
        writer.Write(point1);
        writer.Write(point2);
        writer.Write(point3);
        writer.Write(dealerIndex);
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;
        
        point0 = reader.ReadByte();
        point1 = reader.ReadByte();
        point2 = reader.ReadByte();
        point3 = reader.ReadByte();
        dealerIndex = reader.ReadInt16();
    }*/
}

public class MahjongInitMessage : MahjongShuffleMessage
{
    public short playerIndex;

    public MahjongInitMessage(
        byte point0, 
        byte point1, 
        byte point2, 
        byte point3, 
        short dealerIndex, 
        short playerIndex) : base(
            point0, 
            point1, 
            point2, 
            point3, 
            dealerIndex)
    {
        this.playerIndex = playerIndex;
    }
}

public class MahjongTileCodeMessage : MessageBase
{
    public byte count;
    public IEnumerable<byte> tileCodes;

    public MahjongTileCodeMessage()
    {

    }

    public MahjongTileCodeMessage(byte count, IEnumerable<byte> tileCodes)
    {
        this.count = count;
        this.tileCodes = tileCodes;
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;

        writer.Write(count);
        if(tileCodes != null)
        {
            foreach(byte tileCode in tileCodes)
                writer.Write(tileCode);
        }
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;

        count = reader.ReadByte();
        if (count > 0)
        {
            byte[] tileCodes = new byte[count];
            for (byte i = 0; i < count; ++i)
                tileCodes[i] = reader.ReadByte();

            this.tileCodes = tileCodes;
        }
    }
}

public class MahjongRuleMessage : MessageBase
{
    public ReadOnlyCollection<Mahjong.RuleNode> ruleNodes;

    public MahjongRuleMessage()
    {

    }

    public MahjongRuleMessage(ReadOnlyCollection<Mahjong.RuleNode> ruleNodes)
    {
        this.ruleNodes = ruleNodes;
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;

        int count = ruleNodes == null ? 0 : ruleNodes.Count;
        writer.Write((byte)count);
        if (ruleNodes != null)
        {
            foreach (Mahjong.RuleNode ruleNode in ruleNodes)
            {
                writer.Write((byte)ruleNode.type);
                writer.Write((byte)ruleNode.index);
                writer.Write((byte)ruleNode.offset);
            }
        }
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;

        List<Mahjong.RuleNode> ruleNodes = null;
        int count = reader.ReadByte();
        for(int i = 0; i < count; ++i)
        {
            if (ruleNodes == null)
                ruleNodes = new List<Mahjong.RuleNode>();

            ruleNodes.Add(new Mahjong.RuleNode((Mahjong.RuleType)reader.ReadByte(), reader.ReadByte(), reader.ReadByte()));
        }

        this.ruleNodes = ruleNodes == null ? null : ruleNodes.AsReadOnly();
    }
}

public class MahjongReadyHandMessage : MessageBase
{
    public MahjongReadyHandMessage()
    {

    }
}