using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking;
using ZG.Network.Lobby;

public class MahjongClient : Client
{
    public float readyTime = 5.0f;
    public float tryTime = 5.0f;
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
        
        RegisterHandler((short)MahjongNetworkMessageType.TileCodes, __OnTileCodes);
        RegisterHandler((short)MahjongNetworkMessageType.ReadyHand, __OnReadyHand);
        RegisterHandler((short)MahjongNetworkMessageType.RuleNodes, __OnRuleNodes);
    }

    private void __OnReady()
    {
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room != null)
        {
            if (room.finish.root != null)
                room.finish.root.SetActive(false);

            if (room.finish.normal.root != null)
                room.finish.normal.root.SetActive(false);

            if (room.finish.win.root != null)
                room.finish.win.root.SetActive(false);
        }

        Node node = localPlayer as Node;
        if (node != null)
            node.Ready();
    }

    private void __OnNotReady(int roomIndex, int count)
    {
        if (count == 0)
        {
            MahjongClientRoom room = MahjongClientRoom.instance;
            if (room != null)
            {
                if ((room.finish.normal.root == null || !room.finish.normal.root.activeSelf) &&
                    (room.finish.win.root == null || !room.finish.win.root.activeSelf))
                {
                    if(room.finish.root != null)
                        room.finish.root.SetActive(true);

                    if (room.finish.button != null)
                    {
                        Button.ButtonClickedEvent onClick = room.finish.button.onClick;
                        if (onClick == null)
                            onClick = new Button.ButtonClickedEvent();
                        else
                            onClick.RemoveAllListeners();

                        onClick.AddListener(__OnReady);
                    }
                }

                if (room.finish.normal.ready != null)
                {
                    Button.ButtonClickedEvent onClick = room.finish.normal.ready.onClick;
                    if (onClick == null)
                        onClick = new Button.ButtonClickedEvent();
                    else
                        onClick.RemoveAllListeners();

                    onClick.AddListener(__OnReady);
                }

                if (room.finish.win.ready != null)
                {
                    Button.ButtonClickedEvent onClick = room.finish.win.ready.onClick;
                    if (onClick == null)
                        onClick = new Button.ButtonClickedEvent();
                    else
                        onClick.RemoveAllListeners();

                    onClick.AddListener(__OnReady);
                }
            }
        }
    }
    
    private void __OnTileCodes(NetworkMessage message)
    {
        MahjongTileCodeMessage tileCodeMessage = message == null ? null : message.ReadMessage<MahjongTileCodeMessage>();
        if (tileCodeMessage == null)
            return;

        if (__tiles != null)
            __tiles.Clear();

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

    private void __OnReadyHand(NetworkMessage message)
    {
        MahjongReadyHandMessage readyHandMessage = message == null ? null : message.ReadMessage<MahjongReadyHandMessage>();
        if (readyHandMessage == null)
            return;

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        GameObject gameObject = room.pass.gameObject;
        if (gameObject != null)
            gameObject.SetActive(true);

        Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
        room.pass.onClick = buttonClickedEvent;
        buttonClickedEvent.AddListener(__PassReady);

        if (room.show != null)
        {
            gameObject = room.show.gameObject;
            if (gameObject != null)
                gameObject.SetActive(true);

            int count = readyHandMessage.indices == null ? 0 : readyHandMessage.indices.Count;
            buttonClickedEvent = new Button.ButtonClickedEvent();
            room.show.onClick = buttonClickedEvent;
            UnityAction handler = delegate ()
            {
                MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    player.Select(readyHandMessage.indices[i], delegate ()
                    {
                        player.Ready((byte)((index << 2) | (int)MahjongReadyType.Show));

                        __ClearReady();
                    });
                }
            };

            buttonClickedEvent.AddListener(handler);
        }

        if (room.hide != null)
        {
            gameObject = room.hide.gameObject;
            if (gameObject != null)
                gameObject.SetActive(true);

            int count = readyHandMessage.indices == null ? 0 : readyHandMessage.indices.Count;
            buttonClickedEvent = new Button.ButtonClickedEvent();
            room.hide.onClick = buttonClickedEvent;
            UnityAction handler = delegate ()
            {
                MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    player.Select(readyHandMessage.indices[i], delegate ()
                    {
                        player.Ready((byte)((index << 2) | (int)MahjongReadyType.Hide));

                        __ClearReady();
                    });
                }
            };

            buttonClickedEvent.AddListener(handler);
        }


        Invoke("__ClearReady", readyTime);
    }

    private void __OnRuleNodes(NetworkMessage message)
    {
        MahjongRuleMessage ruleMessage = message == null ? null : message.ReadMessage<MahjongRuleMessage>();
        if (ruleMessage == null || ruleMessage.ruleNodes == null)
            return;
        
        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        int index = 0;
        List<int> chowIndices = null, pongIndices = null, kongIndices = null, winIndices = null;
        foreach(Mahjong.RuleNode ruleNode in ruleMessage.ruleNodes)
        {
            switch(ruleNode.type)
            {
                case Mahjong.RuleType.Chow:
                    if (chowIndices == null)
                        chowIndices = new List<int>();

                    chowIndices.Add(index);
                    break;
                case Mahjong.RuleType.Pong:
                    if (pongIndices == null)
                        pongIndices = new List<int>();

                    pongIndices.Add(index);
                    break;
                case Mahjong.RuleType.Kong:
                case Mahjong.RuleType.HiddenKong:
                case Mahjong.RuleType.MeldedKong:
                    if (kongIndices == null)
                        kongIndices = new List<int>();

                    kongIndices.Add(index);
                    break;
                case Mahjong.RuleType.Win:
                case Mahjong.RuleType.SelfDraw:
                case Mahjong.RuleType.BreakKong:
                case Mahjong.RuleType.OverKong:
                    if (winIndices == null)
                        winIndices = new List<int>();

                    winIndices.Add(index);
                    break;
            }

            ++index;
        }

        bool isSetEvent = false;
        if(chowIndices != null)
            isSetEvent = __SetEvent(ruleMessage.ruleNodes, chowIndices.AsReadOnly(), room.chow) || isSetEvent;

        if (pongIndices != null)
            isSetEvent = __SetEvent(ruleMessage.ruleNodes, pongIndices.AsReadOnly(), room.pong) || isSetEvent;

        if (kongIndices != null)
            isSetEvent = __SetEvent(ruleMessage.ruleNodes, kongIndices.AsReadOnly(), room.kong) || isSetEvent;

        if (winIndices != null)
            isSetEvent = __SetEvent(ruleMessage.ruleNodes, winIndices.AsReadOnly(), room.win) || isSetEvent;

        if (isSetEvent)
        {
            GameObject gameObject = room.pass.gameObject;
            if (gameObject != null)
                gameObject.SetActive(true);

            Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
            room.pass.onClick = buttonClickedEvent;
            buttonClickedEvent.AddListener(__PassTry);

            Invoke("__ClearTry", tryTime);
        }
    }
    
    private void __PassReady()
    {
        MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
        if (player != null)
            player.Ready(255);

        __ClearReady();
    }

    private void __PassTry()
    {
        MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
        if (player != null)
            player.Try(255);

        __ClearTry();
    }

    private bool __SetEvent(ReadOnlyCollection<Mahjong.RuleNode> ruleNodes, ReadOnlyCollection<int> ruleIndices, Button button)
    {
        if (button == null)
            return false;

        int numRules = ruleIndices == null ? 0 : ruleIndices.Count;
        if (numRules < 1)
            return false;

        GameObject gameObject = button.gameObject;
        if (gameObject != null)
            gameObject.SetActive(true);

        Button.ButtonClickedEvent buttonClickedEvent = new Button.ButtonClickedEvent();
        button.onClick = buttonClickedEvent;

        UnityAction listener = null;
        listener = delegate()
        {
            MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
            if (player == null)
                return;

            if (numRules < 2)
            {
                player.Try((byte)ruleIndices[0]);

                __ClearTry();
            }
            else
            {
                foreach (int ruleIndex in ruleIndices)
                {
                    int temp = ruleIndex;
                    player.Select(ruleNodes[temp].index, delegate ()
                    {
                        player.Try((byte)temp);

                        __ClearTry();
                    });
                }
            }
        };

        buttonClickedEvent.AddListener(listener);

        return true;
    }

    private void __ClearReady()
    {
        CancelInvoke("__ClearReady");

        MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
        if (player != null)
            player.Unselect();

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        if (room.pass != null)
        {
            room.pass.onClick = null;

            GameObject gameObject = room.pass.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.show != null)
        {
            room.show.onClick = null;

            GameObject gameObject = room.show.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.hide != null)
        {
            room.hide.onClick = null;

            GameObject gameObject = room.hide.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }

    private void __ClearTry()
    {
        CancelInvoke("__ClearTry");

        MahjongClientPlayer player = localPlayer as MahjongClientPlayer;
        if (player != null)
            player.Unselect();

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room == null)
            return;

        if(room.pass != null)
        {
            room.pass.onClick = null;

            GameObject gameObject = room.pass.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.chow != null)
        {
            room.chow.onClick = null;

            GameObject gameObject = room.chow.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.pong != null)
        {
            room.pong.onClick = null;

            GameObject gameObject = room.pong.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.kong != null)
        {
            room.kong.onClick = null;

            GameObject gameObject = room.kong.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        if (room.win != null)
        {
            room.win.onClick = null;

            GameObject gameObject = room.win.gameObject;
            if (gameObject != null)
                gameObject.SetActive(false);
        }
    }

    void Awake()
    {
        onNotReady += __OnNotReady;
    }
}
