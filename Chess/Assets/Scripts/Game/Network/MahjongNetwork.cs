using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Networking;

public enum MahjongNetworkMessageType
{
    Shuffle = 200,
    TileCodes,
    RuleObjects
}

public enum MahjongNetworkRPCHandle
{
    Draw = 10,
    Throw, 
    Do,
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
    public ReadOnlyCollection<Mahjong.RuleObject> ruleObjects;

    public MahjongRuleMessage()
    {

    }

    public MahjongRuleMessage(ReadOnlyCollection<Mahjong.RuleObject> ruleObjects)
    {
        this.ruleObjects = ruleObjects;
    }

    public override void Serialize(NetworkWriter writer)
    {
        if (writer == null)
            return;

        int count = ruleObjects == null ? 0 : ruleObjects.Count;
        writer.Write((byte)count);
        if (ruleObjects != null)
        {
            foreach (Mahjong.RuleObject ruleObject in ruleObjects)
            {
                writer.Write((byte)ruleObject.instance.type);
                writer.Write((byte)ruleObject.instance.index);
                writer.Write((byte)ruleObject.instance.offset);
                writer.Write((byte)ruleObject.playerIndex);
            }
        }
    }

    public override void Deserialize(NetworkReader reader)
    {
        if (reader == null)
            return;

        List<Mahjong.RuleObject> ruleObjects = null;
        int count = reader.ReadByte();
        for(int i = 0; i < count; ++i)
        {
            if (ruleObjects == null)
                ruleObjects = new List<Mahjong.RuleObject>();

            ruleObjects.Add(new Mahjong.RuleObject(new Mahjong.RuleNode((Mahjong.RuleType)reader.ReadByte(), reader.ReadByte(), reader.ReadByte()), reader.ReadByte()));
        }

        this.ruleObjects = ruleObjects == null ? null : ruleObjects.AsReadOnly();
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