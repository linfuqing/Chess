using System;
using UnityEngine;
using UnityEngine.UI;

public class MahjongFinishPlayerStyle : MonoBehaviour
{
    [Serializable]
    public struct Tiles
    {
        public MahjongFinshTileStyle[] styles;

        public void Clear()
        {
            if (styles != null)
            {
                foreach(MahjongFinshTileStyle style in styles)
                {
                    if (style != null)
                        style.Clear();
                }
            }
        }
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

    public void Clear()
    {
        if (score != null)
            score.text = string.Empty;

        handTiles.tiles.Clear();
        if (handTiles.groups != null)
        {
            foreach (Tiles group in handTiles.groups)
                group.Clear();
        }

        if(scores != null)
        {
            foreach(Score score in scores)
            {
                if (score.root != null)
                    score.root.SetActive(false);
            }
        }

        if(winners != null)
        {
            foreach(GameObject winner in winners)
            {
                if (winner != null)
                    winner.SetActive(false);
            }
        }

        if (losers != null)
        {
            foreach (GameObject loser in losers)
            {
                if (loser != null)
                    loser.SetActive(false);
            }
        }
    }
}
