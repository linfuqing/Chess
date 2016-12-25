using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;

public enum MahjongNetworkMessageType
{
    Shuffle = 200,
    TileCodes,
    RuleNodes
}

public enum MahjongNetworkRPCHandle
{
    Hold = 10, 
    Draw,
    Throw, 
    Try, 
    Do
}

public class MahjongShuffleMessage : MessageBase
{
    public byte points;
    public short dealerIndex;

    public MahjongShuffleMessage(byte points, short dealerIndex)
    {
        this.points = points;
        this.dealerIndex = dealerIndex;
    }

    public MahjongShuffleMessage()
    {

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

public class NameMessage : MessageBase
{
    public string username;

    public NameMessage()
    {

    }

    public NameMessage(string username)
    {
        this.username = username;
    }
}

public class InitMessage : NameMessage
{
    public string roomName;

    public InitMessage()
    {

    }

    public InitMessage(string username, string roomName) : base(username)
    {
        this.roomName = roomName;
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;

        writer.Write(username);
        writer.Write(roomName);
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;
        
        username = reader.ReadString();
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