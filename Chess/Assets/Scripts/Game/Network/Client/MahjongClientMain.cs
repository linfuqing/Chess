using UnityEngine;
using UnityEngine.Networking;
using ZG.Network;

public class MahjongClientMain : MonoBehaviour
{
    private MahjongClient __client;

    public void Create(string ipAddress)
    {
        __client.onConnect += __OnConnect;
        __client.ipAddress = ipAddress;
        __client.Create();
    }

    private void __OnRegistered(Node node)
    {
        ZG.Network.Lobby.Node temp = node as ZG.Network.Lobby.Node;
        if (temp != null && temp.isLocalPlayer)
            temp.SendReadyMessage();
    }

    private void __OnConnect(NetworkMessage message)
    {
        __client.onConnect -= __OnConnect;
        __client.onRegistered += __OnRegistered;
        __client.Register(new InitMessage(System.Guid.NewGuid().ToString(), "fuck"));
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
