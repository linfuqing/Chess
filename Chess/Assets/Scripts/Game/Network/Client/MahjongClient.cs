using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;
using ZG.Network.Lobby;

public class MahjongClient : Client
{
    public Button chow;
    public Button pong;
    public Button kong;
    public Button win;

    private Dictionary<byte, byte> __tiles;

    public Mahjong.Tile GetTile(byte code)
    {
        if(__tiles == null)
            return new Mahjong.Tile(Mahjong.TileType.Unknown, 0);

        byte tile;
        if (__tiles.TryGetValue(code, out tile))
            return tile;

        return new Mahjong.Tile(Mahjong.TileType.Unknown, 0);
    }

    public new void Create()
    {
        base.Create();

        RegisterHandler((short)MahjongNetworkMessageType.Shuffle, __OnShuffle);
        RegisterHandler((short)MahjongNetworkMessageType.TileCodes, __OnTileCodes);
        RegisterHandler((short)MahjongNetworkMessageType.RuleObjects, __OnRuleObjects);
    }

    private void __OnShuffle(NetworkMessage message)
    {
        MahjongShuffleMessage shuffleMessage = message == null ? null : message.ReadMessage<MahjongShuffleMessage>();
        if (shuffleMessage == null)
            return;


    }

    private void __OnTileCodes(NetworkMessage message)
    {
        MahjongTileCodeMessage tileCodeMessage = message == null ? null : message.ReadMessage<MahjongTileCodeMessage>();
        if (tileCodeMessage == null)
            return;

        if (tileCodeMessage.count > 0)
        {
            if(tileCodeMessage.tileCodes != null)
            {
                byte index = 0;
                foreach (byte tileCode in tileCodeMessage.tileCodes)
                {
                    if (__tiles == null)
                        __tiles = new Dictionary<byte, byte>();

                    __tiles.Add(tileCode, index++);
                }
            }
        }
    }

    private void __OnRuleObjects(NetworkMessage message)
    {

    }
}
