using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class MahjongClientMenu : MonoBehaviour
{
    [FormerlySerializedAs("onError")]
    public UnityEvent onRoomNoneError;
    public UnityEvent onRoomFullError;
    public UnityEvent onRoomCreateFailError;
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

    public int roomType
    {
        set
        {
            __roomType = (MahjongRoomType)value;
        }
    }

    public void Create()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
        {
            main.onError = __OnError;
            main.CreateRoom(__shuffleType, __roomType);
        }
    }

    public void Join(string roomName)
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
        {
            main.onError = __OnError;
            main.JoinRoom(roomName);
        }
    }

    private void __OnError(MahjongErrorType type)
    {
        switch(type)
        {
            case MahjongErrorType.RoomNone:
                if (onRoomNoneError != null)
                    onRoomNoneError.Invoke();
                break;
            case MahjongErrorType.RoomFull:
                if (onRoomFullError != null)
                    onRoomFullError.Invoke();
                break;
            case MahjongErrorType.RoomCreatedFail:
                if (onRoomCreateFailError != null)
                    onRoomCreateFailError.Invoke();
                break;
        }
    }

    void Start()
    {
        __shuffleType = Mahjong.ShuffleType.All;
        __roomType = MahjongRoomType.Normal;
    }
}
