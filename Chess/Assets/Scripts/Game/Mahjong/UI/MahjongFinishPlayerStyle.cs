using System;
using UnityEngine;
using UnityEngine.UI;

public class MahjongFinishPlayerStyle : MonoBehaviour
{
    [Serializable]
    public struct Tiles
    {
        public RawImage[] images;
    }

    [Serializable]
    public struct HandTiles
    {
        public Tiles tiles;
        public Tiles[] groups;
    }

    [Serializable]
    public struct Score
    {
        public GameObject root;
        public Text text;
    }

    public Text score;
    public HandTiles handTiles;
    public Score[] scores;
    public GameObject[] winners;
    public GameObject[] losers;
}
