using UnityEngine;
using UnityEngine.Networking;
using ZG.Network;

public class MahjongClientMain : MonoBehaviour
{
    private static string __uid = System.Guid.NewGuid().ToString();
    private MahjongClient __client;
    private string __roomName;

    public void Create(string roomName)
    {
        __roomName = roomName;
        __client.onConnect += __OnConnect;
        __client.Create();
    }

    private void __OnRegistered(Node node)
    {
        ZG.Network.Lobby.Node temp = node as ZG.Network.Lobby.Node;
        if (temp != null && temp.isLocalPlayer)
            temp.Ready();
    }

    private void __Register(string roomName)
    {
        __client.onRegistered += __OnRegistered;
        __client.Register(new InitMessage(__uid, roomName));
    }

    private void __OnRoom(NetworkMessage message)
    {
        __client.UnregisterHandler((short)MahjongNetworkMessageType.Room);

        NameMessage nameMessage = message == null ? null : message.ReadMessage<NameMessage>();
        if (nameMessage == null)
            return;

        __roomName = nameMessage.name;
        __Register(__roomName);
    }

    private void __OnConnect(NetworkMessage message)
    {
        __client.onConnect -= __OnConnect;
        if (string.IsNullOrEmpty(__roomName))
        {
            __client.RegisterHandler((short)MahjongNetworkMessageType.Room, __OnRoom);
            __client.Send((short)MahjongNetworkMessageType.Room, new UnityEngine.Networking.NetworkSystem.EmptyMessage());
        }
        else
            __Register(__roomName);
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
