using UnityEngine;
using UnityEngine.Events;

public class MahjongClientMenu : MonoBehaviour
{
    public UnityEvent onError;

    public void Create()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
        {
            main.onError += __OnError;
            main.JoinRoom(string.Empty);
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
}
