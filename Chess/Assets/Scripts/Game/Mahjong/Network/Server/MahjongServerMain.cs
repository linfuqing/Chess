using UnityEngine;
using System.Collections;

public class MahjongServerMain : MonoBehaviour
{
    private MahjongServer __server;


    void Awake()
    {
        __server = GetComponent<MahjongServer>();

        DontDestroyOnLoad(gameObject);
    }

    void Start ()
    {
        __server.Create();
    }
}
