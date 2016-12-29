using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using ZG.Network;

public class MahjongClientMain : MonoBehaviour
{
    public int mainSceneBuildIndex;
    public int roomSceneBuildIndex;

    private static string __uid = System.Guid.NewGuid().ToString();
    private MahjongClient __client;
    private Coroutine __coroutine;
    private string __roomName;

    public void Create()
    {
        __client.onConnect += __OnConnect;
        __client.Create();
    }

    public void CreateRoom()
    {
        __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
        __client.Send((short)MahjongNetworkMessageType.Room, new UnityEngine.Networking.NetworkSystem.EmptyMessage());
    }
    
    public void Register(string roomName)
    {
        __roomName = roomName;

        __client.onRegistered += __OnRegistered;
        __client.Register(new InitMessage(__uid, roomName));
    }

    private void __OnRegistered(Node node)
    {
        __coroutine = StartCoroutine(__LoadScene(mainSceneBuildIndex, delegate ()
        {
            ZG.Network.Lobby.Node temp = node as ZG.Network.Lobby.Node;
            if (temp != null && temp.isLocalPlayer)
                temp.Ready();
        }, __coroutine));
    }
    
    private void __OnPlayer(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Player);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        if (string.IsNullOrEmpty(nameMessage.name))
            __coroutine = StartCoroutine(__LoadScene(mainSceneBuildIndex, null, __coroutine));
        else
            Register(nameMessage.name);
    }

    private void __OnRoom(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Room);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;
        
        Register(nameMessage.name);
    }

    private void __OnConnect(NetworkMessage message)
    {
        __client.onConnect -= __OnConnect;
        __client.RegisterHandler((short)MahjongNetworkMessageType.Player, __OnPlayer);
        __client.Send((short)MahjongNetworkMessageType.Player, new NameMessage(__uid));
    }

    private IEnumerator __LoadScene(int sceneBuildIndex, Action onComplete, Coroutine coroutine)
    {
        if (coroutine != null)
            yield return coroutine;

        yield return SceneManager.LoadSceneAsync(mainSceneBuildIndex);

        if (onComplete != null)
            onComplete();
    }

    void Awake()
    {
        __client = GetComponent<MahjongClient>();
    }

    /*void Start()
    {
        __client.onConnect += __OnConnect;

        __client.Create();
    }*/
}
