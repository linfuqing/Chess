using UnityEngine;
using UnityEngine.Events;

public class MahjongClientMenu : MonoBehaviour
{
    public UnityEvent onError;
    private Mahjong.ShuffleType __shuffleType;
    private MahjongRoomType __roomType;

    public bool isWinds
    {
        set
        {
            if (value)
                __shuffleType |= Mahjong.ShuffleType.Winds;
            else
                __shuffleType &= ~Mahjong.ShuffleType.Winds;
        }
    }

    public bool isFlowers
    {
        set
        {
            if (value)
                __shuffleType |= Mahjong.ShuffleType.Flowers;
            else
                __shuffleType &= ~Mahjong.ShuffleType.Flowers;
        }
    }

    public MahjongRoomType roomType
    {
        set
        {
            __roomType = value;
        }
    }

    public void Create()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
        {
            main.onError += __OnError;
            main.CreateRoom(__shuffleType, __roomType);
        }
    }

    public void Join(string roomName)
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
        {
            main.onError += __OnError;
            main.JoinRoom(roomName);
        }
    }

    private void __OnError()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
            main.onError -= __OnError;

        if (onError != null)
            onError.Invoke();
    }

    void Start()
    {
        __shuffleType = Mahjong.ShuffleType.All;
        __roomType = MahjongRoomType.Normal;
    }
}
