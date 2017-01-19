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
    
    public Action<MahjongErrorType> onError;

    private static string __uid = System.Guid.NewGuid().ToString();
    private MahjongClient __client;
    private NameMessage __nameMessage;
    private MahjongRoomMessage __roomMessage;
    private MahjongShuffleMessage __shuffleMessage;
    private Coroutine __coroutine;

    public MahjongClient client
    {
        get
        {
            return __client;
        }
    }
    
    public void Shutdown()
    {
        if (__client != null)
        {
            NetworkWriter writer = new NetworkWriter();
            writer.Write((byte)MahjongQuitType.DestroyRoom);
            __client.onUnregistered += __OnUnregistered;
            __client.Unregister(writer.AsArray(), writer.Position);
        }
    }

    public void Create()
    {
        __client.onConnect += __OnConnect;
        __client.Create();
    }

    public void CreateRoom(Mahjong.ShuffleType shuffleType, MahjongRoomType roomType)
    {
        if (__client == null)
            return;

        if (__client.isConnected)
        {
            __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
            __client.Send((short)MahjongNetworkMessageType.Create, new MahjongRoomMessage(shuffleType, roomType));

            __roomMessage = null;
        }
        else if (__nameMessage == null && __roomMessage == null)
        {
            __roomMessage = new MahjongRoomMessage(shuffleType, roomType);

            Create();
        }
    }
    
    public void JoinRoom(string name)
    {
        if (__client == null)
            return;

        if (__client.isConnected)
        {
            __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
            __client.Send((short)MahjongNetworkMessageType.Room, new NameMessage(name));

            __nameMessage = null;
        }
        else if (__nameMessage == null && __roomMessage == null)
        {
            __nameMessage = new NameMessage(name);

            Create();
        }
    }

    private void __OnUnregistered(Node node)
    {
        if (__client != null)
            __client.onUnregistered -= __OnUnregistered;

        __coroutine = StartCoroutine(__LoadScene(menuSceneBuildIndex, null, __coroutine));
    }
    
    private void __OnRegistered(Node node)
    {
        ZG.Network.Lobby.Node temp = node as ZG.Network.Lobby.Node;
        if (temp != null && temp.isLocalPlayer)
            temp.Ready();
    }
    
    private void __OnPlayer(NetworkMessage message)
    {
        if(__client != null)
            __client.UnregisterHandler((short)MahjongNetworkMessageType.Player);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        if (string.IsNullOrEmpty(nameMessage.name))
        {
            if(__nameMessage == null && __roomMessage == null)
                __coroutine = StartCoroutine(__LoadScene(menuSceneBuildIndex, null, __coroutine));
            else if(__client != null)
            {
                if (__nameMessage != null)
                {
                    __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
                    __client.Send((short)MahjongNetworkMessageType.Room, __nameMessage);

                    __nameMessage = null;
                    __roomMessage = null;
                }
                else if(__roomMessage != null)
                {
                    __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
                    __client.Send((short)MahjongNetworkMessageType.Create, __roomMessage);

                    __roomMessage = null;
                }
            }
        }
        else
            JoinRoom(nameMessage.name);
    }

    private void __OnRoom(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Room);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        __coroutine = StartCoroutine(__LoadScene(roomSceneBuildIndex, delegate ()
        {
            MahjongClientRoom room = __InitRoom();
            if(room != null)
            {
                if (room.name != null)
                    room.name.text = nameMessage.name;
            }

            __client.onRegistered += __OnRegistered;
            __client.Register(new RegisterMessage(__uid, nameMessage.name));
        }, __coroutine));
    }
    
    private void __OnShuffle(NetworkMessage message)
    {
        __shuffleMessage = message == null ? null : message.ReadMessage<MahjongShuffleMessage>();
        if (__shuffleMessage == null)
            return;

        MahjongClientRoom room = __InitRoom();
        if(room != null)
            room.Play(__shuffleMessage.point0, __shuffleMessage.point1, __shuffleMessage.point2, __shuffleMessage.point3);
    }
    
    private void __OnConnect(NetworkMessage message)
    {
        __client.onConnect -= __OnConnect;
        __client.onDisconnect += __OnDisconnect;
        __client.RegisterHandler((short)MahjongNetworkMessageType.Error, __OnError);
        __client.RegisterHandler((short)MahjongNetworkMessageType.Player, __OnPlayer);
        __client.RegisterHandler((short)MahjongNetworkMessageType.Shuffle, __OnShuffle);
        __client.Send((short)MahjongNetworkMessageType.Player, new NameMessage(__uid));
    }

    private void __OnDisconnect(NetworkMessage message)
    {
        if (__client != null)
        {
            __client.UnregisterHandler((short)MahjongNetworkMessageType.Error);

            __client.onDisconnect -= __OnDisconnect;
        }

        Invoke("Create", 0.0f);
    }
    
    private void __OnError(NetworkMessage message)
    {
        UnityEngine.Networking.NetworkSystem.ErrorMessage errorMessage = message == null ? null : message.ReadMessage<UnityEngine.Networking.NetworkSystem.ErrorMessage>();
        if (errorMessage == null)
            return;

        if (onError != null)
            onError((MahjongErrorType)errorMessage.errorCode);
    }

    private MahjongClientRoom __InitRoom()
    {
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
            if (__shuffleMessage != null)
            {
                int count = __shuffleMessage.point0 + __shuffleMessage.point1;

                room.Init(__shuffleMessage.tileCount, (((count + 1) & 3) * ((__shuffleMessage.tileCount + 4) >> 3) + count + __shuffleMessage.point2 + __shuffleMessage.point3) << 1);
            }
        }

        return room;
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
