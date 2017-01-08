using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using ZG.Network;

public class MahjongClientMain : MonoBehaviour
{
    public static MahjongClientMain instance;

    public int menuSceneBuildIndex;
    public int roomSceneBuildIndex;

    private static string __uid = System.Guid.NewGuid().ToString();
    private Action __onCreateComplete;
    private MahjongClient __client;
    private MahjongShuffleMessage __shuffleMessage;
    private Coroutine __coroutine;
    private string __roomName;

    public MahjongClient client
    {
        get
        {
            return __client;
        }
    }

    public string roomName
    {
        get
        {
            return __roomName;
        }
    }

    public void Shutdown()
    {
        if (__client != null)
            __client.Shutdown();

        __coroutine = StartCoroutine(__LoadScene(menuSceneBuildIndex, null, __coroutine));
    }

    public void Create()
    {
        __client.onConnect += __OnConnect;
        __client.Create();
    }
    
    public void CreateRoom()
    {
        if (__client == null)
            return;

        if (__client.isConnected)
        {
            __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
            __client.Send((short)MahjongNetworkMessageType.Room, new UnityEngine.Networking.NetworkSystem.EmptyMessage());
        }
        else if(__onCreateComplete == null)
        {
            __onCreateComplete = CreateRoom;

            Create();
        }
    }
    
    public void JoinRoom(string roomName)
    {
        __roomName = roomName;

        if (__client.isConnected)
            __JoinRoom();
        else if (__onCreateComplete == null)
        {
            __onCreateComplete = __JoinRoom;

            Create();
        }
    }

    private void __OnRegistered(Node node)
    {
        ZG.Network.Lobby.Node temp = node as ZG.Network.Lobby.Node;
        if (temp != null && temp.isLocalPlayer)
            temp.Ready();
    }
    
    private void __OnPlayer(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Player);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        if (string.IsNullOrEmpty(nameMessage.name))
        {
            if (__onCreateComplete == null)
                __coroutine = StartCoroutine(__LoadScene(menuSceneBuildIndex, null, __coroutine));
            else
            {
                __onCreateComplete();
                __onCreateComplete = null;
            }
        }
        else
        {
            __onCreateComplete = null;

            JoinRoom(nameMessage.name);
        }
    }

    private void __OnRoom(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Room);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        JoinRoom(nameMessage.name);
    }

    private void __OnShuffle(NetworkMessage message)
    {
        __shuffleMessage = message == null ? null : message.ReadMessage<MahjongShuffleMessage>();
        if (__shuffleMessage == null)
            return;

        __InitRoom();
    }

    private void __OnConnect(NetworkMessage message)
    {
        __client.onConnect -= __OnConnect;
        __client.RegisterHandler((short)MahjongNetworkMessageType.Player, __OnPlayer);
        __client.RegisterHandler((short)MahjongNetworkMessageType.Shuffle, __OnShuffle);
        __client.Send((short)MahjongNetworkMessageType.Player, new NameMessage(__uid));
    }

    private void __JoinRoom()
    {
        __coroutine = StartCoroutine(__LoadScene(roomSceneBuildIndex, delegate ()
        {
            __InitRoom();

            __client.onRegistered += __OnRegistered;
            __client.Register(new RegisterMessage(__uid, __roomName));
        }, __coroutine));
    }

    private void __InitRoom()
    {
        if (__shuffleMessage == null)
            return;
        
        IEnumerable<Node> nodes = __client == null ? null : __client.nodes;
        if (nodes != null)
        {
            MahjongClientPlayer player;
            foreach (Node node in nodes)
            {
                player = node as MahjongClientPlayer;
                if (player != null)
                    player.Clear();
            }
        }

        MahjongClientRoom room = MahjongClientRoom.instance;
        if (room != null)
        {
            int handTileCount = 18;
            int count = __shuffleMessage.point0 + __shuffleMessage.point1;

            room.Init(handTileCount, ((count + 1) & 3) * handTileCount + count + __shuffleMessage.point2 + __shuffleMessage.point3);
        }
    }

    private IEnumerator __LoadScene(int sceneBuildIndex, Action onComplete, Coroutine coroutine)
    {
        if (coroutine != null)
            yield return coroutine;

        yield return SceneManager.LoadSceneAsync(sceneBuildIndex);

        if (onComplete != null)
            onComplete();
    }

    void Awake()
    {
        __client = GetComponent<MahjongClient>();
        
        Create();

        DontDestroyOnLoad(gameObject);
    }

    /*void Start()
    {
        __client.onConnect += __OnConnect;

        __client.Create();
    }*/

    void OnEnable()
    {
        instance = this;
    }

    void OnDisable()
    {
        instance = null;
    }
}
