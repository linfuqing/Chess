using UnityEngine;
using UnityEngine.UI;

public class MahjongClientMenu : MonoBehaviour
{
    public void Create()
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
            main.CreateRoom();
    }

    public void Join(string roomName)
    {
        MahjongClientMain main = MahjongClientMain.instance;
        if (main != null)
            main.JoinRoom(roomName);
    }
}
